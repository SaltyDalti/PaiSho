using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace PaiSho.DevTools
{
    /// <summary>Compact IMGUI helpers for runtime dev tuners (F8, F9, etc.).</summary>
    public static class DevTunerGui
    {
        public const float DefaultPanelWidth = 320f;
        public const float DefaultMargin = 10f;
        public const float FooterHeight = 34f;

        public static Rect LastPanelRect { get; private set; }

        public static bool IsPointerOverPanel(Vector2 screenPosition)
        {
            if (LastPanelRect.width <= 0f)
                return false;

            var guiPoint = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            return LastPanelRect.Contains(guiPoint);
        }

        public static void ClearPanelRect() => LastPanelRect = Rect.zero;

        private static GUIStyle _hintStyle;
        private static GUIStyle _sectionStyle;
        private static GUIStyle _slotOffStyle;
        private static GUIStyle _miniButtonStyle;

        public static void DrawClosedHints(params (KeyCode key, string label)[] entries)
        {
            EnsureStyles();
            float y = Screen.height - 22f;
            float x = 10f;
            foreach ((KeyCode key, string label) in entries)
            {
                string text = $"[{key}] {label}";
                var size = _hintStyle.CalcSize(new GUIContent(text));
                GUI.Label(new Rect(x, y, size.x + 4f, 20f), text, _hintStyle);
                x += size.x + 14f;
            }
        }

        public static void BeginPanel(
            string title,
            float width,
            float margin,
            float footerHeight,
            ref Vector2 scroll,
            out Rect contentArea,
            bool useScrollView = true)
        {
            EnsureStyles();
            var panelRect = new Rect(Screen.width - width - margin, margin, width, Screen.height - margin * 2f);
            LastPanelRect = panelRect;
            GUI.Box(panelRect, title);

            const float pad = 8f;
            const float header = 26f;
            float footerTop = panelRect.yMax - pad - footerHeight;
            contentArea = new Rect(panelRect.x + pad, panelRect.y + header, panelRect.width - pad * 2f, footerTop - panelRect.y - header - 2f);

            GUILayout.BeginArea(contentArea);
            if (useScrollView)
                scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
        }

        public static void EndPanelScroll(bool useScrollView = true)
        {
            if (useScrollView)
                GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        public static void DrawFooter(Rect contentArea, float footerHeight, Action drawFooter)
        {
            var footerRect = new Rect(
                contentArea.x,
                contentArea.yMax + 2f,
                contentArea.width,
                footerHeight);
            GUILayout.BeginArea(footerRect);
            drawFooter?.Invoke();
            GUILayout.EndArea();
        }

        public static bool Section(ref bool expanded, string title)
        {
            EnsureStyles();
            expanded = GUILayout.Toggle(expanded, (expanded ? "v " : "> ") + title, _sectionStyle);
            if (!expanded)
                return false;

            GUILayout.BeginVertical(GUI.skin.box);
            return true;
        }

        public static void EndSection(bool wasOpen)
        {
            if (wasOpen)
                GUILayout.EndVertical();
        }

        public static void Toolbar(int selectedIndex, string[] labels, Action<int> onSelected)
        {
            int next = GUILayout.Toolbar(selectedIndex, labels, GUILayout.Height(24f));
            if (next != selectedIndex)
                onSelected?.Invoke(next);
        }

        public static int SlotButtons(int selected, int count)
        {
            EnsureStyles();
            GUILayout.BeginHorizontal();
            Color previous = GUI.backgroundColor;
            for (int i = 0; i < count; i++)
            {
                GUI.backgroundColor = i == selected ? new Color(0.35f, 0.6f, 0.95f) : previous;
                if (GUILayout.Button(i.ToString(), _slotOffStyle, GUILayout.Width(28f), GUILayout.Height(24f)))
                    selected = i;
            }

            GUI.backgroundColor = previous;

            GUILayout.FlexibleSpace();
            GUILayout.Label($"[{selected}]", GUILayout.Width(36f));
            GUILayout.EndHorizontal();
            return selected;
        }

        public static void NudgeRow(string label, ref float value, float[] steps)
        {
            EnsureStyles();
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(52f));
            foreach (float step in steps)
            {
                string sign = step >= 0f ? "+" : "";
                if (GUILayout.Button($"{sign}{step:G}", _miniButtonStyle, GUILayout.Width(40f)))
                    value += step;
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label(value.ToString("F4", CultureInfo.InvariantCulture), GUILayout.Width(64f));
            GUILayout.EndHorizontal();
        }

        public static void ActionBar(params (string label, Action action)[] actions)
        {
            GUILayout.BeginHorizontal();
            foreach ((string label, Action action) in actions)
            {
                if (GUILayout.Button(label, GUILayout.Height(26f)))
                    action?.Invoke();
            }

            GUILayout.EndHorizontal();
        }

        public static void Status(string message, float clearTime)
        {
            if (string.IsNullOrEmpty(message) || Time.unscaledTime >= clearTime)
                return;

            GUILayout.Label(message, GUI.skin.box);
        }

        public static float FloatField(
            IDictionary<string, string> buffers,
            string key,
            string label,
            float value,
            float min,
            float max,
            float labelWidth = 88f,
            float fieldWidth = 58f)
        {
            if (!buffers.TryGetValue(key, out string text))
                text = value.ToString("G6", CultureInfo.InvariantCulture);

            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(labelWidth));
            string newText = GUILayout.TextField(text, GUILayout.Width(fieldWidth));
            GUILayout.EndHorizontal();

            float current = value;
            if (TryParseFloat(newText, out float parsed))
                current = parsed;

            float clamped = Mathf.Clamp(current, min, max);
            float sliderValue = GUILayout.HorizontalSlider(clamped, min, max);

            float result;
            if (!Mathf.Approximately(sliderValue, clamped))
            {
                result = sliderValue;
                buffers[key] = result.ToString("G6", CultureInfo.InvariantCulture);
            }
            else if (TryParseFloat(newText, out parsed))
            {
                result = parsed;
                buffers[key] = newText;
            }
            else
            {
                result = current;
                buffers[key] = newText;
            }

            return result;
        }

        public static int IntField(
            IDictionary<string, string> buffers,
            string key,
            string label,
            int value,
            int min,
            int max,
            float labelWidth = 88f,
            float fieldWidth = 58f)
        {
            float parsed = FloatField(buffers, key, label, value, min, max, labelWidth, fieldWidth);
            return Mathf.Clamp(Mathf.RoundToInt(parsed), min, max);
        }

        public static Vector3 Vector3Field(
            IDictionary<string, string> buffers,
            string prefix,
            string label,
            Vector3 value,
            Vector3 min,
            Vector3 max)
        {
            GUILayout.Label(label);
            value.x = FloatField(buffers, $"{prefix}.x", "  X", value.x, min.x, max.x);
            value.y = FloatField(buffers, $"{prefix}.y", "  Y", value.y, min.y, max.y);
            value.z = FloatField(buffers, $"{prefix}.z", "  Z", value.z, min.z, max.z);
            return value;
        }

        public static Vector3 EulerField(
            IDictionary<string, string> buffers,
            string prefix,
            string label,
            Vector3 euler,
            float limit = 90f)
        {
            GUILayout.Label(label);
            euler.x = FloatField(buffers, $"{prefix}.x", "  Pitch", euler.x, -limit, limit);
            euler.y = FloatField(buffers, $"{prefix}.y", "  Yaw", euler.y, -limit, limit);
            euler.z = FloatField(buffers, $"{prefix}.z", "  Roll", euler.z, -limit, limit);
            return euler;
        }

        public static float Slider(string label, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(160f));
            GUILayout.Label(value.ToString("F4"), GUILayout.Width(48f));
            GUILayout.EndHorizontal();
            return GUILayout.HorizontalSlider(value, min, max);
        }

        public static bool TryParseFloat(string text, out float value)
        {
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return true;

            return float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        private static void EnsureStyles()
        {
            if (_hintStyle != null)
                return;

            _hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(1f, 1f, 1f, 0.5f) }
            };

            _sectionStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                fixedHeight = 22f
            };

            _slotOffStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 24f
            };

            _miniButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                fixedHeight = 20f,
                padding = new RectOffset(2, 2, 2, 2)
            };
        }
    }
}
