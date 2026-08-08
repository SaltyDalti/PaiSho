using UnityEngine;

namespace PaiSho.Game
{
    /// <summary>Fades path/beam overlays in after markers appear.</summary>
    public class LineRevealAnimator : MonoBehaviour
    {
        [SerializeField] private float delay = 0.12f;
        [SerializeField] private float duration = 0.28f;

        private Renderer[] renderers;
        private Color[] targetColors;
        private float elapsed;

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            targetColors = new Color[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || renderers[i].material == null)
                    continue;

                targetColors[i] = renderers[i].material.color;
                if (renderers[i].material.HasProperty("_BaseColor"))
                    targetColors[i] = renderers[i].material.GetColor("_BaseColor");

                SetAlpha(i, GameSession.ReduceMotion ? targetColors[i].a : 0f);
            }

            if (GameSession.ReduceMotion)
                elapsed = delay + duration;
        }

        public void Configure(float revealDelay, float revealDuration)
        {
            delay = revealDelay;
            duration = revealDuration;
            if (GameSession.ReduceMotion)
            {
                delay = 0f;
                duration = 0.01f;
            }
        }

        private void Update()
        {
            if (GameSession.ReduceMotion)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == null)
                        continue;
                    SetAlpha(i, targetColors[i].a);
                }
                return;
            }

            elapsed += Time.deltaTime;
            if (elapsed < delay)
                return;

            float u = PieceMotion.EaseOutCubic(Mathf.Clamp01((elapsed - delay) / duration));
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    continue;

                Color c = targetColors[i];
                c.a = targetColors[i].a * u;
                SetAlpha(i, c.a);
            }
        }

        private void SetAlpha(int index, float alpha)
        {
            if (renderers[index] == null || renderers[index].material == null)
                return;

            Color c = targetColors[index];
            c.a = alpha;
            renderers[index].material.color = c;
            if (renderers[index].material.HasProperty("_BaseColor"))
                renderers[index].material.SetColor("_BaseColor", c);
        }
    }
}
