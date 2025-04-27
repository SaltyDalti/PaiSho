using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Board;

namespace PaiSho.Game
{
    public class GameSummaryManager : MonoBehaviour
    {
        public static GameSummaryManager Instance;

        private readonly string[] harmonyPhrases =
        {
            "wove melodies from petals",
            "composed radiant alignments",
            "sang with stillness and grace",
            "balanced the breath of blossoms"
        };

        private readonly string[] revivalPhrases =
        {
            "nurtured the fallen with devotion",
            "tended wilted dreams back to bloom",
            "coaxed the quiet to life again",
            "lifted faded flowers from slumber"
        };

        private readonly string[] echoPhrases =
        {
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
            Player opponent = Player.Opponent;

            int hostScore = ScoringManager.Instance.GetTotalScore(host);
            int opponentScore = ScoringManager.Instance.GetTotalScore(opponent);

            // Use TileLifecycleManager.Instance.GetMovedTileCount()
            int hostMoves = TileLifecycleManager.Instance.GetMovedTileCount(host);
            int opponentMoves = TileLifecycleManager.Instance.GetMovedTileCount(opponent);

            int hostEchoes = EchoTileManager.Instance.GetEchoCount(host);
            int opponentEchoes = EchoTileManager.Instance.GetEchoCount(opponent);

            int hostMomentum = MomentumManager.Instance.GetMomentum(host);
            int opponentMomentum = MomentumManager.Instance.GetMomentum(opponent);

            int hostRevived = TileLifecycleManager.Instance.GetTotalRevived(host);
            int opponentRevived = TileLifecycleManager.Instance.GetTotalRevived(opponent);

            System.Random rand = new System.Random();

            string hostLine = $"Host {RandomPhrase(harmonyPhrases, rand)}, {RandomPhrase(revivalPhrases, rand)}, and {RandomPhrase(echoPhrases, rand)}.";
            string opponentLine = $"Opponent {RandomPhrase(harmonyPhrases, rand)}, {RandomPhrase(revivalPhrases, rand)}, and {RandomPhrase(echoPhrases, rand)}.";

            DebugLogger.Log("======= Match Summary =======");
            DebugLogger.Log(hostLine);
            DebugLogger.Log(opponentLine);
            DebugLogger.Log($"Final Score — Host: {hostScore}, Opponent: {opponentScore}");

            if (hostScore > opponentScore)
                DebugLogger.Log("🌸 Host flourished with victory.");
            else if (opponentScore > hostScore)
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
