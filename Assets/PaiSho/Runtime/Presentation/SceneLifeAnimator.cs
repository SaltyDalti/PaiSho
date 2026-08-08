using System.Collections;
using UnityEngine;

namespace PaiSho
{
    public class SceneLifeAnimator : MonoBehaviour
    {
        public static SceneLifeAnimator Instance { get; private set; }

        [SerializeField] private Light boardAccent;
        [SerializeField] private float pulseAmount = 0.14f;
        [SerializeField] private float pulseSpeed = 0.55f;

        private float baseIntensity;
        private Color baseColor;
        private float seasonPunch;
        private Coroutine seasonRoutine;

        private void Awake()
        {
            Instance = this;
        }

        public void Configure(Light accent)
        {
            boardAccent = accent;
            if (boardAccent != null)
            {
                baseIntensity = boardAccent.intensity;
                baseColor = boardAccent.color;
            }
        }

        public static void PulseSeasonChange()
        {
            if (Instance == null)
                Instance = Object.FindAnyObjectByType<SceneLifeAnimator>();
            Instance?.PlaySeasonPulse();
        }

        public void PlaySeasonPulse()
        {
            if (seasonRoutine != null)
                StopCoroutine(seasonRoutine);
            seasonRoutine = StartCoroutine(SeasonPulseRoutine());
        }

        private IEnumerator SeasonPulseRoutine()
        {
            seasonPunch = 1f;
            float duration = 1.1f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                seasonPunch = 1f - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            seasonPunch = 0f;
            seasonRoutine = null;
        }

        private void Update()
        {
            if (boardAccent == null)
                return;

            float wave = Mathf.Sin(Time.time * pulseSpeed);
            float punch = seasonPunch * (0.55f + 0.45f * Mathf.Sin(Time.time * 9f));
            boardAccent.intensity = baseIntensity + pulseAmount * wave + punch * 1.4f;
            boardAccent.color = Color.Lerp(
                baseColor,
                JapaneseTheme.GoldLeaf,
                0.08f + 0.06f * (0.5f + 0.5f * wave) + punch * 0.45f);
        }
    }
}
