using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;
using PaiSho.Board;
using PaiSho.Pieces;
using PaiSho.UI;
using PaiSho;

namespace PaiSho.Game
{
    /// <summary>Minimal floating HUD — dark frosted pills, centered dock, no full-screen bars.</summary>
    [DefaultExecutionOrder(50)]
    public class GameUI : MonoBehaviour
    {
        public static bool IsPointerOverHud =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        /// <summary>True while the hotseat pass-device scrim blocks board/tray input.</summary>
        public static bool IsPassScrimShowing { get; private set; }

        private TextMeshProUGUI metaLine;
        private TextMeshProUGUI youLine;
        private TextMeshProUGUI oppLine;
        private TextMeshProUGUI contextLine;
        private TextMeshProUGUI toastLine;
        private TextMeshProUGUI endTitle;
        private TextMeshProUGUI endBody;
        private TextMeshProUGUI logBody;
        private TextMeshProUGUI guideBody;
        private TextMeshProUGUI echoTitle;
        private TextMeshProUGUI passScrimText;

        private RectTransform toastRect;
        private Vector2 toastRestPosition;
        private CanvasGroup toastGroup;
        private CanvasGroup endScreenGroup;
        private RectTransform guideSheet;
        private RectTransform logSheet;
        private RectTransform echoSheet;
        private RectTransform echoButtonRow;
        private RectTransform menuSheet;
        private RectTransform passScrimSheet;
        private RectTransform safeArea;

        private HudButton passButton;
        private TextMeshProUGUI passButtonLabel;
        private HudButton extraMoveButton;
        private HudButton reviveButton;
        private HudButton freezeButton;
        private HudButton rotateButton;
        private HudButton menuButton;
        private HudButton playAgainButton;
        private HudButton titleButton;
        private Slider volumeMenuSlider;

        private readonly List<(TextMeshProUGUI text, float baseSize)> scalableTexts = new();
        private readonly List<(TextMeshProUGUI text, Func<string> label)> menuToggleLabels = new();

        private bool showGuide;
        private bool showLog;
        private bool hudReady;
        private Player lastBannerPlayer = (Player)(-1);
        private int lastBannerTurn;
        private string lastEchoSignature = "";
        private int lastHostRingThreat = -1;
        private int lastOppRingThreat = -1;
        private int lastHotseatTurnNumber = -1;
        private float forfeitArmedUntil;
        private int lastSafeAreaScreenWidth = -1;
        private int lastSafeAreaScreenHeight = -1;
        private bool lastAppliedLargerHudType;

        private void Awake()
        {
            EnsureEventSystem();
            HudDesignSystem.EnsureDefaultFont();
            BuildHud();
            hudReady = true;
        }

