using UnityEngine;

namespace PaiSho.Game
{
    /// <summary>Scale-in with overshoot when overlays appear — stagger via delay for cascade effect.</summary>
    public class SpawnRevealAnimator : MonoBehaviour
    {
        [SerializeField] private float delay;
        [SerializeField] private float duration = 0.32f;
        [SerializeField] private Transform target;

        private Vector3 endScale;
        private float elapsed;
        private bool revealed;
        private bool configured;

        public void Configure(Transform revealTarget, float staggerDelay, float revealDuration = 0.32f)
        {
            target = revealTarget;
            delay = staggerDelay;
            duration = revealDuration;
            configured = true;
            Begin();
        }

        private void Awake()
        {
            if (target == null)
                target = transform;

            if (!configured)
                Begin();
        }

        private void Begin()
        {
            if (target == null)
                return;

            if (endScale == Vector3.zero || endScale == target.localScale)
                endScale = target.localScale;

            if (endScale == Vector3.zero)
                endScale = Vector3.one;

            if (GameSession.ReduceMotion)
            {
                target.localScale = endScale;
                revealed = true;
                return;
            }

            target.localScale = Vector3.zero;
            elapsed = 0f;
            revealed = false;
        }

        private void Update()
        {
            if (target == null || revealed)
                return;

            if (GameSession.ReduceMotion)
            {
                target.localScale = endScale;
                revealed = true;
                return;
            }

            elapsed += Time.deltaTime;
            if (elapsed < delay)
                return;

            float u = Mathf.Clamp01((elapsed - delay) / duration);
            float scale = PieceMotion.EaseOutBack(u, 1.55f);
            target.localScale = endScale * scale;

            if (u >= 1f)
            {
                target.localScale = endScale;
                revealed = true;
            }
        }
    }
}
