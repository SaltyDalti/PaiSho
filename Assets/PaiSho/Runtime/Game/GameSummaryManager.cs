using UnityEngine;

namespace PaiSho.Game
{
    public class GameSummaryManager : MonoBehaviour
    {
        public static GameSummaryManager Instance;

        private readonly string[] harmonyPhrases = {
            "wove melodies from petals",
            "composed radiant alignments",
            "sang with stillness and grace",
            "balanced the breath of blossoms"
        };

        private readonly string[] revivalPhrases = {
            "nurtured the fallen with devotion",
            "tended wilted dreams back to bloom",
            "coaxed the quiet to life again",
            "lifted faded flowers from slumber"
        };

        private readonly string[] echoPhrases = {
            "echoed souls of old returned",
            "summoned whispers from the Pot",
            "called back memories with bloom",
            "invoked spirits from the garden’s past"
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public void GenerateSummary()
        {
            int hostScore = GameEndManager.Instance != null
                ? GameEndManager.Instance.HostScore
                : ScoringManager.Instance.GetTotalScore(Player.Host);
            int oppScore = GameEndManager.Instance != null
                ? GameEndManager.Instance.OpponentScore
                : ScoringManager.Instance.GetTotalScore(Player.Opponent);
            Player? winner = GameEndManager.Instance?.Winner;

            System.Random rand = new();
            string hostLine = $"Host {RandomPhrase(harmonyPhrases, rand)}, {RandomPhrase(revivalPhrases, rand)}, and {RandomPhrase(echoPhrases, rand)}.";
            string oppLine = $"Opponent {RandomPhrase(harmonyPhrases, rand)}, {RandomPhrase(revivalPhrases, rand)}, and {RandomPhrase(echoPhrases, rand)}.";

            DebugLogger.Log("======= Match Summary =======");
            DebugLogger.Log(hostLine);
            DebugLogger.Log(oppLine);
            DebugLogger.Log($"Final Score — Host: {hostScore}, Opponent: {oppScore}");

            if (winner == Player.Host)
                DebugLogger.Log("🌸 Host flourished with victory.");
            else if (winner == Player.Opponent)
                DebugLogger.Log("🍂 Opponent claimed the bloom.");
            else if (hostScore > oppScore)
                DebugLogger.Log("🌸 Host flourished with victory.");
            else if (oppScore > hostScore)
                DebugLogger.Log("🍂 Opponent claimed the bloom.");
            else
                DebugLogger.Log("🤝 The garden knew no winner, only growth.");

            DebugLogger.Log("Thank you for tending this garden of glass and breath.");
        }

        private string RandomPhrase(string[] phrases, System.Random rand)
        {
            return phrases[rand.Next(phrases.Length)];
        }
    }
}