        private void LateUpdate()
        {
            if (!hudReady || GameManager.Instance == null || ReserveManager.Instance == null)
                return;

            RefreshSafeAreaIfChanged();
            ApplyHudTypeScaleIfChanged();

            if (TitleMenu.Instance != null && TitleMenu.Instance.IsOpen)
                return;

            if (GameStateManager.Instance != null && GameStateManager.Instance.IsEndPhase())
            {
                ShowEndScreen();
                return;
            }

            HideEndScreen();
            RefreshHud();
            RefreshToast();
            RefreshEchoChooser();
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildHud()
        {
            Canvas canvas = HudBuilder.EnsureCanvas(transform, "GameHud");
            safeArea = HudBuilder.Stretch(canvas.transform, "SafeArea");
            ApplySafeArea();

            BuildTopPills(safeArea);
            BuildContextPill(safeArea);
            BuildDock(safeArea);
            BuildToast(safeArea);
            BuildSheets(safeArea);
            BuildMenuSheet(safeArea);
            BuildEchoChooser(safeArea);
            BuildEndScreen(safeArea);
            BuildPassScrim(safeArea);
        }

        /// <summary>Anchors the HUD root to the device safe area (notches / rounded corners), plus fixed breathing room.</summary>
        private void ApplySafeArea()
        {
            if (safeArea == null)
                return;

            Rect area = Screen.safeArea;
            float width = Mathf.Max(1f, Screen.width);
            float height = Mathf.Max(1f, Screen.height);

            safeArea.anchorMin = new Vector2(area.x / width, area.y / height);
            safeArea.anchorMax = new Vector2((area.x + area.width) / width, (area.y + area.height) / height);
            safeArea.offsetMin = new Vector2(HudDesignSystem.Safe, HudDesignSystem.Safe);
            safeArea.offsetMax = new Vector2(-HudDesignSystem.Safe, -HudDesignSystem.Safe);

            lastSafeAreaScreenWidth = Screen.width;
            lastSafeAreaScreenHeight = Screen.height;
        }

        private void RefreshSafeAreaIfChanged()
        {
            if (Screen.width != lastSafeAreaScreenWidth || Screen.height != lastSafeAreaScreenHeight)
                ApplySafeArea();
        }

        private void BuildTopPills(RectTransform safe)
        {
            var metaPill = HudBuilder.CreatePill(safe, "Meta", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0f));
            metaLine = HudBuilder.AddPillText(metaPill, "Spring | Turn 1", 18f, HudDesignSystem.TextPrimary, FontStyles.Bold);
            RegisterScalableText(metaLine, 18f);

            // You-first stats pill: your line reads bold/full-size, opponent's is a quieter second line.
            var scorePill = HudBuilder.CreateRect(
                safe, "Score", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(260f, HudDesignSystem.PillHeight));
            HudBuilder.AddSlicedImage(scorePill, HudDesignSystem.PillSprite, Color.white);
            var scoreFitter = scorePill.gameObject.AddComponent<ContentSizeFitter>();
            scoreFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            scoreFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scoreLayout = scorePill.gameObject.AddComponent<VerticalLayoutGroup>();
            scoreLayout.padding = new RectOffset(16, 16, 8, 8);
            scoreLayout.spacing = 2f;
            scoreLayout.childAlignment = TextAnchor.MiddleRight;
            scoreLayout.childControlWidth = true;
            scoreLayout.childControlHeight = true;
            scoreLayout.childForceExpandWidth = false;
            scoreLayout.childForceExpandHeight = false;

            var youRt = HudBuilder.CreateRect(scorePill, "You", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            youLine = HudBuilder.CreateText(youRt, "You  0 pts", 17f, FontStyles.Bold, HudDesignSystem.TextPrimary, TextAlignmentOptions.MidlineRight);
            youRt.gameObject.AddComponent<LayoutElement>().minWidth = 40f;
            RegisterScalableText(youLine, 17f);

            var oppRt = HudBuilder.CreateRect(scorePill, "Opponent", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            oppLine = HudBuilder.CreateText(oppRt, "Opponent  0 pts", 13f, FontStyles.Normal, HudDesignSystem.TextSecondary, TextAlignmentOptions.MidlineRight);
            oppRt.gameObject.AddComponent<LayoutElement>().minWidth = 40f;
            RegisterScalableText(oppLine, 13f);
        }

        private void RegisterScalableText(TextMeshProUGUI text, float baseSize)
        {
            if (text != null)
                scalableTexts.Add((text, baseSize));
        }

        private void BuildContextPill(RectTransform safe)
        {
            var pill = HudBuilder.CreatePill(
                safe,
                "Context",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, HudDesignSystem.DockHeight + 28f),
                320f);
            pill.sizeDelta = new Vector2(680f, HudDesignSystem.PillHeight);
            var fitter = pill.GetComponent<ContentSizeFitter>();
            if (fitter != null)
            {
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            contextLine = HudBuilder.AddPillText(pill, "Tap a tile or drag from your rack.", 16f, HudDesignSystem.TextSecondary);
            contextLine.textWrappingMode = TextWrappingModes.Normal;
            contextLine.overflowMode = TextOverflowModes.Ellipsis;
            RegisterScalableText(contextLine, 16f);
        }

        private void BuildDock(RectTransform safe)
        {
            var dock = HudBuilder.CreateRect(
                safe,
                "Dock",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 8f),
                new Vector2(HudDesignSystem.MaxDockWidth, HudDesignSystem.DockHeight + 12f));

            HudBuilder.AddSlicedImage(dock, HudDesignSystem.PanelSprite, Color.white);

            var row = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(dock, false);
            var rowRt = row.GetComponent<RectTransform>();
            rowRt.anchorMin = Vector2.zero;
            rowRt.anchorMax = Vector2.one;
            rowRt.offsetMin = new Vector2(10f, 6f);
            rowRt.offsetMax = new Vector2(-10f, -6f);

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            // Guide / Log / Clear / AI live inside the Menu sheet — keeps the dock touch-friendly on phones.
            passButton = HudBuilder.CreateDockButton(row.transform, "Pass", false, OnPassOrEndTurnClicked);
            passButtonLabel = passButton.GetComponentInChildren<TextMeshProUGUI>();
            extraMoveButton = HudBuilder.CreateDockButton(row.transform, "Extra Move", false, OnExtraMoveClicked);
            reviveButton = HudBuilder.CreateDockButton(row.transform, "Revive", false, () =>
                GameInputController.Instance?.BeginMomentumMode(MomentumSpendMode.Revive));
            freezeButton = HudBuilder.CreateDockButton(row.transform, "Freeze", false, () =>
                GameInputController.Instance?.BeginMomentumMode(MomentumSpendMode.Freeze));
            rotateButton = HudBuilder.CreateDockButton(row.transform, "Rotate", false, () =>
            {
                Player current = GameManager.Instance.GetCurrentPlayer();
                TileSelector.Instance?.TryRotateWheel(current, GameInputController.Instance.SelectedBoardPiece);
            });
            menuButton = HudBuilder.CreateDockButton(row.transform, "Menu", false, ToggleMenuSheet);
        }

        private static void OnExtraMoveClicked()
        {
            if (GameManager.Instance == null)
                return;

            Player current = GameManager.Instance.GetCurrentPlayer();
            if (!GameManager.Instance.TryGrantExtraMove(current))
                GameplayFeedback.Show("Can't buy Extra Move right now.");
        }

        private void ToggleMenuSheet()
        {
            if (menuSheet == null)
                return;

            bool open = !menuSheet.gameObject.activeSelf;
            menuSheet.gameObject.SetActive(open);
            if (open)
            {
                showGuide = false;
                showLog = false;
                RefreshMenuLabels();
            }
        }

        private static void OnPassOrEndTurnClicked()
        {
            if (GameManager.Instance == null)
                return;

            GameManager.Instance.PassTurn();
        }

        private void OnLogClicked()
        {
            showLog = !showLog;

            if (GameLogManager.Instance == null)
            {
                GameplayFeedback.Show("No game log yet.");
                return;
            }

            string path = GameLogManager.Instance.ExportMatchLog("hud-log");
            if (string.IsNullOrEmpty(path))
            {
                GameplayFeedback.Show("Couldn't save match log.");
                return;
            }

            string fileName = Path.GetFileName(path);
            GameplayFeedback.Show($"Saved {fileName}", 4f);
        }

        private void BuildToast(RectTransform safe)
        {
            var toast = HudBuilder.CreateRect(
                safe,
                "Toast",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, HudDesignSystem.DockHeight + 84f),
                new Vector2(560f, HudDesignSystem.PillHeight));

            HudBuilder.AddSlicedImage(toast, HudDesignSystem.PillSprite, new Color(0.22f, 0.1f, 0.12f, 0.92f));
            toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            toastGroup.blocksRaycasts = false;
            toastRect = toast;
            toastRestPosition = toast.anchoredPosition;

            var label = HudBuilder.CreateRect(toast, "Label", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            label.offsetMin = new Vector2(16f, 8f);
            label.offsetMax = new Vector2(-16f, -8f);
            toastLine = HudBuilder.CreateText(label, "", 16f, FontStyles.Bold, HudDesignSystem.Danger, TextAlignmentOptions.Center);
            RegisterScalableText(toastLine, 16f);
        }

        private void BuildSheets(RectTransform safe)
        {
            guideSheet = BuildSheet(safe, "GuideSheet", 360f,
                "Field guide",
                "Gold lines - harmony\nBlue dots - legal moves\nRed - captures\nVermillion - ports\n\n" +
                "Momentum: tap Extra Move before you move to spend M for a second move\n" +
                "Revive / Freeze spend Momentum and end the turn\n" +
                "Echo: at 10 revival points choose which Ghost flower returns\n" +
                "Wilt: idle flowers fade — Revive them with Momentum\n" +
                "Knotweed: adjacent enemy flowers are drained (cannot move)\n" +
                "Boat: tap flower to load, U / empty space to unload\n" +
                "Wheel: select then Rotate\n" +
                "Ring: close a harmony chain around Mid to win",
                out guideBody);
            RegisterScalableText(guideBody, 15f);

            logSheet = BuildSheet(safe, "LogSheet", 320f, "Move log", "", out logBody);
            RegisterScalableText(logBody, 15f);
        }

        private void BuildEchoChooser(RectTransform safe)
        {
            echoSheet = HudBuilder.CreateRect(
                safe,
                "EchoChooser",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, HudDesignSystem.DockHeight + 118f),
                new Vector2(HudDesignSystem.MaxDockWidth, 88f));

            HudBuilder.AddSlicedImage(echoSheet, HudDesignSystem.PanelSprite, Color.white, raycast: true);

            var titleRt = HudBuilder.CreateRect(
                echoSheet, "Title", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 28f));
            titleRt.offsetMin = new Vector2(14f, -28f);
            titleRt.offsetMax = new Vector2(-14f, -4f);
            echoTitle = HudBuilder.CreateText(
                titleRt, "Choose Echo", 16f, FontStyles.Bold, HudDesignSystem.TextPrimary, TextAlignmentOptions.MidlineLeft);
            RegisterScalableText(echoTitle, 16f);

