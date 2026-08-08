using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using PaiSho;
using PaiSho.Board;
using PaiSho.DevTools;

namespace PaiSho.Game
{
    /// <summary>Runtime overlay to tune player stand + curved hand tile layout. Press F9 in play mode.</summary>
    public class HandTrayTuner : MonoBehaviour
    {
        public static HandTrayTuner Instance;

        private static readonly float[] NudgeFine = { -0.05f, -0.01f, 0.01f, 0.05f };
        private static readonly float[] NudgeCoarse = { -0.2f, -0.1f, 0.1f, 0.2f };

        [SerializeField] private Key toggleKey = Key.F9;
        [SerializeField] private bool panelOpen;

        private HandTrayTunerSettings settings = new();
        private readonly Dictionary<string, string> fieldBuffers = new();
        private Vector2 scroll;
        private Rect lastContentArea;
        private int selectedSlot;
        private bool showStandSection;
        private bool showTraySection;
        private bool showSlotsSection;
        private bool slot3DEdit = true;
        private float savedTimeScale = 1f;
        private string statusMessage = "F9 hand tray tuner";
        private float statusClearTime;
        private float debouncedApplyTime;
        private HandTraySlotGizmo slotGizmo;

        public bool IsPanelOpen => panelOpen;
        public bool IsSlot3DEditActive => panelOpen && slot3DEdit;
        public HandTrayTunerSettings Settings => settings;

        private void Awake()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            enabled = false;
            return;
#endif
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            settings.EnsureSlotArrays();
            settings.PullFromDefaults();
            slotGizmo = gameObject.AddComponent<HandTraySlotGizmo>();
            slotGizmo.Bind(this);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            ResumeGame();
        }

        private void Update()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                panelOpen = !panelOpen;
                if (panelOpen)
                {
                    settings.EnsureSlotArrays();
                    settings.PullFromDefaults();
                    settings.useManualSlotPositions = true;
                    settings.previewAllSlots = true;
                    slot3DEdit = true;
                    selectedSlot = 0;
                    slotGizmo?.SetSelectedSlot(selectedSlot);
                    slotGizmo?.SetMode(HandTraySlotGizmo.GizmoMode.Translate);
                    ClearFieldBuffers();
                    PauseGame();
                    ApplySettings();
                    SetStatus("3D edit — W move, E rotate, click tile or drag axes");
                }
                else
                {
                    settings.previewAllSlots = false;
                    slotGizmo?.Tick(false, settings);
                    ResumeGame();
                    ApplySettings();
                }

