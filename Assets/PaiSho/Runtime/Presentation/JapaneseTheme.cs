using UnityEngine;

namespace PaiSho
{
    /// <summary>Magical Japanese tea-house palette for board overlays, UI, and accent lighting.</summary>
    public static class JapaneseTheme
    {
        // Hinoki & sumi wood tones
        public static readonly Color HinokiLight = Hex("#E8D4A8");
        public static readonly Color HinokiMid = Hex("#C9A66B");
        public static readonly Color HinokiDark = Hex("#8B6914");
        public static readonly Color SumiInk = Hex("#1C1410");
        public static readonly Color ShojiPaper = Hex("#F5F0E6");
        public static readonly Color TatamiGreen = Hex("#6B7D4A");

        // Lacquer & accent
        public static readonly Color Vermillion = Hex("#C23B22");
        public static readonly Color Indigo = Hex("#2E4057");
        public static readonly Color GoldLeaf = Hex("#D4AF37");
        public static readonly Color Wisteria = Hex("#9B7BB8");
        public static readonly Color Sakura = Hex("#E8A0BF");

        // Overlay markers
        public static readonly Color MoveMarker = new Color(0.18f, 0.38f, 0.62f, 0.92f);
        public static readonly Color CaptureMarker = new Color(0.85f, 0.28f, 0.18f, 0.95f);
        public static readonly Color MomentumMarker = new Color(0.55f, 0.38f, 0.72f, 0.92f);
        public static readonly Color UnloadMarker = new Color(0.36f, 0.68f, 0.58f, 0.92f);
        public static readonly Color DisharmonyMarker = new Color(0.72f, 0.22f, 0.42f, 0.78f);
        public static readonly Color GardenBlockedMarker = new Color(0.68f, 0.52f, 0.16f, 0.75f);
        public static readonly Color PathInk = new Color(0.12f, 0.10f, 0.14f, 0.55f);
        public static readonly Color CaptureLine = new Color(0.92f, 0.42f, 0.22f, 0.65f);
        public static readonly Color PushLine = new Color(0.35f, 0.55f, 0.72f, 0.6f);
        public static readonly Color PortGlow = new Color(0.95f, 0.82f, 0.45f, 0.7f);
        public static readonly Color WheelArrow = new Color(0.88f, 0.72f, 0.28f, 0.85f);

        // Harmony — host garden (green vine) / opponent garden (pink laser)
        public static readonly Color HostHarmonyLine = new Color(0.42f, 0.95f, 0.62f, 0.82f);
        public static readonly Color OpponentHarmonyLine = new Color(1f, 0.52f, 0.72f, 0.82f);
        public static readonly Color HostHarmonyAura = new Color(0.45f, 0.92f, 0.58f, 0.48f);
        public static readonly Color OpponentHarmonyAura = new Color(1f, 0.55f, 0.74f, 0.48f);

        // Spring buds on inlays
        public static readonly Color HostSpringBloom = new Color(0.38f, 0.95f, 0.55f, 1f);
        public static readonly Color OpponentSpringBloom = new Color(1f, 0.58f, 0.78f, 1f);

        // UI — warm Nintendo-style palette (board overlays keep tea-house tones above)
        public static readonly Color UiInk = Hex("#3D4463");
        public static readonly Color UiSoftWhite = Hex("#FDFCF9");
        public static readonly Color UiPanelTop = Hex("#FFFFFF");
        public static readonly Color UiPanelBottom = Hex("#F3F1EC");
        public static readonly Color UiShadow = new Color(0.18f, 0.24f, 0.36f, 0.22f);
        public static readonly Color UiGold = Hex("#FFB347");
        public static readonly Color UiCream = UiSoftWhite;
        public static readonly Color UiSky = Hex("#5B9FE8");
        public static readonly Color UiSkyDark = Hex("#3D7CC4");
        public static readonly Color UiCoral = Hex("#FF6B6B");
        public static readonly Color UiMint = Hex("#6BCB9A");
        public static readonly Color UiLavender = Hex("#9B8AFB");
        public static readonly Color UiPanelBorder = new Color(0.55f, 0.62f, 0.78f, 0.35f);

