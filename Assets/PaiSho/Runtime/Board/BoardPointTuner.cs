using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using PaiSho;
using PaiSho.DevTools;
using PaiSho.Game;

namespace PaiSho.Board
{
    /// <summary>Runtime overlay to tune board grid + gardens. Press F8 in play mode.</summary>
    public class BoardPointTuner : MonoBehaviour
    {
        public static BoardPointTuner Instance;

        [SerializeField] private Key toggleKey = Key.F8;
        [SerializeField] private bool panelOpen;

        private BoardPointTunerSettings settings = new();
        private readonly System.Collections.Generic.Dictionary<string, string> fieldBuffers = new();
        private BoardLayout layout;
        private Vector2 scroll;
        private Rect lastContentArea;
        private Rect panelScreenRect;
        private bool showMarkers = true;
        private bool showGrid = true;
        private bool showGardens = true;
        private bool showModel = false;
        private string statusMessage = "F8 board tuner";
        private float statusClearTime;
        private float debouncedApplyTime;

        public bool IsPanelOpen => panelOpen;
        public BoardPointTunerSettings Settings => settings;

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
            settings.PullFromDefaults();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                panelOpen = !panelOpen;
                if (panelOpen)
                {
                    OpenPanel();
                    return;
                }

                settings.showMarkers = false;
                settings.showLabels = false;
                BoardManager.Instance?.RefreshGameplayBoardPoints();
                return;
            }

            if (!panelOpen)
                return;

