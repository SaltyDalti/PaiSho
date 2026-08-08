using UnityEngine;
using PaiSho;

namespace PaiSho.Game
{
    /// <summary>
    /// Fantastical harmony link: soft laser-vine with rising light motes.
    /// </summary>
    public class HarmonyConnectionAnimator : MonoBehaviour
    {
        private const int PointCount = 18;
        private const int MoteCount = 5;

        private LineRenderer halo;
        private LineRenderer core;
        private Mote[] motes;
        private Vector3 from;
        private Vector3 to;
        private Color baseColor;
        private float baseWidth;
        private float phaseOffset;
        private float reveal;
        private float age;
        private Vector3[] path = new Vector3[PointCount];

        private struct Mote
        {
            public Transform Transform;
            public Renderer Renderer;
            public float PathOffset;
            public float Rise;
            public float Speed;
            public float Size;
            public float Phase;
        }

        public static GameObject Create(Vector3 fromWorld, Vector3 toWorld, Color color, float width)
        {
            Vector3 delta = toWorld - fromWorld;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.0001f)
                return null;

            var root = new GameObject("HarmonyVine");
            var anim = root.AddComponent<HarmonyConnectionAnimator>();
            anim.Build(fromWorld, toWorld, color, width);
            return root;
        }

        public void ConfigureReveal(float delay, float duration)
        {
            phaseOffset = delay;
            reveal = Mathf.Max(0.18f, duration);
        }

        private void Build(Vector3 fromWorld, Vector3 toWorld, Color color, float width)
        {
            from = fromWorld;
            to = toWorld;
            baseColor = color;
            // Thinner average beam; glow comes from emission, not thickness.
            baseWidth = Mathf.Max(0.005f, width * 0.62f);
            phaseOffset = Random.Range(0f, Mathf.PI * 2f);
            reveal = 0.42f;

            halo = CreateLine("Halo", baseWidth * 2.1f, 0.28f);
            core = CreateLine("Core", baseWidth * 0.85f, 1f);
            CreateMotes();

            RebuildPath(0f);
            ApplyLinePoints(0f);
        }

        private void CreateMotes()
        {
            motes = new Mote[MoteCount];
            for (int i = 0; i < MoteCount; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = $"HarmonyMote_{i}";
                Object.Destroy(go.GetComponent<Collider>());
                go.transform.SetParent(transform, false);

                float size = baseWidth * Random.Range(1.1f, 1.8f);
                go.transform.localScale = Vector3.one * size;

                var renderer = go.GetComponent<Renderer>();
                WoodTheme.ApplyEmissiveColorPublic(
                    renderer,
                    Color.Lerp(baseColor, Color.white, 0.65f),
                    5.5f);
                if (renderer.material != null)
                    renderer.material.renderQueue = 3010;

                motes[i] = new Mote
                {
                    Transform = go.transform,
                    Renderer = renderer,
                    PathOffset = i / (float)MoteCount + Random.Range(-0.05f, 0.05f),
                    Rise = Random.Range(0.02f, 0.07f),
                    Speed = Random.Range(0.045f, 0.08f),
                    Size = size,
                    Phase = Random.Range(0f, Mathf.PI * 2f)
                };
            }
        }

        private LineRenderer CreateLine(string name, float width, float alphaScale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = PointCount;
            line.widthMultiplier = width;
            line.numCapVertices = 4;
            line.numCornerVertices = 3;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.useWorldSpace = true;

            Color c = baseColor;
            c.a *= alphaScale;
            line.material = CreateLineMaterial(c, alphaScale > 0.5f ? 3.8f : 1.8f);
            line.startColor = c;
            line.endColor = c;
            line.startWidth = width;
            line.endWidth = width * 0.78f;
            return line;
        }