            var rowGo = new GameObject("EchoButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowGo.transform.SetParent(echoSheet, false);
            echoButtonRow = rowGo.GetComponent<RectTransform>();
            echoButtonRow.anchorMin = new Vector2(0f, 0f);
            echoButtonRow.anchorMax = new Vector2(1f, 1f);
            echoButtonRow.offsetMin = new Vector2(10f, 8f);
            echoButtonRow.offsetMax = new Vector2(-10f, -32f);

            var layout = rowGo.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            echoSheet.gameObject.SetActive(false);
        }

        private static RectTransform BuildSheet(RectTransform safe, string name, float height, string title, string body, out TextMeshProUGUI bodyText)
        {
            var sheet = HudBuilder.CreateRect(
                safe,
                name,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, HudDesignSystem.DockHeight + 20f),
                new Vector2(HudDesignSystem.MaxDockWidth, height));

            HudBuilder.AddSlicedImage(sheet, HudDesignSystem.PanelSprite, Color.white, raycast: true);

            var titleRt = HudBuilder.CreateRect(sheet, "Title", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 36f));
            titleRt.offsetMin = new Vector2(16f, -36f);
            titleRt.offsetMax = new Vector2(-16f, 0f);
            HudBuilder.CreateText(titleRt, title, 18f, FontStyles.Bold, HudDesignSystem.TextPrimary, TextAlignmentOptions.MidlineLeft);

            var bodyRt = HudBuilder.CreateRect(sheet, "Body", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            bodyRt.offsetMin = new Vector2(16f, 12f);
            bodyRt.offsetMax = new Vector2(-16f, -44f);
            bodyText = HudBuilder.CreateText(bodyRt, body, 15f, FontStyles.Normal, HudDesignSystem.TextSecondary, TextAlignmentOptions.TopLeft);

            sheet.gameObject.SetActive(false);
            return sheet;
        }

        private void BuildMenuSheet(RectTransform safe)
        {
            menuSheet = HudBuilder.CreateRect(
                safe,
                "MenuSheet",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, HudDesignSystem.DockHeight + 20f),
                new Vector2(420f, 720f));

