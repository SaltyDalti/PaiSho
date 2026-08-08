using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;
using PaiSho.UI;

namespace PaiSho.Game
{
    /// <summary>Title overlay: New Game (AI / hotseat), Settings, Quit.</summary>
    [DefaultExecutionOrder(40)]
    public class TitleMenu : MonoBehaviour
    {
        public static TitleMenu Instance { get; private set; }

        private CanvasGroup rootGroup;
        private RectTransform settingsPanel;
        private Slider volumeSlider;
        private TextMeshProUGUI muteLabel;
        private TextMeshProUGUI colorblindLabel;
        private TextMeshProUGUI reduceMotionLabel;
        private TextMeshProUGUI largerHudLabel;

        public bool IsOpen => rootGroup != null && rootGroup.blocksRaycasts;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureEventSystem();
            HudDesignSystem.EnsureDefaultFont();
            GameSession.ApplyAudio();
            BuildUi();
            SetOpen(GameSession.ShowTitleOnBoot && !GameSession.MatchStarted);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static IEnumerator WaitUntilMatchRequested()
        {
            if (!GameSession.ShowTitleOnBoot || GameSession.MatchStarted)
                yield break;

            while (Instance != null && Instance.IsOpen)
            {
                // Self-play / headless tools should not block on the title overlay.
                if (HeadlessActionExecutor.IsActive)
                {
                    GameSession.MarkMatchStarted();
                    Instance.SetOpen(false);
                    yield break;
                }

                yield return null;
            }
        }

        public void Show()
        {
            GameSession.ResetForTitle();
            SetOpen(true);
        }

        public void DismissForMatch()
        {
            GameSession.MarkMatchStarted();
            SetOpen(false);
        }

        private void SetOpen(bool open)
        {
            if (rootGroup == null)
                return;

            rootGroup.alpha = open ? 1f : 0f;
            rootGroup.blocksRaycasts = open;
            rootGroup.interactable = open;
            if (settingsPanel != null)
                settingsPanel.gameObject.SetActive(false);
        }

