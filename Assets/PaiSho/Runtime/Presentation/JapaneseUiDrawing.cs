using UnityEngine;

namespace PaiSho
{
    /// <summary>Soft rounded UI chrome — Nintendo-style panels, pills, and shadows.</summary>
    public static class JapaneseUiDrawing
    {
        public static void DrawSoftShadow(Rect rect, float spread = 5f, float alpha = 0.18f)
        {
            Color prev = GUI.color;
            GUI.color = new Color(JapaneseTheme.UiShadow.r, JapaneseTheme.UiShadow.g, JapaneseTheme.UiShadow.b, alpha);
            GUI.DrawTexture(new Rect(rect.x + spread * 0.35f, rect.y + spread, rect.width, rect.height), JapaneseTheme.GetPanelTexture(), ScaleMode.StretchToFill);
            GUI.color = prev;
        }

        public static void DrawPanel(Rect rect, float pulse, bool drawHeaderStrip = false)
        {
            DrawSoftShadow(rect);
            Color prev = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(rect, JapaneseTheme.GetPanelTexture(), ScaleMode.StretchToFill);
            GUI.color = prev;
        }

        public static void DrawTopHudBar(Rect rect, float pulse)
        {
            DrawSoftShadow(rect, 3f, 0.12f);
            Color prev = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(rect, JapaneseTheme.GetTopBarTexture(), ScaleMode.StretchToFill);
            GUI.color = prev;
        }

        public static void DrawBadge(Rect rect, string label, Color fill, Color textColor, GUIStyle style)
        {
            Color prev = GUI.color;
            GUI.color = fill;
            GUI.DrawTexture(rect, JapaneseTheme.GetBadgeTexture(), ScaleMode.StretchToFill);
            DrawOutlinedLabel(rect, label, style, textColor, new Color(0f, 0f, 0f, 0.08f));
            GUI.color = prev;
        }

        public static void DrawStatRow(
            Rect rect,
            string label,
            string value,
            float fill01,
            Color fillColor,
            GUIStyle labelStyle,
            GUIStyle valueStyle)
        {
            DrawOutlinedLabel(new Rect(rect.x, rect.y, 78f, rect.height), label, labelStyle, labelStyle.normal.textColor, new Color(0f, 0f, 0f, 0.06f));
            DrawOutlinedLabel(new Rect(rect.xMax - 40f, rect.y, 40f, rect.height), value, valueStyle, valueStyle.normal.textColor, new Color(0f, 0f, 0f, 0.06f));

            var track = new Rect(rect.x + 82f, rect.y + rect.height * 0.34f, rect.width - 126f, 10f);
            Color prev = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(track, JapaneseTheme.GetBarTrackTexture(), ScaleMode.StretchToFill);

            fill01 = Mathf.Clamp01(fill01);
            if (fill01 > 0.01f)
            {
                var fill = new Rect(track.x + 1f, track.y + 1f, (track.width - 2f) * fill01, track.height - 2f);
                GUI.color = fillColor;
                GUI.DrawTexture(fill, JapaneseTheme.GetBarTrackTexture(), ScaleMode.StretchToFill);
            }

            GUI.color = prev;
        }

        public static void DrawChip(Rect rect, Color swatch, string label, GUIStyle style)
        {
            Color prev = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(rect, JapaneseTheme.GetChipTexture(), ScaleMode.StretchToFill);

            GUI.color = swatch;
            GUI.DrawTexture(new Rect(rect.x + 8f, rect.y + (rect.height - 8f) * 0.5f, 8f, 8f), Texture2D.whiteTexture);

            DrawOutlinedLabel(new Rect(rect.x + 22f, rect.y + 1f, rect.width - 26f, rect.height - 2f), label, style, style.normal.textColor, new Color(0f, 0f, 0f, 0.05f));
            GUI.color = prev;
        }

        public static void DrawButton(Rect rect, bool hover, bool pressed, bool enabled, bool primary = false)
        {
            Color prev = GUI.color;

            GUI.color = enabled ? Color.white : new Color(1f, 1f, 1f, 0.55f);
            Texture2D tex;
            if (!enabled)
                tex = JapaneseTheme.GetButtonTexture();
            else if (primary)
                tex = hover ? JapaneseTheme.GetPrimaryButtonHoverTexture() : JapaneseTheme.GetPrimaryButtonTexture();
            else
                tex = hover ? JapaneseTheme.GetButtonHoverTexture() : JapaneseTheme.GetButtonTexture();

            DrawSoftShadow(rect, pressed ? 2f : 4f, enabled ? 0.14f : 0.08f);
            GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill);
            GUI.color = prev;
        }

        public static void DrawTurnBanner(Rect rect, float scale, string title, string subtitle, GUIStyle titleStyle, GUIStyle subtitleStyle)
        {
            var scaled = new Rect(
                rect.center.x - rect.width * scale * 0.5f,
                rect.center.y - rect.height * scale * 0.5f,
                rect.width * scale,
                rect.height * scale);

            DrawSoftShadow(scaled, 8f, 0.22f);
            Color prev = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(scaled, JapaneseTheme.GetPanelTexture(), ScaleMode.StretchToFill);

            DrawOutlinedLabel(
                new Rect(scaled.x + 16f, scaled.y + 12f, scaled.width - 32f, 34f),
                title,
                titleStyle,
                JapaneseTheme.UiSky,
                new Color(0f, 0f, 0f, 0.12f));

            if (!string.IsNullOrEmpty(subtitle))
            {
                DrawOutlinedLabel(
                    new Rect(scaled.x + 16f, scaled.y + 46f, scaled.width - 32f, 22f),
                    subtitle,
                    subtitleStyle,
                    JapaneseTheme.UiInk,
                    new Color(0f, 0f, 0f, 0.06f));
            }

            GUI.color = prev;
        }

        public static void DrawOutlinedLabel(Rect rect, string text, GUIStyle style, Color fill, Color outline)
        {
            Color prev = GUI.color;
            var shadowStyle = new GUIStyle(style) { normal = { textColor = outline } };
            GUI.color = outline;
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, shadowStyle);
            GUI.color = fill;
            GUI.Label(rect, text, style);
            GUI.color = prev;
        }

        public static void DrawAccentLine(Rect rect, Color color)
        {
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, JapaneseTheme.GetBarTrackTexture(), ScaleMode.StretchToFill);
            GUI.color = prev;
        }
    }
}
