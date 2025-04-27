
using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;

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
            Player host = Player.Host;
            Player opp = Player.Opponent;

            int hostScore = ScoringManager.Instance.GetTotalScore(host);
            int oppScore = ScoringManager.Instance.GetTotalScore(opp);

            int hostMoves = MovementManager.Instance.GetMovedTileCount(Player.Host);
            int oppMoves = MovementManager.Instance.GetMovedTileCount(Player.Opponent);

            int hostEchoes = EchoTileManager.Instance.GetEchoCount(host);
            int oppEchoes = EchoTileManager.Instance.GetEchoCount(opp);

            int hostMomentum = MomentumManager.Instance.GetTotalEarned(host);
            int oppMomentum = MomentumManager.Instance.GetTotalEarned(opp);

            int hostRevived = TileLifecycleManager.Instance.GetTotalRevived(host);
            int oppRevived = TileLifecycleManager.Instance.GetTotalRevived(opp);

            System.Random rand = new();
            string hostLine = $"Host {RandomPhrase(harmonyPhrases, rand)}, {RandomPhrase(revivalPhrases, rand)}, and {RandomPhrase(echoPhrases, rand)}.";
            string oppLine = $"Opponent {RandomPhrase(harmonyPhrases, rand)}, {RandomPhrase(revivalPhrases, rand)}, and {RandomPhrase(echoPhrases, rand)}.";

            DebugLogger.Log("======= Match Summary =======");
            DebugLogger.Log(hostLine);
            DebugLogger.Log(oppLine);
            DebugLogger.Log($"Final Score — Host: {hostScore}, Opponent: {oppScore}");
            DebugLogger.Log(hostScore > oppScore ? "🌸 Host flourished with victory." :
                             oppScore > hostScore ? "🍂 Opponent claimed the bloom." : "🤝 The garden knew no winner, only growth.");
            DebugLogger.Log("Thank you for tending this garden of glass and breath.");
        }

        private string RandomPhrase(string[] phrases, System.Random rand)
        {
            return phrases[rand.Next(phrases.Length)];
        }
    }
}