        private static Texture2D washiTexture;
        private static Texture2D panelTexture;
        private static Texture2D topBarTexture;
        private static Texture2D badgeTexture;
        private static Texture2D buttonTexture;
        private static Texture2D buttonHoverTexture;
        private static Texture2D buttonPrimaryTexture;
        private static Texture2D buttonPrimaryHoverTexture;
        private static Texture2D chipTexture;
        private static Texture2D barTrackTexture;

        public static Texture2D GetWashiTexture()
        {
            if (washiTexture != null)
                return washiTexture;

            washiTexture = CreatePaperTexture(256, 256, UiCream, 0.05f);
            washiTexture.wrapMode = TextureWrapMode.Repeat;
            return washiTexture;
        }

        public static Texture2D GetTopBarTexture()
        {
            if (topBarTexture != null)
                return topBarTexture;

            topBarTexture = CreateRoundedRectTexture(128, 32, new Color(1f, 1f, 1f, 0.92f), Color.clear, 0.42f, topHighlight: 0.12f);
            topBarTexture.wrapMode = TextureWrapMode.Clamp;
            return topBarTexture;
        }

        public static Texture2D GetBadgeTexture()
        {
            if (badgeTexture != null)
                return badgeTexture;

            badgeTexture = CreateRoundedRectTexture(64, 28, new Color(0.96f, 0.97f, 1f, 0.98f), UiPanelBorder, 0.48f, topHighlight: 0.18f);
            badgeTexture.wrapMode = TextureWrapMode.Clamp;
            return badgeTexture;
        }

        public static Texture2D GetPanelTexture()
        {
            if (panelTexture != null)
                return panelTexture;

            panelTexture = CreateRoundedRectTexture(96, 96, UiPanelTop, UiPanelBorder, 0.14f, topHighlight: 0.08f);
            panelTexture.wrapMode = TextureWrapMode.Clamp;
            return panelTexture;
        }

        public static Texture2D GetChipTexture()
        {
            if (chipTexture != null)
                return chipTexture;

            chipTexture = CreateRoundedRectTexture(64, 24, new Color(0.97f, 0.98f, 1f, 0.95f), UiPanelBorder, 0.45f, topHighlight: 0.1f);
            chipTexture.wrapMode = TextureWrapMode.Clamp;
            return chipTexture;
        }

        public static Texture2D GetBarTrackTexture()
        {
            if (barTrackTexture != null)
                return barTrackTexture;

            barTrackTexture = CreateRoundedRectTexture(64, 12, new Color(0.9f, 0.92f, 0.96f, 1f), Color.clear, 0.48f, topHighlight: 0f);
            barTrackTexture.wrapMode = TextureWrapMode.Clamp;
            return barTrackTexture;
        }

        public static Texture2D GetButtonTexture()
        {
            if (buttonTexture != null)
                return buttonTexture;

            buttonTexture = CreateRoundedRectTexture(96, 36, Color.white, UiPanelBorder, 0.42f, topHighlight: 0.22f);
            buttonTexture.wrapMode = TextureWrapMode.Clamp;
            return buttonTexture;
        }

        public static Texture2D GetButtonHoverTexture()
        {
            if (buttonHoverTexture != null)
                return buttonHoverTexture;

            buttonHoverTexture = CreateRoundedRectTexture(96, 36, new Color(0.98f, 0.99f, 1f, 1f), UiSky, 0.42f, topHighlight: 0.28f);
            buttonHoverTexture.wrapMode = TextureWrapMode.Clamp;
            return buttonHoverTexture;
        }

        public static Texture2D GetPrimaryButtonTexture()
        {
            if (buttonPrimaryTexture != null)
                return buttonPrimaryTexture;

            buttonPrimaryTexture = CreateRoundedRectTexture(96, 36, UiSky, UiSkyDark, 0.42f, topHighlight: 0.35f);
            buttonPrimaryTexture.wrapMode = TextureWrapMode.Clamp;
            return buttonPrimaryTexture;
        }

