using UnityEditor;
using UnityEngine;
using PaiSho.Game;

namespace PaiSho.EditorTools
{
    public static class AiSelfPlayMenu
    {
        [MenuItem("PaiSho/AI/Run Self-Play Benchmark (3 games)")]
        private static void RunBenchmark()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "AI Self-Play",
                    "Enter Play Mode first, then run this again.\n\nSelf-play needs the live game scene.",
                    "OK");
                return;
            }

            AiSelfPlayRunner runner = Object.FindAnyObjectByType<AiSelfPlayRunner>();
            if (runner == null)
            {
                var go = new GameObject("AiSelfPlayRunner");
                runner = go.AddComponent<AiSelfPlayRunner>();
            }

            runner.StartBenchmark(3, maxTurns: 500);
            // Dismiss title so GameBootstrap.Start can finish if still waiting.
            GameSession.ShowTitleOnBoot = false;
            TitleMenu.Instance?.DismissForMatch();
            UnityEngine.Debug.Log("AI self-play started (3 games, max 500 turns). Editor should stay responsive - watch Console.");
        }

        [MenuItem("PaiSho/AI/Stop Self-Play")]
        private static void StopBenchmark()
        {
            AiSelfPlayRunner runner = Object.FindAnyObjectByType<AiSelfPlayRunner>();
            if (runner == null)
            {
                UnityEngine.Debug.Log("No self-play runner active.");
                return;
            }

            runner.StopBenchmark();
            UnityEngine.Debug.Log("AI self-play stopped.");
        }

        [MenuItem("PaiSho/AI/Create Scorer Weights Asset")]
        private static void CreateWeightsAsset()
        {
            var asset = ScriptableObject.CreateInstance<AiScorerWeights>();
            AssetDatabase.CreateAsset(asset, "Assets/AiScorerWeights.asset");
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            UnityEngine.Debug.Log("Created Assets/AiScorerWeights.asset — duplicate to experiment.");
        }
    }
}
