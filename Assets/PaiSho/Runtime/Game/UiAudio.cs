using UnityEngine;

namespace PaiSho.Game
{
    /// <summary>Cheerful menu chimes — Nintendo-style UI feedback.</summary>
    public class UiAudio : MonoBehaviour
    {
        public static UiAudio Instance;

        [SerializeField] private float volume = 0.42f;

        private AudioSource source;
        private AudioClip hoverClip;
        private AudioClip confirmClip;
        private AudioClip backClip;
        private AudioClip notifyClip;
        private float lastHoverTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;

            hoverClip = CreateChime(new[] { 880f }, 0.045f, 0.22f);
            confirmClip = CreateChime(new[] { 660f, 990f }, 0.09f, 0.34f);
            backClip = CreateChime(new[] { 520f, 440f }, 0.08f, 0.28f);
            notifyClip = CreateChime(new[] { 523f, 784f, 1047f }, 0.16f, 0.38f);
        }

        public void PlayHover()
        {
            if (Time.unscaledTime - lastHoverTime < 0.055f)
                return;

            lastHoverTime = Time.unscaledTime;
            Play(hoverClip, 0.85f);
        }

        public void PlayConfirm() => Play(confirmClip);

        public void PlayBack() => Play(backClip);

        public void PlayNotify() => Play(notifyClip, 1.05f);

        private void Play(AudioClip clip, float scale = 1f)
        {
            if (source == null || clip == null)
                return;

            source.PlayOneShot(clip, volume * scale);
        }

        private static AudioClip CreateChime(float[] notes, float duration, float noteVolume)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var clip = AudioClip.Create($"ui_{notes.Length}", sampleCount, 1, sampleRate, false);
            var data = new float[sampleCount];

            float noteSpacing = duration / notes.Length;
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float sample = 0f;

                for (int n = 0; n < notes.Length; n++)
                {
                    float start = n * noteSpacing;
                    if (t < start)
                        continue;

                    float local = t - start;
                    float attack = Mathf.Clamp01(local / 0.006f);
                    float decay = Mathf.Exp(-local * 8f);
                    float envelope = attack * decay;
                    float freq = notes[n];
                    sample += Mathf.Sin(2f * Mathf.PI * freq * local) * envelope * noteVolume;
                    sample += Mathf.Sin(2f * Mathf.PI * freq * 2f * local) * envelope * noteVolume * 0.18f;
                }

                data[i] = Mathf.Clamp(sample, -1f, 1f);
            }

            clip.SetData(data, 0);
            return clip;
        }
    }
}
