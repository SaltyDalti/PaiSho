using UnityEngine;

namespace PaiSho.Game
{
    /// <summary>Gentle pulse and float for board overlay markers.</summary>
    public class OverlayAnimator : MonoBehaviour
    {
        public enum Style
        {
            GemPulse,
            RingBreathe,
            ArrowBob,
            DiscFade
        }

        [SerializeField] private Style style = Style.GemPulse;
        [SerializeField] private float speed = 2.8f;
        [SerializeField] private float amplitude = 0.12f;
        [SerializeField] private Transform pulseTarget;

        private Vector3 baseLocalScale;
        private Vector3 baseLocalPosition;
        private float phaseOffset;
        private Renderer[] renderers;
        private Color[] baseColors;

        public void Configure(Style animStyle, Transform target = null, float animSpeed = 2.8f, float animAmplitude = 0.12f)
        {
            style = animStyle;
            pulseTarget = target;
            speed = animSpeed;
            amplitude = animAmplitude;
            CacheBase();
        }

        private void Awake()
        {
            phaseOffset = Random.Range(0f, Mathf.PI * 2f);
            CacheBase();
        }

        private void CacheBase()
        {
            Transform t = pulseTarget != null ? pulseTarget : transform;
            baseLocalScale = t.localScale;
            baseLocalPosition = t.localPosition;

            renderers = GetComponentsInChildren<Renderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material != null)
                    baseColors[i] = renderers[i].material.color;
            }
        }

        private void Update()
        {
            Transform target = pulseTarget != null ? pulseTarget : transform;
            if (GameSession.ReduceMotion)
            {
                target.localScale = baseLocalScale;
                target.localPosition = baseLocalPosition;
                if (style == Style.DiscFade)
                    AnimateAlpha(0.72f);
                return;
            }

            float t = Time.time * speed + phaseOffset;

            switch (style)
            {
                case Style.GemPulse:
                {
                    float wave = 0.5f + 0.5f * Mathf.Sin(t);
                    float pulse = 1f + amplitude * (wave * wave);
                    target.localScale = baseLocalScale * pulse;
                    target.localPosition = baseLocalPosition + Vector3.up * (amplitude * 0.003f * Mathf.Sin(t * 0.9f + 0.5f));
                    break;
                }
                case Style.RingBreathe:
                {
                    float pulse = 1f + amplitude * 0.45f * Mathf.Sin(t * 0.85f);
                    target.localScale = new Vector3(baseLocalScale.x * pulse, baseLocalScale.y, baseLocalScale.z * pulse);
                    break;
                }
                case Style.ArrowBob:
                {
                    float bob = amplitude * 0.015f * Mathf.Sin(t * 1.1f);
                    target.localPosition = baseLocalPosition + Vector3.up * bob;
                    target.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 0.4f) * 4f, 0f);
                    break;
                }
                case Style.DiscFade:
                    AnimateAlpha(0.5f + amplitude * 0.3f * (0.5f + 0.5f * Mathf.Sin(t * 0.9f)));
                    break;
            }
        }

        private void AnimateAlpha(float alpha)
        {
            if (renderers == null)
                return;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || renderers[i].material == null)
                    continue;

                Color c = baseColors[i];
                c.a = alpha;
                renderers[i].material.color = c;
                if (renderers[i].material.HasProperty("_BaseColor"))
                    renderers[i].material.SetColor("_BaseColor", c);
            }
        }
    }
}
