using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PaiSho;
using PaiSho.Game;

namespace PaiSho.UI
{
    public static class HudBuilder
    {
        public static Canvas EnsureCanvas(Transform parent, string name, int sortOrder = 200)
        {
            HudDesignSystem.EnsureDefaultFont();

            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one;

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            canvas.pixelPerfect = false;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(HudDesignSystem.ReferenceWidth, HudDesignSystem.ReferenceHeight);
            scaler.matchWidthOrHeight = 0.55f;

            return canvas;
        }

        public static RectTransform Stretch(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static RectTransform CreateRect(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
            return rt;
        }

        public static Image AddSlicedImage(RectTransform rect, Sprite sprite, Color color, bool raycast = false)
        {
            var image = rect.gameObject.GetComponent<Image>();
            if (image == null)
                image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite != null ? sprite : HudDesignSystem.WhiteSprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        public static TextMeshProUGUI CreateText(
            RectTransform rect,
            string text,
            float fontSize,
            FontStyles style,
            Color color,
            TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            var tmp = rect.gameObject.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
                tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();

            if (HudDesignSystem.Font != null)
                tmp.font = HudDesignSystem.Font;

            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.richText = false;
            tmp.raycastTarget = false;
            tmp.enableAutoSizing = false;
            tmp.margin = Vector4.zero;
            return tmp;
        }

        public static RectTransform CreatePill(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, float minWidth = 0f)
        {
            var rt = CreateRect(parent, name, anchor, anchor, pivot, pos, new Vector2(minWidth, HudDesignSystem.PillHeight));
            AddSlicedImage(rt, HudDesignSystem.PillSprite, Color.white);
            var fitter = rt.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 8, 8);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return rt;
        }

        public static TextMeshProUGUI AddPillText(RectTransform pill, string text, float size, Color color, FontStyles style = FontStyles.Normal)
        {
            var label = CreateRect(pill, "Label", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var tmp = CreateText(label, text, size, style, color, TextAlignmentOptions.Center);
            var layout = label.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = 40f;
            return tmp;
        }

        public static HudButton CreateDockButton(Transform parent, string label, bool primary, Action onClick)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(HudButton), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            // Touch targets stay >= HudDesignSystem.TouchMin even where the dock itself is shorter.
            float buttonHeight = Mathf.Max(HudDesignSystem.DockHeight - 10f, HudDesignSystem.TouchMin);

            var layout = go.GetComponent<LayoutElement>();
            layout.minWidth = 84f;
            layout.preferredWidth = 96f;
            layout.minHeight = buttonHeight;
            layout.preferredHeight = buttonHeight;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(96f, buttonHeight);

            var image = go.GetComponent<Image>();
            image.sprite = primary ? HudDesignSystem.DockButtonPrimarySprite : HudDesignSystem.DockButtonSprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.05f, 1.05f, 1.08f, 1f);
            colors.pressedColor = new Color(0.88f, 0.9f, 0.95f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.28f);
            colors.fadeDuration = 0.07f;
            button.colors = colors;

            var labelRt = CreateRect(go.transform, "Label", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            labelRt.offsetMin = new Vector2(6f, 4f);
            labelRt.offsetMax = new Vector2(-6f, -4f);
            CreateText(labelRt, label, 17f, FontStyles.Bold,
                primary ? HudDesignSystem.TextOnAccent : HudDesignSystem.TextPrimary,
                TextAlignmentOptions.Center);

            var hudButton = go.GetComponent<HudButton>();
            hudButton.Initialize(button, onClick);
            return hudButton;
        }
    }

    public class HudButton : MonoBehaviour,
        UnityEngine.EventSystems.IPointerEnterHandler,
        UnityEngine.EventSystems.IPointerExitHandler,
        UnityEngine.EventSystems.IPointerDownHandler
    {
        private Button button;
        private RectTransform rect;
        private float scale = 1f;
        private float scaleVelocity;
        private bool hovered;

        public bool Interactable
        {
            get => button != null && button.interactable;
            set
            {
                if (button != null)
                    button.interactable = value;
            }
        }

        public void Initialize(Button uiButton, Action click)
        {
            button = uiButton;
            rect = transform as RectTransform;
            button.onClick.AddListener(() =>
            {
                if (button.interactable)
                {
                    UiAudio.Instance?.PlayConfirm();
                    click?.Invoke();
                }
            });
        }

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (!button.interactable)
                return;
            hovered = true;
            UiAudio.Instance?.PlayHover();
        }

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (!button.interactable)
                return;
            scale = 0.95f;
            scaleVelocity = 0f;
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData) => hovered = false;

        private void Update()
        {
            if (rect == null)
                return;

            if (button == null || !button.interactable)
                hovered = false;

            float dt = Time.unscaledDeltaTime;
            // After long AI thinks, the first UI frame can carry a huge hitch delta.
            if (dt > 0.1f)
            {
                scale = hovered && button != null && button.interactable ? 1.04f : 1f;
                scaleVelocity = 0f;
                rect.localScale = Vector3.one * scale;
                return;
            }

            float target = hovered && button != null && button.interactable ? 1.04f : 1f;
            UiFeel.Spring(ref scale, target, ref scaleVelocity, dt, frequency: 9f, damping: 0.75f);

            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale < 0.5f || scale > 1.35f)
            {
                scale = 1f;
                scaleVelocity = 0f;
            }

            rect.localScale = Vector3.one * scale;
        }
    }
}
