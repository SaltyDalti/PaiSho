using UnityEngine;

namespace PaiSho.Game
{
    /// <summary>Short-lived on-screen message for invalid clicks and other player feedback.</summary>
    public static class GameplayFeedback
    {
        private static string message;
        private static float showUntil;
        private static float showStart;
        private static float popScale = 1f;
        private static float popVelocity;

        public static void Show(string text, float durationSeconds = 4f)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            // Headless self-play can fire thousands of these — skip UI/audio/log spam.
            if (HeadlessActionExecutor.IsActive)
                return;

            message = text;
            showStart = Time.unscaledTime;
            showUntil = showStart + durationSeconds;
            popScale = 0.82f;
            popVelocity = 0f;
            DebugLogger.LogWarning(text);
            UiAudio.Instance?.PlayBack();
        }

        public static bool TryGetMessage(out string text)
        {
            if (string.IsNullOrEmpty(message) || Time.unscaledTime > showUntil)
            {
                text = null;
                return false;
            }

            text = message;
            return true;
        }

        public static float GetDisplayAlpha()
        {
            if (string.IsNullOrEmpty(message) || Time.unscaledTime > showUntil)
                return 0f;

            float age = Time.unscaledTime - showStart;
            float fadeIn = UiFeel.OutBack(Mathf.Clamp01(age / 0.28f));
            float fadeOut = Mathf.Clamp01((showUntil - Time.unscaledTime) / 0.45f);
            return fadeIn * fadeOut;
        }

        public static float GetSlideOffset()
        {
            float alpha = GetDisplayAlpha();
            return Mathf.Lerp(18f, 0f, alpha);
        }

        public static float GetPopScale()
        {
            if (string.IsNullOrEmpty(message) || Time.unscaledTime > showUntil)
                return 1f;

            UiFeel.Spring(ref popScale, 1f, ref popVelocity, Time.unscaledDeltaTime, frequency: 7f, damping: 0.62f);
            return popScale;
        }

        public static void Clear()
        {
            message = null;
            showUntil = 0f;
            showStart = 0f;
            popScale = 1f;
            popVelocity = 0f;
        }
    }
}