            HudBuilder.AddSlicedImage(menuSheet, HudDesignSystem.PanelSprite, Color.white, raycast: true);

            var titleRt = HudBuilder.CreateRect(menuSheet, "Title", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 36f));
            titleRt.offsetMin = new Vector2(16f, -36f);
            titleRt.offsetMax = new Vector2(-16f, 0f);
            var menuTitle = HudBuilder.CreateText(titleRt, "Menu", 18f, FontStyles.Bold, HudDesignSystem.TextPrimary, TextAlignmentOptions.MidlineLeft);
            RegisterScalableText(menuTitle, 18f);

            var listGo = new GameObject("MenuList", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listGo.transform.SetParent(menuSheet, false);
            var listRt = listGo.GetComponent<RectTransform>();
            listRt.anchorMin = Vector2.zero;
            listRt.anchorMax = Vector2.one;
            listRt.offsetMin = new Vector2(14f, 14f);
            listRt.offsetMax = new Vector2(-14f, -40f);

            var listLayout = listGo.GetComponent<VerticalLayoutGroup>();
            listLayout.spacing = 8f;
            listLayout.childAlignment = TextAnchor.UpperCenter;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

            AddMenuToggleRow(listGo.transform, "Mute", () => GameSession.Muted ? "On" : "Off", () => GameSession.Muted = !GameSession.Muted);
            AddMenuSliderRow(listGo.transform, "Volume");
            AddMenuToggleRow(listGo.transform, "Colorblind Assist", () => GameSession.ColorblindAssist ? "On" : "Off", () => GameSession.ColorblindAssist = !GameSession.ColorblindAssist);
            AddMenuToggleRow(listGo.transform, "Reduce Motion", () => GameSession.ReduceMotion ? "On" : "Off", () => GameSession.ReduceMotion = !GameSession.ReduceMotion);
            AddMenuToggleRow(listGo.transform, "Larger HUD Text", () => GameSession.LargerHudType ? "On" : "Off", () => GameSession.LargerHudType = !GameSession.LargerHudType);
            AddMenuActionRow(listGo.transform, "Field Guide", () =>
            {
                showGuide = !showGuide;
                menuSheet.gameObject.SetActive(false);
            });
            AddMenuActionRow(listGo.transform, "Move Log", () =>
            {
                OnLogClicked();
                menuSheet.gameObject.SetActive(false);
            });
            AddMenuActionRow(listGo.transform, "Clear Selection", () => GameInputController.Instance?.ClearSelection());
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            AddMenuActionRow(listGo.transform, "Toggle AI Opponent", () =>
                AiController.Instance?.SetAiEnabled(!AiController.Instance.IsAiEnabled));
#endif
            AddMenuActionRow(listGo.transform, "Forfeit Match", OnForfeitClicked);
            AddMenuActionRow(listGo.transform, "Return to Title", OnReturnToTitleClicked);
            AddMenuActionRow(listGo.transform, "Close", () => menuSheet.gameObject.SetActive(false));

            menuSheet.gameObject.SetActive(false);
        }

        private void AddMenuToggleRow(Transform parent, string title, Func<string> stateText, Action onToggle)
        {
            var button = HudBuilder.CreateDockButton(parent, $"{title}: {stateText()}", false, () =>
            {
                onToggle();
                RefreshMenuLabels();
            });
            var text = button.GetComponentInChildren<TextMeshProUGUI>();
            RegisterScalableText(text, 17f);
            menuToggleLabels.Add((text, () => $"{title}: {stateText()}"));
        }

        private void AddMenuActionRow(Transform parent, string label, Action onClick)
        {
            var button = HudBuilder.CreateDockButton(parent, label, false, onClick);
            RegisterScalableText(button.GetComponentInChildren<TextMeshProUGUI>(), 17f);
        }

        private void AddMenuSliderRow(Transform parent, string label)
        {
            var rowGo = new GameObject($"{label}Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            rowGo.transform.SetParent(parent, false);
            var rowLayoutElement = rowGo.GetComponent<LayoutElement>();
            rowLayoutElement.preferredHeight = HudDesignSystem.TouchMin;
            rowLayoutElement.minHeight = HudDesignSystem.TouchMin;

            var rowLayout = rowGo.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 10f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            var labelRt = HudBuilder.CreateRect(rowGo.transform, "Label", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var labelText = HudBuilder.CreateText(labelRt, label, 16f, FontStyles.Normal, HudDesignSystem.TextSecondary, TextAlignmentOptions.MidlineLeft);
            labelRt.gameObject.AddComponent<LayoutElement>().minWidth = 90f;
            RegisterScalableText(labelText, 16f);

            var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
            sliderGo.transform.SetParent(rowGo.transform, false);
            sliderGo.GetComponent<LayoutElement>().flexibleWidth = 1f;

            var slider = sliderGo.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = GameSession.MasterVolume;
            slider.onValueChanged.AddListener(v =>
            {
                GameSession.MasterVolume = v;
                if (GameSession.Muted && v > 0.01f)
                    GameSession.Muted = false;
                RefreshMenuLabels();
            });

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(sliderGo.transform, false);
            StretchFullRect(bg.GetComponent<RectTransform>());
            bg.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            StretchFullRect(fillArea.GetComponent<RectTransform>());
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            StretchFullRect(fill.GetComponent<RectTransform>());
            fill.GetComponent<Image>().color = HudDesignSystem.Accent;
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = fill.GetComponent<Image>();

            volumeMenuSlider = slider;
        }

        private static void StretchFullRect(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void RefreshMenuLabels()
        {
            foreach (var entry in menuToggleLabels)
            {
                if (entry.text != null)
                    entry.text.text = entry.label();
            }

            if (volumeMenuSlider != null && Mathf.Abs(volumeMenuSlider.value - GameSession.MasterVolume) > 0.001f)
                volumeMenuSlider.SetValueWithoutNotify(GameSession.MasterVolume);
        }

        private void OnForfeitClicked()
        {
            if (Time.unscaledTime < forfeitArmedUntil)
            {
                menuSheet.gameObject.SetActive(false);
                Player forfeiter = ResolveLocalForfeitPlayer();
                GameManager.Instance?.ForfeitMatch(forfeiter);
                forfeitArmedUntil = 0f;
                return;
            }

            forfeitArmedUntil = Time.unscaledTime + 3f;
            GameplayFeedback.Show("Tap Forfeit Match again to confirm.", 3f);
        }

        private static Player ResolveLocalForfeitPlayer()
        {
            // AI matches: only the human (Host) can forfeit. Hotseat: whoever holds the device.
            if (AiController.Instance != null && AiController.Instance.IsAiEnabled)
                return Player.Host;

            return GameManager.Instance != null ? GameManager.Instance.GetCurrentPlayer() : Player.Host;
        }

        private void OnReturnToTitleClicked()
        {
            menuSheet.gameObject.SetActive(false);
            GameResetService.ResetMatch();
            TitleMenu.Instance?.Show();
        }

        private void BuildPassScrim(RectTransform safe)
        {
            passScrimSheet = HudBuilder.Stretch(safe, "PassScrim");
            HudBuilder.AddSlicedImage(passScrimSheet, null, HudDesignSystem.Scrim, raycast: true);

            var card = HudBuilder.CreateRect(
                passScrimSheet, "Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(380f, 220f));
            HudBuilder.AddSlicedImage(card, HudDesignSystem.PanelSprite, Color.white);

            var textRt = HudBuilder.CreateRect(card, "Text", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            textRt.offsetMin = new Vector2(20f, 64f);
            textRt.offsetMax = new Vector2(-20f, -16f);
            passScrimText = HudBuilder.CreateText(
                textRt, "Pass the device", 20f, FontStyles.Bold, HudDesignSystem.TextPrimary, TextAlignmentOptions.Center);
            RegisterScalableText(passScrimText, 20f);

            var readyButton = HudBuilder.CreateDockButton(card, "I'm ready", true, HidePassScrim);
            var readyRt = readyButton.GetComponent<RectTransform>();
            readyRt.anchorMin = new Vector2(0.5f, 0f);
            readyRt.anchorMax = new Vector2(0.5f, 0f);
            readyRt.pivot = new Vector2(0.5f, 0f);
            readyRt.anchoredPosition = new Vector2(0f, 16f);
            readyRt.sizeDelta = new Vector2(180f, HudDesignSystem.TouchMin);

            passScrimSheet.gameObject.SetActive(false);
        }

        private void ShowPassScrim(Player upcomingPlayer)
        {
            if (passScrimSheet == null)
                return;

            string who = upcomingPlayer == Player.Host ? "Host" : "Opponent";
            passScrimText.text = $"Pass the device\n\n{who}'s turn";
            passScrimSheet.gameObject.SetActive(true);
            IsPassScrimShowing = true;
        }

        private void HidePassScrim()
        {
            if (passScrimSheet == null)
                return;

            passScrimSheet.gameObject.SetActive(false);
            IsPassScrimShowing = false;
        }

        private void MaybeShowHotseatScrim(Player current, int turn)
        {
            bool hotseat = AiController.Instance == null || !AiController.Instance.IsAiEnabled;
            if (!hotseat || GameStateManager.Instance == null || GameStateManager.Instance.IsEndPhase())
            {
                lastHotseatTurnNumber = turn;
                return;
            }

            if (lastHotseatTurnNumber < 0 || turn < lastHotseatTurnNumber)
            {
                // First observation, or a new match / reset restarted the turn counter.
                lastHotseatTurnNumber = turn;
                return;
            }

            if (turn != lastHotseatTurnNumber)
            {
                lastHotseatTurnNumber = turn;
                ShowPassScrim(current);
            }
        }

        private void ApplyHudTypeScaleIfChanged()
        {
            bool larger = GameSession.LargerHudType;
            if (larger == lastAppliedLargerHudType)
                return;

            float scale = larger ? 1.25f : 1f;
            foreach (var entry in scalableTexts)
            {
                if (entry.text != null)
                    entry.text.fontSize = entry.baseSize * scale;
            }

            lastAppliedLargerHudType = larger;
        }

        private void BuildEndScreen(RectTransform safe)
        {
            var end = HudBuilder.Stretch(safe, "EndScreen");
            HudBuilder.AddSlicedImage(end, null, HudDesignSystem.Scrim, raycast: true);

            var card = HudBuilder.CreateRect(
                end,
                "Card",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(480f, 320f));
            HudBuilder.AddSlicedImage(card, HudDesignSystem.PanelSprite, Color.white);

            var titleRt = HudBuilder.CreateRect(card, "Title", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 56f));
            titleRt.offsetMin = new Vector2(24f, -56f);
            titleRt.offsetMax = new Vector2(-24f, -8f);
            endTitle = HudBuilder.CreateText(titleRt, "Match complete", 28f, FontStyles.Bold, HudDesignSystem.Accent, TextAlignmentOptions.Top);
            RegisterScalableText(endTitle, 28f);

            var bodyRt = HudBuilder.CreateRect(card, "Body", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            bodyRt.offsetMin = new Vector2(24f, 80f);
            bodyRt.offsetMax = new Vector2(-24f, -8f);
            endBody = HudBuilder.CreateText(bodyRt, "", 17f, FontStyles.Normal, HudDesignSystem.TextPrimary, TextAlignmentOptions.Top);
            RegisterScalableText(endBody, 17f);

            playAgainButton = HudBuilder.CreateDockButton(card, "Play again", true, () => GameResetService.ResetMatch());
            var againRt = playAgainButton.GetComponent<RectTransform>();
            againRt.anchorMin = new Vector2(0.5f, 0f);
            againRt.anchorMax = new Vector2(0.5f, 0f);
            againRt.pivot = new Vector2(0.5f, 0f);
            againRt.anchoredPosition = new Vector2(-108f, 16f);
            againRt.sizeDelta = new Vector2(196f, HudDesignSystem.TouchMin);

            titleButton = HudBuilder.CreateDockButton(card, "Title", false, () =>
            {
                GameResetService.ResetMatch();
                TitleMenu.Instance?.Show();
            });
            var titleButtonRt = titleButton.GetComponent<RectTransform>();
            titleButtonRt.anchorMin = new Vector2(0.5f, 0f);
            titleButtonRt.anchorMax = new Vector2(0.5f, 0f);
            titleButtonRt.pivot = new Vector2(0.5f, 0f);
            titleButtonRt.anchoredPosition = new Vector2(108f, 16f);
            titleButtonRt.sizeDelta = new Vector2(196f, HudDesignSystem.TouchMin);

            endScreenGroup = end.gameObject.AddComponent<CanvasGroup>();
            endScreenGroup.alpha = 0f;
            endScreenGroup.blocksRaycasts = false;
            end.gameObject.SetActive(false);
        }

        private void RefreshHud()
        {
            Player current = GameManager.Instance.GetCurrentPlayer();
            bool humanTurn = AiController.Instance == null || !AiController.Instance.IsAiPlayer(current);
            bool bonusPending = GameManager.Instance.PendingBonusMove;

            string phase = GameStateManager.Instance != null ? FriendlyPhase() : "Setup";
            string season = SeasonManager.Instance != null ? SeasonManager.Instance.GetCurrentSeason().ToString() : "Spring";
            string holdNote = "";
            if (ReserveManager.Instance != null &&
                !ReserveManager.Instance.HasHoldReleased(current) &&
                ReserveManager.Instance.GetOnHoldCount(current) > 0)
            {
                holdNote = $" | Hold: {string.Join("/", ReserveManager.Instance.GetOnHold(current))}";
            }

            metaLine.text = humanTurn
                ? $"{phase} | {season} | Turn {GameManager.Instance.GetTurnNumber()} | > Your turn{holdNote}"
                : $"{phase} | {season} | Turn {GameManager.Instance.GetTurnNumber()} | Computer playing...{holdNote}";

            int hostScore = GameManager.Instance.GetLiveScore(Player.Host);
            int oppScore = GameManager.Instance.GetLiveScore(Player.Opponent);
            int hostHarmony = GameManager.Instance.CountHarmonizedPieces(Player.Host);
            int oppHarmony = GameManager.Instance.CountHarmonizedPieces(Player.Opponent);
            int hostMomentum = MomentumManager.Instance != null ? MomentumManager.Instance.GetMomentum(Player.Host) : 0;
            int oppMomentum = MomentumManager.Instance != null ? MomentumManager.Instance.GetMomentum(Player.Opponent) : 0;
            int hostEcho = EchoTileManager.Instance != null ? EchoTileManager.Instance.GetRevivalPoints(Player.Host) : 0;
            int oppEcho = EchoTileManager.Instance != null ? EchoTileManager.Instance.GetRevivalPoints(Player.Opponent) : 0;
            int hostRing = HarmonyRingDetector.GetRingProgress(Player.Host);
            int oppRing = HarmonyRingDetector.GetRingProgress(Player.Opponent);

            // You-first: your stats read full-size on top, opponent's are a quieter second line.
            youLine.text =
                $"You  {hostScore} pts · Momentum {hostMomentum} · Harmony {hostHarmony} · " +
                $"Ring {hostRing}/{HarmonyRingDetector.MinRingSize} · Echo {hostEcho}/10";
            oppLine.text =
                $"Opponent  {oppScore} pts · Momentum {oppMomentum} · Harmony {oppHarmony} · " +
                $"Ring {oppRing}/{HarmonyRingDetector.MinRingSize} · Echo {oppEcho}/10";

            GameCoach.Instance?.EvaluateHudState(current, humanTurn);
            MaybeShowHotseatScrim(current, GameManager.Instance.GetTurnNumber());

            NotifyRingThreat(Player.Host, hostRing);
            NotifyRingThreat(Player.Opponent, oppRing);

            contextLine.text = BuildContextMessage(current);

            bool canPass = humanTurn && !GameManager.Instance.HasLegalActions(current);
            int momentum = MomentumManager.Instance != null ? MomentumManager.Instance.GetMomentum(current) : 0;
            if (passButtonLabel != null)
                passButtonLabel.text = "Pass";
            passButton.Interactable = humanTurn && canPass;
            if (extraMoveButton != null)
            {
                extraMoveButton.Interactable = humanTurn && !bonusPending &&
                    GameManager.Instance.CanGrantExtraMove(current);
            }

            reviveButton.Interactable = humanTurn && !bonusPending && momentum > 0 &&
                GameInputController.GetMomentumTargets(current, MomentumSpendMode.Revive).Count > 0;
            freezeButton.Interactable = humanTurn && !bonusPending && momentum > 0 &&
                GameInputController.GetMomentumTargets(current, MomentumSpendMode.Freeze).Count > 0;
            rotateButton.Interactable = humanTurn && !bonusPending &&
                GameInputController.Instance?.SelectedBoardPiece != null &&
                GameInputController.Instance.SelectedBoardPiece.Type == PieceType.Wheel &&
                GameInputController.Instance.SelectedBoardPiece.Owner == current;

            guideSheet.gameObject.SetActive(showGuide);
            logSheet.gameObject.SetActive(showLog);

            if (showLog && GameLogManager.Instance != null)
            {
                var entries = GameLogManager.Instance.GetEntries();
                var builder = new StringBuilder();
                int start = Mathf.Max(0, entries.Count - 10);
                for (int i = start; i < entries.Count; i++)
                    builder.AppendLine(entries[i].ToString());
                logBody.text = builder.ToString();
            }

            int turn = GameManager.Instance.GetTurnNumber();
            if ((current != lastBannerPlayer || turn != lastBannerTurn) && turn > 1)
                UiAudio.Instance?.PlayNotify();
            lastBannerPlayer = current;
            lastBannerTurn = turn;
        }

        private void RefreshEchoChooser()
        {
            if (echoSheet == null || EchoTileManager.Instance == null)
                return;

            bool show = EchoTileManager.Instance.HasPendingEchoChoice;
            echoSheet.gameObject.SetActive(show);
            if (!show)
            {
                lastEchoSignature = "";
                return;
            }

            var types = EchoTileManager.Instance.PendingEchoTypes;
            var signature = new StringBuilder();
            signature.Append(EchoTileManager.Instance.PendingEchoPlayer).Append(':');
            foreach (PieceType type in types)
                signature.Append(type).Append(',');

            string sig = signature.ToString();
            if (sig == lastEchoSignature)
                return;

            lastEchoSignature = sig;
            if (echoTitle != null)
                echoTitle.text = "Choose Echo flower";

            for (int i = echoButtonRow.childCount - 1; i >= 0; i--)
                Destroy(echoButtonRow.GetChild(i).gameObject);

            foreach (PieceType type in types)
            {
                PieceType chosen = type;
                HudBuilder.CreateDockButton(echoButtonRow, FriendlyEchoName(chosen), true, () =>
                {
                    EchoTileManager.Instance?.ConfirmPendingEcho(chosen);
                    lastEchoSignature = "";
                });
            }
        }

        private void NotifyRingThreat(Player player, int ringProgress)
        {
            int threat = HarmonyRingDetector.MinRingSize - 1;
            ref int last = ref (player == Player.Host ? ref lastHostRingThreat : ref lastOppRingThreat);
            if (ringProgress == threat && last != threat)
            {
                string who = player == Player.Host ? "You are" : "The computer is";
                GameplayFeedback.Show($"{who} one harmony from a winning ring!", 4.5f);
                UiAudio.Instance?.PlayNotify();
            }

            last = ringProgress;
        }

        private static string FriendlyEchoName(PieceType type) =>
            type switch
            {
                PieceType.Chrysanthemum => "Chrys",
                PieceType.Rhododendron => "Rhodo",
                _ => type.ToString()
            };

        private void RefreshToast()
        {
            if (toastGroup == null)
                return;

            if (!GameplayFeedback.TryGetMessage(out string message))
            {
                toastGroup.alpha = Mathf.MoveTowards(toastGroup.alpha, 0f, Time.unscaledDeltaTime * 5f);
                if (toastRect != null)
                    toastRect.anchoredPosition = toastRestPosition;
                return;
            }

            toastLine.text = message;
            toastGroup.alpha = GameplayFeedback.GetDisplayAlpha();

            if (toastRect == null)
                return;

            if (GameSession.ReduceMotion)
            {
                // Reduce motion: fade only, no slide or spring pop.
                toastRect.anchoredPosition = toastRestPosition;
                toastRect.localScale = Vector3.one;
                return;
            }

            float slide = GameplayFeedback.GetSlideOffset();
            toastRect.anchoredPosition = toastRestPosition + new Vector2(0f, -slide);
            toastRect.localScale = Vector3.one * GameplayFeedback.GetPopScale();
        }

        private void ShowEndScreen()
        {
            if (GameEndManager.Instance == null || endScreenGroup == null)
                return;

            endScreenGroup.gameObject.SetActive(true);
            endScreenGroup.alpha = 1f;
            endScreenGroup.blocksRaycasts = true;

            Player? winner = GameEndManager.Instance.Winner;
            bool youWon = winner.HasValue && winner.Value == Player.Host;
            bool vsAi = AiController.Instance != null && AiController.Instance.IsAiEnabled;

            endTitle.text = !winner.HasValue
                ? "A quiet draw"
                : youWon ? "Victory!" : (vsAi ? "Defeat" : $"{winner.Value} wins");

            string outcome = !winner.HasValue
                ? "Neither garden closed a ring — the match ends in balance."
                : youWon
                    ? "You win this match."
                    : vsAi
                        ? "The computer wins this match."
                        : $"{winner.Value} wins this match.";

            // Guarantee a study log even if resolve somehow skipped export.
            string logPath = GameLogManager.Instance?.EnsureMatchEndExport();
            string logNote = string.IsNullOrEmpty(logPath)
                ? "Match log could not be saved."
                : $"Log: {Path.GetFileName(logPath)}";

            endBody.text =
                $"{GameEndManager.Instance.WinReason}\n\n{outcome}\n\n" +
                $"Host {GameEndManager.Instance.HostScore}  |  Guest {GameEndManager.Instance.OpponentScore}\n\n{logNote}";
        }

        private void HideEndScreen()
        {
            if (endScreenGroup == null || !endScreenGroup.gameObject.activeSelf)
                return;

            endScreenGroup.gameObject.SetActive(false);
            endScreenGroup.blocksRaycasts = false;
        }

        private static string FriendlyPhase()
        {
            return GameStateManager.Instance.GetCurrentPhase() switch
            {
                GamePhase.Spring => "Spring",
                GamePhase.Play => "Play",
                GamePhase.End => "End",
                _ => GameStateManager.Instance.GetCurrentPhase().ToString()
            };
        }

        private static string BuildContextMessage(Player current)
        {
            if (EchoTileManager.Instance != null && EchoTileManager.Instance.HasPendingEchoChoice)
                return "Choose which Ghost Echo flower to summon.";

            if (GameManager.Instance != null && GameManager.Instance.PendingBonusMove)
                return "Extra Move active — move a second tile.";

            if (GameStateManager.Instance != null && GameStateManager.Instance.IsSpringPhase())
            {
                PieceType? drawn = ReserveManager.Instance.GetSpringDrawnFlower(current);
                if (!drawn.HasValue)
                    return "Drawing spring tile...";

                int remaining = GameManager.Instance.GetSpringPlacementsRemaining();
                int total = PieceRules.SpringFlowerCount;
                return $"Place {drawn.Value} on your side | {remaining} of {total} left.";
            }

            if (GameInputController.Instance?.SelectedBoardPiece != null)
            {
                var selected = GameInputController.Instance.SelectedBoardPiece;
                if (selected.Type == PieceType.Boat)
                {
                    return BoatManager.Instance != null && BoatManager.Instance.HasCargo(selected)
                        ? "Move the Boat with cargo, tap empty adjacent to unload, or press U."
                        : "Move the Boat, or tap an adjacent flower to load cargo.";
                }

                return selected.Type == PieceType.Wheel ? "Move the wheel or tap Rotate." : $"Move your {selected.Type}.";
            }

            if (BoardPieceDragController.Instance != null && BoardPieceDragController.Instance.IsDragging)
                return "Release on a blue marker.";

            if (HandTrayController.Instance != null && HandTrayController.Instance.IsDragging)
                return "Release to place from your rack.";

            if (GameInputController.Instance?.MomentumMode == MomentumSpendMode.Revive)
                return "Select a wilted tile to revive (costs 1 Momentum).";

            if (GameInputController.Instance?.MomentumMode == MomentumSpendMode.Freeze)
                return "Select a tile to Freeze wilt for a turn (costs 1 Momentum).";

            int hostRing = HarmonyRingDetector.GetRingProgress(Player.Host);
            int oppRing = HarmonyRingDetector.GetRingProgress(Player.Opponent);
            int threat = HarmonyRingDetector.MinRingSize - 1;
            if (hostRing >= threat)
                return hostRing >= HarmonyRingDetector.MinRingSize
                    ? "Ring complete — match ending."
                    : "One harmony from your winning ring!";
            if (oppRing >= threat)
                return "Computer is one harmony from a winning ring — disrupt it!";

            int momentum = MomentumManager.Instance != null ? MomentumManager.Instance.GetMomentum(current) : 0;
            int echoPts = EchoTileManager.Instance != null ? EchoTileManager.Instance.GetRevivalPoints(current) : 0;
            if (momentum > 0)
                return $"You have {momentum} Momentum — tap Extra Move for a second move, or Revive / Freeze.";
            if (echoPts > 0)
                return $"Echo {echoPts}/10 — at 10 you choose a Ghost flower.";

            int wiltedCount = CountWiltedTiles(current);
            if (wiltedCount > 0)
            {
                return wiltedCount == 1
                    ? "One of your tiles is wilting — Revive or Freeze it before it fades."
                    : $"{wiltedCount} of your tiles are wilting — Revive or Freeze them before they fade.";
            }

            if (!GameManager.Instance.HasLegalActions(current))
                return "No moves left - Pass is ready.";

            if (AiController.Instance != null && AiController.Instance.IsAiPlayer(current))
                return "Waiting for the computer...";

            return "Tap a tile or drag from your rack.";
        }

        private static int CountWiltedTiles(Player player)
        {
            if (BoardManager.Instance == null)
                return 0;

            int count = 0;
            foreach (var piece in BoardManager.Instance.GetAllPieces())
            {
                if (piece != null && piece.Owner == player && piece.WiltLevel > 0)
                    count++;
            }

            return count;
        }
    }
}