        private static Material CreateLineMaterial(Color color, float emission)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Universal Render Pipeline/Lit");

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
            }

            material.renderQueue = 3000;
            return material;
        }

        private void Update()
        {
            age += Time.deltaTime;
            // Slow magical drift — about half the previous tempo.
            float t = Time.time * 0.55f + phaseOffset;
            float appear = Mathf.Clamp01((age - phaseOffset * 0.15f) / reveal);
            appear = PieceMotion.EaseOutCubic(appear);

            RebuildPath(t);
            ApplyLinePoints(appear);
            AnimateMotes(t, appear);
            PulseWidths(t, appear);
            FlowColors(t, appear);
        }

        private void RebuildPath(float t)
        {
            Vector3 mid = Vector3.Lerp(from, to, 0.5f);
            float span = Vector3.Distance(from, to);
            float arcHeight = Mathf.Clamp(span * 0.11f, 0.035f, 0.16f);
            mid.y += arcHeight;

            for (int i = 0; i < PointCount; i++)
            {
                float u = i / (float)(PointCount - 1);
                Vector3 p = QuadraticBezier(from, mid, to, u);

                Vector3 along = (to - from).normalized;
                Vector3 side = Vector3.Cross(Vector3.up, along).normalized;
                if (side.sqrMagnitude < 0.001f)
                    side = Vector3.right;

                float wave = Mathf.Sin(u * Mathf.PI * 2.2f + t * 1.1f) * span * 0.014f;
                float liftWave = Mathf.Sin(u * Mathf.PI * 3f + t * 1.4f) * span * 0.008f;
                p += side * wave;
                p.y += liftWave * Mathf.Sin(u * Mathf.PI);
                path[i] = p;
            }
        }

        private void ApplyLinePoints(float appear)
        {
            int visible = Mathf.Max(2, Mathf.CeilToInt(PointCount * appear));
            if (halo != null)
            {
                halo.positionCount = visible;
                for (int i = 0; i < visible; i++)
                    halo.SetPosition(i, path[i]);
            }

            if (core != null)
            {
                core.positionCount = visible;
                for (int i = 0; i < visible; i++)
                    core.SetPosition(i, path[i]);
            }
        }

        private void AnimateMotes(float t, float appear)
        {
            if (motes == null)
                return;

            for (int i = 0; i < motes.Length; i++)
            {
                Mote mote = motes[i];
                if (mote.Transform == null)
                    continue;

                if (appear < 0.12f)
                {
                    mote.Transform.gameObject.SetActive(false);
                    continue;
                }

                mote.Transform.gameObject.SetActive(true);

                // Drift slowly along the vine, then rise like ember motes.
                float u = Mathf.Repeat(mote.PathOffset + t * mote.Speed, 1f);
                float riseWave = Mathf.Sin((u + mote.Phase) * Mathf.PI);
                Vector3 pos = SamplePath(u);
                pos.y += mote.Rise * riseWave * appear;

                mote.Transform.position = pos;
                float pulse = 0.85f + 0.25f * Mathf.Sin(t * 2.2f + mote.Phase);
                mote.Transform.localScale = Vector3.one * (mote.Size * pulse);

                if (mote.Renderer != null && mote.Renderer.material != null)
                {
                    Color c = Color.Lerp(baseColor, Color.white, 0.55f + 0.2f * Mathf.Sin(t * 1.8f + mote.Phase));
                    c.a = 0.9f * appear;
                    if (mote.Renderer.material.HasProperty("_BaseColor"))
                        mote.Renderer.material.SetColor("_BaseColor", c);
                    if (mote.Renderer.material.HasProperty("_EmissionColor"))
                        mote.Renderer.material.SetColor("_EmissionColor", c * (5.2f + pulse));
                    mote.Renderer.material.color = c;
                }
            }
        }

        private void PulseWidths(float t, float appear)
        {
            float breath = 1f + 0.1f * Mathf.Sin(t * 1.4f);
            float tip = 0.8f + 0.15f * Mathf.Sin(t * 2.1f + 1.2f);

            if (halo != null)
            {
                float w = baseWidth * 2.1f * breath * appear;
                halo.startWidth = w;
                halo.endWidth = w * tip;
            }

            if (core != null)
            {
                float w = baseWidth * 0.85f * breath * appear;
                core.startWidth = w;
                core.endWidth = w * tip * 0.9f;
            }
        }

        private void FlowColors(float t, float appear)
        {
            Color hot = Color.Lerp(baseColor, Color.white, 0.62f);
            Color cool = Color.Lerp(baseColor, new Color(0.55f, 0.9f, 1f, 1f), 0.3f);
            float flow = 0.5f + 0.5f * Mathf.Sin(t * 1.5f);

            Color start = Color.Lerp(cool, hot, flow);
            Color end = Color.Lerp(hot, cool, flow);
            start.a = baseColor.a * appear;
            end.a = baseColor.a * appear * 0.88f;

            ApplyLineColor(halo, start, end, 0.42f, 2.4f);
            ApplyLineColor(core, Color.Lerp(start, Color.white, 0.3f), end, 1f, 4.6f);
        }

        private static void ApplyLineColor(LineRenderer line, Color start, Color end, float alphaScale, float emission)
        {
            if (line == null)
                return;

            start.a *= alphaScale;
            end.a *= alphaScale;
            line.startColor = start;
            line.endColor = end;

            if (line.material == null)
                return;

            Color mid = Color.Lerp(start, end, 0.5f);
            if (line.material.HasProperty("_BaseColor"))
                line.material.SetColor("_BaseColor", mid);
            if (line.material.HasProperty("_Color"))
                line.material.SetColor("_Color", mid);
            if (line.material.HasProperty("_EmissionColor"))
                line.material.SetColor("_EmissionColor", mid * emission);
        }

        private Vector3 SamplePath(float u)
        {
            float scaled = Mathf.Clamp01(u) * (PointCount - 1);
            int i = Mathf.FloorToInt(scaled);
            int j = Mathf.Min(i + 1, PointCount - 1);
            float f = scaled - i;
            return Vector3.Lerp(path[i], path[j], f);
        }

        private static Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float u)
        {
            float o = 1f - u;
            return o * o * a + 2f * o * u * b + u * u * c;
        }
    }
}
