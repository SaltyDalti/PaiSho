using UnityEngine;
using TMPro;
using UnityEngine.TextCore.LowLevel;

namespace PaiSho.UI
{
    /// <summary>Design tokens — 8pt grid, floating Switch-style surfaces.</summary>
    public static class HudDesignSystem
    {
        public const float Unit = 8f;
        public const float Safe = 20f;
        public const float TouchMin = 48f;
        public const float DockHeight = 52f;
        public const float PillHeight = 40f;
        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;
        public const float MaxDockWidth = 920f;

        // Dark frosted surfaces — reads premium over the wood board, not "Windows dialog"
        public static readonly Color TextPrimary = Hex("#F4F7FB");
        public static readonly Color TextSecondary = Hex("#A8B2C3");
        public static readonly Color TextOnAccent = Color.white;
        public static readonly Color Surface = new Color(0.09f, 0.11f, 0.15f, 0.88f);
        public static readonly Color SurfaceRaised = new Color(0.13f, 0.16f, 0.22f, 0.94f);
        public static readonly Color SurfaceMuted = new Color(0.11f, 0.13f, 0.18f, 0.78f);
        public static readonly Color Scrim = new Color(0.04f, 0.05f, 0.08f, 0.72f);
        public static readonly Color Accent = Hex("#4DA3FF");
        public static readonly Color AccentStrong = Hex("#2B7FE8");
        public static readonly Color Warning = Hex("#FFB020");
        public static readonly Color Danger = Hex("#FF6B6B");
        public static readonly Color Success = Hex("#3DDC97");

        private static Sprite whiteSprite;
        private static Sprite panelSprite;
        private static Sprite pillSprite;
        private static Sprite dockButtonSprite;
        private static Sprite dockButtonPrimarySprite;
        private static TMP_FontAsset fontAsset;

        public static TMP_FontAsset Font
        {
            get
            {
                EnsureDefaultFont();
                return fontAsset;
            }
        }

        public static void EnsureDefaultFont()
        {
            if (fontAsset != null)
                return;

            fontAsset = TMP_Settings.defaultFontAsset;
            if (fontAsset != null)
                return;

            fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (fontAsset != null)
                return;

            foreach (TMP_FontAsset asset in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            {
                if (asset != null && asset.atlasTexture != null)
                {
                    fontAsset = asset;
                    return;
                }
            }

            UnityEngine.Font osFont = UnityEngine.Font.CreateDynamicFontFromOSFont(
                new[] { "Segoe UI", "Helvetica Neue", "Arial", "Sans Serif" }, 32);
            if (osFont == null)
                return;

            fontAsset = TMP_FontAsset.CreateFontAsset(
                osFont,
                32,
                4,
                GlyphRenderMode.SDFAA,
                512,
                512);
        }

        public static Sprite WhiteSprite => whiteSprite ??= CreateSolidSprite(Color.white);

        public static Sprite PanelSprite => panelSprite ??= CreateRoundedSprite(128, SurfaceRaised, 0.14f, 10f);
        public static Sprite PillSprite => pillSprite ??= CreateRoundedSprite(64, SurfaceMuted, 0.44f, 8f);
        public static Sprite DockButtonSprite => dockButtonSprite ??= CreateRoundedSprite(96, Surface, 0.4f, 8f);
        public static Sprite DockButtonPrimarySprite => dockButtonPrimarySprite ??= CreateRoundedSprite(96, AccentStrong, 0.4f, 8f);

        private static Sprite CreateSolidSprite(Color color)
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Sprite CreateRoundedSprite(int size, Color fill, float radius01, float borderPx)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float radius = size * radius01;
            var pixels = new Color[size * size];
            Color edge = new Color(1f, 1f, 1f, 0.08f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float sdf = RoundedBoxSdf(x + 0.5f, y + 0.5f, size - 1f, size - 1f, radius);
                    float alpha = Mathf.Clamp01(0.5f - sdf);
                    Color c = fill;

                    if (sdf > -borderPx && sdf < 0.5f)
                    {
                        float t = Mathf.Clamp01(1f - Mathf.Abs(sdf) / borderPx);
                        c = Color.Lerp(c, edge, t * 0.5f);
                    }

                    if (y > size * 0.78f && sdf < 0f)
                        c = Color.Lerp(c, Color.black, 0.12f);
                    else if (y < size * 0.12f && sdf < 0f)
                        c = Color.Lerp(c, Color.white, 0.06f);

                    c.a = alpha * fill.a;
                    pixels[y * size + x] = c;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            float slice = Mathf.Max(radius + 2f, borderPx + 2f);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(slice, slice, slice, slice));
        }

        private static float RoundedBoxSdf(float px, float py, float width, float height, float radius)
        {
            float cx = width * 0.5f;
            float cy = height * 0.5f;
            float dx = Mathf.Abs(px - cx) - width * 0.5f + radius;
            float dy = Mathf.Abs(py - cy) - height * 0.5f + radius;
            float ax = Mathf.Max(dx, 0f);
            float ay = Mathf.Max(dy, 0f);
            return Mathf.Sqrt(ax * ax + ay * ay) - radius + Mathf.Min(Mathf.Max(dx, dy), 0f);
        }

        public static Color Hex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
                return color;
            return Color.white;
        }
    }
}
