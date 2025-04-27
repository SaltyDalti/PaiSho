
using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Board;

namespace PaiSho.Game
{
    public class VictoryManager : MonoBehaviour
    {
        public static VictoryManager Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public bool CheckForHarmonyRingEnd(Player player, List<Piece> pieces)
        {
            // Simplified logic: check for 4+ harmonizing pieces around middle gate
            int[] ring = { 170, 171, 172, 180, 188, 196, 195, 194 };

            List<Piece> ringPieces = new();
            foreach (int pos in ring)
            {
                Piece p = BoardManager.Instance.GetPieceAt(pos);
                if (p != null && p.Owner == player && p.InHarmony && !p.IsGhost)
                    ringPieces.Add(p);
            }

            if (ringPieces.Count >= 4)
            {
                DebugLogger.Log($">>> {player} formed a harmonic ring! Game ends.");
                EndGame();
                return true;
            }

            return false;
        }

        public void EndGame()
        {
            Player host = Player.Host;
            Player opponent = Player.Opponent;

            int hostScore = ScoringManager.Instance.GetTotalScore(host);
            int oppScore = ScoringManager.Instance.GetTotalScore(opponent);

            DebugLogger.Log("====== GAME OVER ======");
            GameSummaryManager.Instance.GenerateSummary();
            GameLogManager.Instance.PrintLog();
            DebugLogger.Log($"Host Score: {hostScore}");
            DebugLogger.Log($"Opponent Score: {oppScore}");

            if (hostScore > oppScore)
                DebugLogger.Log("🏆 Host Wins!");
            else if (oppScore > hostScore)
                DebugLogger.Log("🏆 Opponent Wins!");
            else
                DebugLogger.Log("🤝 The game ends in a poetic draw.");
        }
    }
}
