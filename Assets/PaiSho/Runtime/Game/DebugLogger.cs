
using UnityEngine;

namespace PaiSho.Game
{
    public static class DebugLogger
    {
        public static void Log(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[PaiSho] {message}");
#endif
        }

        public static void LogWarning(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[PaiSho WARNING] {message}");
#endif
        }

        public static void LogError(string message)
        {
            Debug.LogError($"[PaiSho ERROR] {message}");
        }
    }
}
