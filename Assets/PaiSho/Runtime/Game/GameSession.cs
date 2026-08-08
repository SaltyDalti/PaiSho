using UnityEngine;

namespace PaiSho.Game
{
    /// <summary>Shared session prefs for title flow, AI mode, and audio.</summary>
    public static class GameSession
    {
        private const string VolumeKey = "pai_sho_master_volume";
        private const string MuteKey = "pai_sho_mute";
        private const string AiKey = "pai_sho_ai_enabled";
        private const string ColorblindKey = "pai_sho_colorblind_assist";
        private const string ReduceMotionKey = "pai_sho_reduce_motion";
        private const string LargerHudKey = "pai_sho_larger_hud_type";

        public static bool MatchStarted { get; private set; }
        public static bool ShowTitleOnBoot { get; set; } = true;

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 0.85f);
            set
            {
                PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
                AudioListener.volume = Muted ? 0f : MasterVolume;
            }
        }

        public static bool Muted
        {
            get => PlayerPrefs.GetInt(MuteKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(MuteKey, value ? 1 : 0);
                PlayerPrefs.Save();
                AudioListener.volume = value ? 0f : MasterVolume;
            }
        }

        public static bool AiEnabled
        {
            get => PlayerPrefs.GetInt(AiKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(AiKey, value ? 1 : 0);
                PlayerPrefs.Save();
                if (AiController.Instance != null)
                    AiController.Instance.SetAiEnabled(value);
            }
        }

        /// <summary>Accessibility: swaps move/capture/momentum markers to distinct shapes, not just color.</summary>
        public static bool ColorblindAssist
        {
            get => PlayerPrefs.GetInt(ColorblindKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(ColorblindKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Accessibility: skips cinematic camera moves, drag spin, and shortens toast pop-in.</summary>
        public static bool ReduceMotion
        {
            get => PlayerPrefs.GetInt(ReduceMotionKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(ReduceMotionKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Accessibility: scales up HUD text for readability.</summary>
        public static bool LargerHudType
        {
            get => PlayerPrefs.GetInt(LargerHudKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(LargerHudKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static void ApplyAudio()
        {
            AudioListener.volume = Muted ? 0f : MasterVolume;
        }

        public static void MarkMatchStarted() => MatchStarted = true;

        public static void ResetForTitle()
        {
            MatchStarted = false;
        }
    }
}