            HandleGardenHotkeys();
            HandleGardenPaintClick();
        }

        private void LateUpdate()
        {
            if (!panelOpen || debouncedApplyTime <= 0f || Time.unscaledTime < debouncedApplyTime)
                return;

            debouncedApplyTime = 0f;
            ApplySettings();
        }

        private void OpenPanel()
        {
            layout = FindAnyObjectByType<BoardLayout>();
            if (layout != null)
                settings.PullFromLayout(layout);
            else
                settings.PullGardensFromActive();

            settings.showMarkers = true;
            settings.showLabels = true;
            settings.colorByGarden = true;
            settings.gardenPaintMode = GardenPaintMode.Cycle;
            ApplySettings();
            SetStatus(PortLegend() + " | Click points to paint gardens");
        }

        public void ApplySettings()
        {
            layout = layout != null ? layout : FindAnyObjectByType<BoardLayout>();
            if (layout == null)
                return;

            settings.SyncMarkerColorAlpha();
            settings.ApplyGardensToRuntime();
            layout.SetGridSpacingScale(settings.gridSpacingScale);
            layout.SetTunerOverlay(
                settings.spacingFineTune,
                new Vector2(settings.gridOffsetX, settings.gridOffsetZ),
                settings.tileHeightOffset);
            layout.SetGridYawDegrees(settings.gridYawDegrees);
            layout.SetBoardSurfaceHeight(settings.tileHeight);
            layout.SetBoardPointColliderScale(settings.colliderScale);
            layout.SetBoardModelOffset(new Vector3(
                settings.boardModelOffsetX,
                settings.boardModelOffsetY,
                settings.boardModelOffsetZ));

            BoardManager.Instance?.RefreshAllBoardPoints(settings);
            BoardManager.Instance?.RefreshAllPiecePositions();
            HandTrayController.Instance?.Refresh();
        }

        private void HandleGardenHotkeys()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                settings.gardenPaintMode = GardenPaintMode.Light;
                SetStatus("Paint mode: Light / White only");
            }
            else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                settings.gardenPaintMode = GardenPaintMode.Dark;
                SetStatus("Paint mode: Dark / Red only");
            }
            else if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                settings.gardenPaintMode = GardenPaintMode.Mixed;
                SetStatus("Paint mode: Mixed border (both)");
            }
            else if (Keyboard.current.digit4Key.wasPressedThisFrame)
            {
                settings.gardenPaintMode = GardenPaintMode.Neutral;
                SetStatus("Paint mode: Neutral");
            }
            else if (Keyboard.current.digit0Key.wasPressedThisFrame || Keyboard.current.cKey.wasPressedThisFrame)
            {
                settings.gardenPaintMode = GardenPaintMode.Cycle;
                SetStatus("Paint mode: Cycle N → L → D → M");
            }
        }

        private void HandleGardenPaintClick()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
                return;

            Vector2 screen = Mouse.current.position.ReadValue();
            Vector2 guiPoint = new Vector2(screen.x, Screen.height - screen.y);
            if (panelScreenRect.Contains(guiPoint))
                return;

            Camera camera = Camera.main;
            if (camera == null || BoardManager.Instance == null)
                return;

            Ray ray = camera.ScreenPointToRay(screen);
            if (!BoardManager.Instance.TryResolveCoordinate(ray, out int coordinate))
                return;

            if (BoardUtils.IsPort(coordinate))
            {
                SetStatus($"Port {coordinate} is fixed (not a garden)");
                return;
            }

            GardenType applied;
            if (settings.gardenPaintMode == GardenPaintMode.Cycle)
            {
                applied = BoardUtils.CycleGardenType(coordinate);
            }
            else
            {
                applied = settings.gardenPaintMode switch
                {
                    GardenPaintMode.Light => GardenType.LightGarden,
                    GardenPaintMode.Dark => GardenType.DarkGarden,
                    GardenPaintMode.Mixed => GardenType.MixedGarden,
                    _ => GardenType.NeutralGarden
                };
                BoardUtils.SetGardenType(coordinate, applied);
            }

            settings.PullGardensFromActive();
            BoardManager.Instance.RefreshAllBoardPoints(settings);
            SetStatus($"Point {coordinate} → {BoardPointTunerSettings.GardenName(applied)}  " +
                      $"(L={settings.lightGardenCoordinates.Length} D={settings.darkGardenCoordinates.Length})");
        }

        private static string PortLegend()
        {
            return $"Ports: S/Home={BoardUtils.SouthGate}  N/Foreign={BoardUtils.NorthGate}  " +
                   $"E={BoardUtils.EastGate}  W={BoardUtils.WestGate}  Mid={BoardUtils.MiddleGate}";
        }

        public void RealignBoardModel()
        {
            layout = layout != null ? layout : FindAnyObjectByType<BoardLayout>();
            if (layout == null)
                return;

            layout.SetBoardModelOffset(new Vector3(
                settings.boardModelOffsetX,
                settings.boardModelOffsetY,
                settings.boardModelOffsetZ));
            WoodTheme.FineAlignBoardModelIfPresent(layout);
            settings.tileHeight = layout.TileHeight;
            ApplySettings();
            SetStatus("Board model re-aligned");
        }

        public void ExportSettings()
        {
            settings.SyncMarkerColorAlpha();
            settings.PullGardensFromActive();
            string report = settings.ToShareableReport();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string exportPath = Path.Combine(Application.persistentDataPath, "board-point-tuner-export.txt");
            File.WriteAllText(exportPath, report);
#endif
            GUIUtility.systemCopyBuffer = report;
            SetStatus("Exported grid + gardens — clipboard + file");
            DebugLogger.Log(report);
        }

        public void ResetFromLayout()
        {
            settings.PullFromDefaults();
            fieldBuffers.Clear();
            ApplySettings();
            SetStatus("Baked defaults + default gardens loaded");
        }

        public void ResetGardensOnly()
        {
            settings.PullGardensFromBoardUtilsDefaults();
            BoardManager.Instance?.RefreshAllBoardPoints(settings);
            SetStatus($"Gardens reset (L={settings.lightGardenCoordinates.Length} D={settings.darkGardenCoordinates.Length})");
        }

        public void ClosePanel()
        {
            panelOpen = false;
            settings.showMarkers = false;
            settings.showLabels = false;
            BoardManager.Instance?.RefreshGameplayBoardPoints();
        }

        private void SetStatus(string message)
        {
            statusMessage = message;
            statusClearTime = Time.unscaledTime + 8f;
        }

        private void OnGUI()
        {
            if (!panelOpen)
            {
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
            DevTunerGui.BeginPanel(
                "Board Grid + Gardens [F8]",
                DevTunerGui.DefaultPanelWidth,
                DevTunerGui.DefaultMargin,
                DevTunerGui.FooterHeight,
                ref scroll,
                out lastContentArea);

            panelScreenRect = new Rect(
                DevTunerGui.DefaultMargin,
                DevTunerGui.DefaultMargin,
                DevTunerGui.DefaultPanelWidth,
                Screen.height - DevTunerGui.DefaultMargin * 2f);

            settings.showMarkers = GUILayout.Toggle(settings.showMarkers, "Show markers");
            settings.showLabels = GUILayout.Toggle(settings.showLabels, "Show coord labels (# / L·D·N)");
            settings.colorByGarden = GUILayout.Toggle(settings.colorByGarden, "Color by garden (L=tan D=blue ports=gold)");

            GUILayout.Label(PortLegend(), EditorLabelStyle());

            if (DevTunerGui.Section(ref showGardens, "Gardens (click board points to paint)"))
            {
                GUILayout.Label("Paint: 1=Light  2=Dark  3=Mixed(both)  4=Neutral  0/C=Cycle", EditorLabelStyle());
                GUILayout.BeginHorizontal();
                DrawPaintModeButton("Cycle", GardenPaintMode.Cycle);
                DrawPaintModeButton("Light", GardenPaintMode.Light);
                DrawPaintModeButton("Dark", GardenPaintMode.Dark);
                DrawPaintModeButton("Mixed", GardenPaintMode.Mixed);
                DrawPaintModeButton("Neutral", GardenPaintMode.Neutral);
                GUILayout.EndHorizontal();

                settings.PullGardensFromActive();
                GUILayout.Label(
                    $"Light/White: {settings.lightGardenCoordinates.Length}   " +
                    $"Dark/Red: {settings.darkGardenCoordinates.Length}",
                    EditorLabelStyle());

                if (GUILayout.Button("Reset gardens to defaults"))
                    ResetGardensOnly();

                DevTunerGui.EndSection(true);
            }

            if (DevTunerGui.Section(ref showMarkers, "Markers"))
            {
                settings.markerDiameterScale = DevTunerGui.FloatField(
                    fieldBuffers, "marker.size", "Size", settings.markerDiameterScale, 0.04f, 0.35f);
                settings.markerHeightOffset = DevTunerGui.FloatField(
                    fieldBuffers, "marker.height", "Height", settings.markerHeightOffset, -0.05f, 0.12f);
                settings.markerAlpha = DevTunerGui.FloatField(
                    fieldBuffers, "marker.alpha", "Alpha", settings.markerAlpha, 0.1f, 1f);
                settings.markerColor = DrawCompactColor(settings.markerColor);
                DevTunerGui.EndSection(true);
            }

            if (DevTunerGui.Section(ref showGrid, "Grid"))
            {
                settings.gridSpacingScale = DevTunerGui.FloatField(
                    fieldBuffers, "grid.scale", "Spacing", settings.gridSpacingScale, 0.85f, 1.1f);
                settings.spacingFineTune = DevTunerGui.FloatField(
                    fieldBuffers, "grid.fine", "Fine tune", settings.spacingFineTune, 0.92f, 1.08f);
                settings.gridOffsetX = DevTunerGui.FloatField(
                    fieldBuffers, "grid.offX", "Offset X", settings.gridOffsetX, -0.25f, 0.25f);
                settings.gridOffsetZ = DevTunerGui.FloatField(
                    fieldBuffers, "grid.offZ", "Offset Z", settings.gridOffsetZ, -0.25f, 0.25f);
                settings.gridYawDegrees = DevTunerGui.FloatField(
                    fieldBuffers, "grid.yaw", "Yaw °", settings.gridYawDegrees, -180f, 180f);
                settings.tileHeight = DevTunerGui.FloatField(
                    fieldBuffers, "grid.tileH", "Surface Y", settings.tileHeight, 0f, 0.6f);
                settings.tileHeightOffset = DevTunerGui.FloatField(
                    fieldBuffers, "grid.ptH", "Point Y", settings.tileHeightOffset, -0.08f, 0.08f);
                settings.colliderScale = DevTunerGui.FloatField(
                    fieldBuffers, "grid.hit", "Hit scale", settings.colliderScale, 0.5f, 1.1f);
                DevTunerGui.EndSection(true);
            }

            if (DevTunerGui.Section(ref showModel, "Board model nudge"))
            {
                settings.boardModelOffsetX = DevTunerGui.FloatField(
                    fieldBuffers, "model.x", "X", settings.boardModelOffsetX, -0.5f, 0.5f);
                settings.boardModelOffsetY = DevTunerGui.FloatField(
                    fieldBuffers, "model.y", "Y", settings.boardModelOffsetY, -0.5f, 0.5f);
                settings.boardModelOffsetZ = DevTunerGui.FloatField(
                    fieldBuffers, "model.z", "Z", settings.boardModelOffsetZ, -0.5f, 0.5f);
                DevTunerGui.EndSection(true);
            }

            if (layout != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"cell {layout.CellSpacing:F3}", GUILayout.Width(100f));
                GUILayout.Label($"span {layout.GridSpan:F3}", GUILayout.Width(100f));
                GUILayout.Label($"yaw {layout.GridYawDegrees:F1}°");
                GUILayout.EndHorizontal();
            }

            DevTunerGui.Status(statusMessage, statusClearTime);

            if (GUI.changed)
                debouncedApplyTime = Time.unscaledTime + 0.15f;

            DevTunerGui.EndPanelScroll();

            DevTunerGui.DrawFooter(lastContentArea, DevTunerGui.FooterHeight, () =>
            {
                DevTunerGui.ActionBar(
                    ("Export", ExportSettings),
                    ("Reset", ResetFromLayout),
                    ("Align", RealignBoardModel),
                    ("Close", ClosePanel));
            });
        }

        private void DrawPaintModeButton(string label, GardenPaintMode mode)
        {
            bool selected = settings.gardenPaintMode == mode;
            Color previous = GUI.backgroundColor;
            if (selected)
                GUI.backgroundColor = new Color(0.4f, 0.75f, 1f);
            if (GUILayout.Button(label))
                settings.gardenPaintMode = mode;
            GUI.backgroundColor = previous;
        }

        private Color DrawCompactColor(Color color)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("RGB", GUILayout.Width(88f));
            color.r = GUILayout.HorizontalSlider(color.r, 0f, 1f);
            color.g = GUILayout.HorizontalSlider(color.g, 0f, 1f);
            color.b = GUILayout.HorizontalSlider(color.b, 0f, 1f);
            GUILayout.EndHorizontal();
            return color;
        }

        private static GUIStyle EditorLabelStyle()
        {
            return new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                fontSize = 11
            };
        }
    }
}