                return;
            }

            if (!panelOpen)
                return;

            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                settings.editingPlayer = settings.editingPlayer == Player.Host ? Player.Opponent : Player.Host;
                showStandSection = true;
                ClearFieldBuffers();
                debouncedApplyTime = Time.unscaledTime + 0.05f;
                slotGizmo?.SetSelectedSlot(selectedSlot);
                SetStatus($"Editing {settings.editingPlayer}");
            }

            if (Keyboard.current.leftBracketKey.wasPressedThisFrame)
            {
                selectedSlot = Mathf.Max(0, selectedSlot - 1);
                slotGizmo?.SetSelectedSlot(selectedSlot);
                debouncedApplyTime = Time.unscaledTime + 0.05f;
            }

            if (Keyboard.current.rightBracketKey.wasPressedThisFrame)
            {
                selectedSlot = Mathf.Min(HandTrayTunerSettings.MaxSlots - 1, selectedSlot + 1);
                slotGizmo?.SetSelectedSlot(selectedSlot);
                debouncedApplyTime = Time.unscaledTime + 0.05f;
            }

            slotGizmo?.Tick(panelOpen && slot3DEdit, settings);
        }

        public void NotifySlotSelected(int slot)
        {
            selectedSlot = slot;
            slotGizmo?.SetSelectedSlot(slot);
            SetStatus($"Slot {slot} selected");
        }

        public void NotifySlotEdited()
        {
            debouncedApplyTime = Time.unscaledTime + 0.05f;
        }

        private void PauseGame()
        {
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        private void ResumeGame()
        {
            if (Time.timeScale == 0f)
                Time.timeScale = savedTimeScale > 0f ? savedTimeScale : 1f;
        }

        private void LateUpdate()
        {
            if (!panelOpen || debouncedApplyTime <= 0f || Time.unscaledTime < debouncedApplyTime)
                return;

            if (slotGizmo != null && slotGizmo.IsDragging)
                return;

            debouncedApplyTime = 0f;
            ApplySettings();
        }

        private bool standLayoutDirty;

        public void ApplySettings(bool refreshStands = false)
        {
            settings.EnsureSlotArrays();
            HandTrayController.Instance?.ApplyTunerSettings(settings, panelOpen);
            if (refreshStands || standLayoutDirty)
            {
                standLayoutDirty = false;
                BoardManager.Instance?.RefreshAllPlayerStands();
            }
        }

        public void ExportSettings()
        {
            settings.EnsureSlotArrays();
            string report = settings.ToShareableReport();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string exportPath = Path.Combine(Application.persistentDataPath, "hand-tray-tuner-export.txt");
            File.WriteAllText(exportPath, report);
#endif
            GUIUtility.systemCopyBuffer = report;
            SetStatus("Exported — clipboard + file");
            DebugLogger.Log(report);
        }

        public void ResetStandDefaults()
        {
            if (settings.editingPlayer == Player.Host)
                HandTrayAlignmentDefaults.ApplyHostTo(settings);
            else
                HandTrayAlignmentDefaults.ApplyOpponentTo(settings);

            ClearFieldBuffers();
            standLayoutDirty = true;
            ApplySettings();
            SetStatus($"{settings.editingPlayer} reset");
        }

        public void SeedLinearSlots()
        {
            float localSpacing = settings.autoSlotSpacing;
            BoardLayout layout = FindAnyObjectByType<BoardLayout>();
            if (layout != null)
            {
                localSpacing = layout.CellSpacing * settings.autoSlotSpacing;
                Transform stand = BoardManager.Instance?.GetPlayerStandTransform(settings.editingPlayer);
                if (stand != null)
                {
                    float standScale = Mathf.Max(stand.lossyScale.x, 0.001f);
                    localSpacing /= standScale;
                }
            }

            settings.SeedLinearSlotLayout(localSpacing, settings.editingPlayer);
            ClearFieldBuffers();
            ApplySettings();
            SetStatus("Linear seed — curve each slot");
        }

        public void ClosePanel()
        {
            panelOpen = false;
            settings.previewAllSlots = false;
            ResumeGame();
            ApplySettings();
        }

        private void SetStatus(string message)
        {
            statusMessage = message;
            statusClearTime = Time.unscaledTime + 8f;
        }

        private void ClearFieldBuffers() => fieldBuffers.Clear();

        private void OnGUI()
        {
            if (!panelOpen)
            {
                DevTunerGui.ClearPanelRect();
                DevTunerGui.DrawClosedHints(
                    (KeyCode.F8, "Board"),
                    (KeyCode.F9, "Hand tray"),
                    (KeyCode.F10, "Capture pot"));
                return;
            }

            DrawPanel();
        }

        private void DrawPanel()
        {
            bool useScrollView = !slot3DEdit;
            DevTunerGui.BeginPanel(
                "Hand Tray [F9]",
                DevTunerGui.DefaultPanelWidth,
                DevTunerGui.DefaultMargin,
                DevTunerGui.FooterHeight,
                ref scroll,
                out lastContentArea,
                useScrollView);

            int editIndex = settings.editingPlayer == Player.Host ? 0 : 1;
            DevTunerGui.Toolbar(editIndex, new[] { "Host", "Opponent" }, index =>
            {
                settings.editingPlayer = index == 0 ? Player.Host : Player.Opponent;
                showStandSection = true;
                ClearFieldBuffers();
                debouncedApplyTime = Time.unscaledTime + 0.05f;
                slotGizmo?.SetSelectedSlot(selectedSlot);
            });

            GUILayout.BeginHorizontal();
            settings.previewAllSlots = GUILayout.Toggle(settings.previewAllSlots, "7 slots", GUILayout.Width(68f));
            settings.useManualSlotPositions = GUILayout.Toggle(settings.useManualSlotPositions, "Manual");
            slot3DEdit = GUILayout.Toggle(slot3DEdit, "3D gizmo", GUILayout.Width(82f));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (slot3DEdit)
            {
                GUILayout.Label(
                    "Game paused. Click a preview tile to select it.\n" +
                    "W = move axes (drag red/green/blue)   E = rotate rings\n" +
                    "[ ] prev/next slot   Tab = switch stand side",
                    GUI.skin.label);
            }
            else
            {
                GUILayout.Label("Tab side  [ ] slot", GUI.skin.label);
            }

            if (DevTunerGui.Section(ref showStandSection, $"Stand — {settings.editingPlayer}"))
            {
                settings.standSouthCells = DevTunerGui.FloatField(
                    fieldBuffers, "stand.south", "South", settings.standSouthCells, 0f, 6f);
                settings.standEastCells = DevTunerGui.FloatField(
                    fieldBuffers, "stand.east", "East", settings.standEastCells, -3f, 5f);
                settings.standLiftOffset = DevTunerGui.FloatField(
                    fieldBuffers, "stand.lift", "Lift", settings.standLiftOffset, -0.2f, 0.2f);
                settings.standYawOffset = DevTunerGui.FloatField(
                    fieldBuffers, "stand.yaw", "Yaw", settings.standYawOffset, -45f, 45f);
                settings.standScaleMultiplier = DevTunerGui.FloatField(
                    fieldBuffers, "stand.scale", "Scale", settings.standScaleMultiplier, 0.5f, 1.5f);
                settings.standExtraOffset = DevTunerGui.Vector3Field(
                    fieldBuffers, "stand.extra", "Extra offset",
                    settings.standExtraOffset,
                    new Vector3(-1f, -0.3f, -1f),
                    new Vector3(1f, 0.3f, 1f));
                DevTunerGui.EndSection(true);
                if (GUI.changed)
                    standLayoutDirty = true;
            }

            if (DevTunerGui.Section(ref showTraySection, "Tray (local to stand)"))
            {
                string trayPrefix = settings.editingPlayer == Player.Host ? "host.tray" : "opp.tray";
                ref Vector3 trayOffset = ref settings.TrayLocalOffsetFor(settings.editingPlayer);
                ref Vector3 trayEuler = ref settings.TrayLocalEulerFor(settings.editingPlayer);

                trayOffset = DevTunerGui.Vector3Field(
                    fieldBuffers, $"{trayPrefix}.pos", "Offset",
                    trayOffset,
                    new Vector3(-1f, -1.5f, -1f),
                    new Vector3(1f, 0.5f, 1f));

                DevTunerGui.NudgeRow("Tray Y", ref trayOffset.y, NudgeCoarse);

                trayEuler = DevTunerGui.EulerField(fieldBuffers, $"{trayPrefix}.euler", "Rotation", trayEuler, 30f);
                DevTunerGui.EndSection(true);
            }

            if (DevTunerGui.Section(ref showSlotsSection, $"Slot {selectedSlot} (tray local)"))
            {
                selectedSlot = DevTunerGui.SlotButtons(selectedSlot, HandTrayTunerSettings.MaxSlots);
                slotGizmo?.SetSelectedSlot(selectedSlot);

                if (!slot3DEdit)
                {
                    if (!settings.useManualSlotPositions)
                    {
                        settings.autoSlotSpacing = DevTunerGui.FloatField(
                            fieldBuffers, "slot.autoSpacing", "Auto gap", settings.autoSlotSpacing, 0.5f, 1.2f);
                    }

                    Vector3[] slotPositions = settings.GetSlotPositions(settings.editingPlayer);
                    Vector3[] slotEuler = settings.GetSlotEuler(settings.editingPlayer);
                    string slotPrefix = $"{settings.editingPlayer}.slot{selectedSlot}";

                    ref Vector3 slotPos = ref slotPositions[selectedSlot];
                    ref Vector3 slotRot = ref slotEuler[selectedSlot];

                    DevTunerGui.NudgeRow("Y fine", ref slotPos.y, NudgeFine);
                    DevTunerGui.NudgeRow("Y coarse", ref slotPos.y, NudgeCoarse);

                    slotPos = DevTunerGui.Vector3Field(
                        fieldBuffers, $"{slotPrefix}.pos", "Position",
                        slotPos,
                        new Vector3(-8f, -1.5f, -2f),
                        new Vector3(8f, 0.5f, 2f));
                    slotRot = DevTunerGui.EulerField(
                        fieldBuffers, $"{slotPrefix}.euler", "Rotation", slotRot, 90f);
                }
                else
                {
                    GUILayout.Label("Use the 3D gizmo in the scene view. Expand sections below for stand/tray sliders.");
                }

                DevTunerGui.EndSection(true);
            }

            DevTunerGui.Status(statusMessage, statusClearTime);

            if (GUI.changed)
                debouncedApplyTime = Time.unscaledTime + 0.15f;

            DevTunerGui.EndPanelScroll(useScrollView);

            DevTunerGui.DrawFooter(lastContentArea, DevTunerGui.FooterHeight, () =>
            {
                DevTunerGui.ActionBar(
                    ("Export", ExportSettings),
                    ("Reset", ResetStandDefaults),
                    ("Seed", SeedLinearSlots),
                    ("Close", ClosePanel));
            });
        }
    }
}