        private void StartMatch(bool aiEnabled)
        {
            GameSession.AiEnabled = aiEnabled;
            GameSession.MarkMatchStarted();
            SetOpen(false);
            UiAudio.Instance?.PlayConfirm();
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildUi()
        {
            Canvas canvas = HudBuilder.EnsureCanvas(transform, "TitleHud", sortOrder: 400);
            var root = HudBuilder.Stretch(canvas.transform, "TitleRoot");
            rootGroup = root.gameObject.AddComponent<CanvasGroup>();

            var dim = HudBuilder.CreateRect(
                root, "Dim", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            HudBuilder.AddSlicedImage(dim, HudDesignSystem.WhiteSprite, new Color(0.06f, 0.05f, 0.04f, 0.72f), raycast: true);

            var panel = HudBuilder.CreateRect(
                root,
                "TitlePanel",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(420f, 440f));
            HudBuilder.AddSlicedImage(panel, HudDesignSystem.PanelSprite, Color.white, raycast: true);

            AddCenteredLabel(panel, "Title", "Pai Sho", 42f, FontStyles.Bold,
                HudDesignSystem.TextPrimary, new Vector2(0f, 0.78f), new Vector2(1f, 0.95f));
            AddCenteredLabel(panel, "Subtitle", "A garden of harmony", 18f, FontStyles.Normal,
                HudDesignSystem.TextSecondary, new Vector2(0f, 0.68f), new Vector2(1f, 0.78f));

            float y = 0.52f;
            PlaceMenuButton(panel, "New Game vs Computer", ref y, () => StartMatch(true));
            PlaceMenuButton(panel, "New Game Hotseat", ref y, () => StartMatch(false));
            PlaceMenuButton(panel, "Settings", ref y, ToggleSettings);
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
            PlaceMenuButton(panel, "Quit", ref y, () =>
            {
                UiAudio.Instance?.PlayBack();
                Application.Quit();
            });
#endif

            BuildSettingsPanel(root);
            RefreshSettingsLabels();
        }

        private static void AddCenteredLabel(
            RectTransform parent,
            string name,
            string text,
            float size,
            FontStyles style,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var rt = HudBuilder.CreateRect(
                parent, name, anchorMin, anchorMax, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rt.offsetMin = new Vector2(16f, 0f);
            rt.offsetMax = new Vector2(-16f, 0f);
            HudBuilder.CreateText(rt, text, size, style, color, TextAlignmentOptions.Center);
        }

        private static void PlaceMenuButton(RectTransform parent, string label, ref float y, System.Action onClick)
        {
            var button = HudBuilder.CreateDockButton(parent, label, true, onClick);
            var layout = button.GetComponent<LayoutElement>();
            if (layout != null)
                layout.ignoreLayout = true;

            var rt = button.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.12f, y);
            rt.anchorMax = new Vector2(0.88f, y + 0.1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            y -= 0.12f;
        }

        private void BuildSettingsPanel(RectTransform root)
        {
            settingsPanel = HudBuilder.CreateRect(
                root,
                "SettingsPanel",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -20f),
                new Vector2(380f, 520f));
            HudBuilder.AddSlicedImage(settingsPanel, HudDesignSystem.PanelSprite, Color.white, raycast: true);
            settingsPanel.gameObject.SetActive(false);

            AddCenteredLabel(settingsPanel, "Heading", "Settings", 24f, FontStyles.Bold,
                HudDesignSystem.TextPrimary, new Vector2(0f, 0.93f), new Vector2(1f, 1f));

            float y = 0.86f;
            const float buttonHeight = 0.1f;
            const float gap = 0.015f;

            muteLabel = PlaceToggleRow(settingsPanel, "Mute", () => GameSession.Muted ? "On" : "Off", ref y, buttonHeight, gap,
                () => GameSession.Muted = !GameSession.Muted);

            AddCenteredLabel(settingsPanel, "VolumeLabel", "Volume", 15f, FontStyles.Normal,
                HudDesignSystem.TextSecondary, new Vector2(0f, y - 0.045f), new Vector2(1f, y));
            y -= 0.045f + gap;

            var sliderGo = new GameObject("VolumeSlider", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(settingsPanel, false);
            var sRt = sliderGo.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.12f, y - buttonHeight);
            sRt.anchorMax = new Vector2(0.88f, y);
            sRt.offsetMin = Vector2.zero;
            sRt.offsetMax = Vector2.zero;
            y -= buttonHeight + gap * 2f;

            volumeSlider = sliderGo.GetComponent<Slider>();
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = GameSession.MasterVolume;
            volumeSlider.onValueChanged.AddListener(v =>
            {
                GameSession.MasterVolume = v;
                if (GameSession.Muted && v > 0.01f)
                    GameSession.Muted = false;
                RefreshSettingsLabels();
            });

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(sliderGo.transform, false);
            StretchFull(bg.GetComponent<RectTransform>());
            bg.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            StretchFull(fillArea.GetComponent<RectTransform>());
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            StretchFull(fill.GetComponent<RectTransform>());
            fill.GetComponent<Image>().color = HudDesignSystem.Accent;
            volumeSlider.fillRect = fill.GetComponent<RectTransform>();
            volumeSlider.targetGraphic = fill.GetComponent<Image>();

            AddCenteredLabel(settingsPanel, "AccessibilityHeading", "Accessibility", 16f, FontStyles.Bold,
                HudDesignSystem.TextPrimary, new Vector2(0f, y - 0.045f), new Vector2(1f, y));
            y -= 0.045f + gap;

            colorblindLabel = PlaceToggleRow(settingsPanel, "Colorblind Assist", () => GameSession.ColorblindAssist ? "On" : "Off", ref y, buttonHeight, gap,
                () => GameSession.ColorblindAssist = !GameSession.ColorblindAssist);
            reduceMotionLabel = PlaceToggleRow(settingsPanel, "Reduce Motion", () => GameSession.ReduceMotion ? "On" : "Off", ref y, buttonHeight, gap,
                () => GameSession.ReduceMotion = !GameSession.ReduceMotion);
            largerHudLabel = PlaceToggleRow(settingsPanel, "Larger HUD Text", () => GameSession.LargerHudType ? "On" : "Off", ref y, buttonHeight, gap,
                () => GameSession.LargerHudType = !GameSession.LargerHudType);

            PlaceAbsoluteButton(settingsPanel, "Close", new Vector2(0.25f, y - buttonHeight), new Vector2(0.75f, y), ToggleSettings);
        }

        private TextMeshProUGUI PlaceToggleRow(
            RectTransform parent, string title, System.Func<string> stateText, ref float y, float buttonHeight, float gap, System.Action onToggle)
        {
            HudButton button = PlaceAbsoluteButton(parent, $"{title}: {stateText()}", new Vector2(0.12f, y - buttonHeight), new Vector2(0.88f, y), () =>
            {
                onToggle();
                RefreshSettingsLabels();
            });
            y -= buttonHeight + gap;
            return button.GetComponentInChildren<TextMeshProUGUI>();
        }

        private static HudButton PlaceAbsoluteButton(
            RectTransform parent, string label, Vector2 anchorMin, Vector2 anchorMax, System.Action onClick)
        {
            var button = HudBuilder.CreateDockButton(parent, label, true, onClick);
            var layout = button.GetComponent<LayoutElement>();
            if (layout != null)
                layout.ignoreLayout = true;

            var rt = button.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return button;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void ToggleSettings()
        {
            if (settingsPanel == null)
                return;

            bool open = !settingsPanel.gameObject.activeSelf;
            settingsPanel.gameObject.SetActive(open);
            RefreshSettingsLabels();
            UiAudio.Instance?.PlayConfirm();
        }

        private void RefreshSettingsLabels()
        {
            if (muteLabel != null)
                muteLabel.text = GameSession.Muted ? "Mute: On" : "Mute: Off";
            if (colorblindLabel != null)
                colorblindLabel.text = GameSession.ColorblindAssist ? "Colorblind Assist: On" : "Colorblind Assist: Off";
            if (reduceMotionLabel != null)
                reduceMotionLabel.text = GameSession.ReduceMotion ? "Reduce Motion: On" : "Reduce Motion: Off";
            if (largerHudLabel != null)
                largerHudLabel.text = GameSession.LargerHudType ? "Larger HUD Text: On" : "Larger HUD Text: Off";
            if (volumeSlider != null && Mathf.Abs(volumeSlider.value - GameSession.MasterVolume) > 0.001f)
                volumeSlider.SetValueWithoutNotify(GameSession.MasterVolume);
        }
    }
}