        public static Texture2D GetPrimaryButtonHoverTexture()
        {
            if (buttonPrimaryHoverTexture != null)
                return buttonPrimaryHoverTexture;

            buttonPrimaryHoverTexture = CreateRoundedRectTexture(96, 36, new Color(0.45f, 0.72f, 0.98f, 1f), UiSkyDark, 0.42f, topHighlight: 0.4f);
            buttonPrimaryHoverTexture.wrapMode = TextureWrapMode.Clamp;
            return buttonPrimaryHoverTexture;
        }

        private static Texture2D CreateRoundedRectTexture(
            int width,
            int height,
            Color fill,
            Color border,
            float radius01,
            float topHighlight = 0.2f)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear
            };

            float radius = Mathf.Min(width, height) * radius01;
            var pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float sdf = RoundedBoxSdf(x + 0.5f, y + 0.5f, width - 1f, height - 1f, radius);
                    float alpha = Mathf.Clamp01(0.5f - sdf);

                    Color c = fill;
                    if (topHighlight > 0f && sdf < 0f)
                    {
                        float t = y / (float)(height - 1);
                        c = Color.Lerp(Color.Lerp(fill, Color.white, topHighlight), fill, t * t);
                    }

                    if (border.a > 0.01f && sdf > -1.5f && sdf < 0.5f)
                    {
                        float edge = Mathf.Clamp01(1f - Mathf.Abs(sdf) / 1.2f);
                        c = Color.Lerp(c, border, edge * border.a);
                    }

                    c.a = alpha * fill.a;
                    pixels[y * width + x] = c;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static float RoundedBoxSdf(float px, float py, float width, float height, float radius)
        {
            float cx = width * 0.5f;
            float cy = height * 0.5f;
            float dx = Mathf.Abs(px - cx) - width * 0.5f + radius;
            float dy = Mathf.Abs(py - cy) - height * 0.5f + radius;
            float ax = Mathf.Max(dx, 0f);
            float ay = Mathf.Max(dy, 0f);
            float outside = Mathf.Sqrt(ax * ax + ay * ay) - radius;
            float inside = Mathf.Min(Mathf.Max(dx, dy), 0f);
            return outside + inside;
        }

        private static Texture2D CreateGradientPanelTexture(int width, int height)
            => CreateGradientPanelTexture(width, height, UiPanelTop, UiPanelBottom);

        private static Texture2D CreateGradientPanelTexture(int width, int height, Color top, Color bottom)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float vy = y / (float)(height - 1);
                Color row = Color.Lerp(top, bottom, vy * vy);

                for (int x = 0; x < width; x++)
                {
                    Color c = row;
                    c.a = 0.98f;
                    pixels[y * width + x] = c;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        // Legacy helpers kept for washi backdrop

        private static Texture2D CreatePaperTexture(int width, int height, Color baseColor, float noiseStrength)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = x / (float)width;
                    float ny = y / (float)height;
                    float fiber = Mathf.PerlinNoise(nx * 6f, ny * 14f) * 0.45f
                                + Mathf.PerlinNoise(nx * 18f + 3.7f, ny * 8f + 1.2f) * 0.35f;
                    float horizontalFiber = Mathf.PerlinNoise(nx * 3f, ny * 48f) * 0.15f;
                    float grain = (Mathf.PerlinNoise(nx * 64f, ny * 64f) - 0.5f) * noiseStrength;
                    float edge = Mathf.Min(nx, 1f - nx, ny, 1f - ny);
                    float vignette = Mathf.Clamp01(edge * 6f);

                    Color c = baseColor;
                    c.r = Mathf.Clamp01(c.r + (fiber - 0.35f) * noiseStrength + grain + horizontalFiber * 0.5f);
                    c.g = Mathf.Clamp01(c.g + (fiber - 0.35f) * noiseStrength * 0.85f + grain);
                    c.b = Mathf.Clamp01(c.b + (fiber - 0.35f) * noiseStrength * 0.45f + horizontalFiber * 0.2f);
                    c.a = Mathf.Lerp(0.92f, 1f, vignette);
                    pixels[y * width + x] = c;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        public static Color Hex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
                return color;
            return Color.white;
        }
    }
}
