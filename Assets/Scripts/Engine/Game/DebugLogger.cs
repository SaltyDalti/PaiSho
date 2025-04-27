
using UnityEngine;

namespace PaiSho.Game
{
    public static class DebugLogger
    {
        public static void Log(string message)
        {
            Debug.Log($"[PaiSho] {message}");
        }

        public static void LogWarning(string message)
        {
            Debug.LogWarning($"[PaiSho WARNING] {message}");
        }

        public static void LogError(string message)
        {
            Debug.LogError($"[PaiSho ERROR] {message}");
        }
    }
}
