using UnityEngine;
using System.Collections.Generic;
using PaiSho.Pieces;
using PaiSho.Board;

namespace PaiSho.Game
{
    public class GameEndManager : MonoBehaviour
    {
        public static GameEndManager Instance;

        public int HostScore { get; private set; }
        public int OpponentScore { get; private set; }
        public Player? Winner { get; private set; }
        public string WinReason { get; private set; } = "";

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public void ResetForNewMatch()
        {
            HostScore = 0;
            OpponentScore = 0;
            Winner = null;
            WinReason = "";
        }

        /// <summary>Credits the other side with the win and records a clear forfeit reason.</summary>
        public void ResolveForfeit(Player forfeitingPlayer)
        {
            Player winner = forfeitingPlayer == Player.Host ? Player.Opponent : Player.Host;
            ResolveFinalScore($"{forfeitingPlayer} forfeited the match.", winner);
        }

        public void ResolveFinalScore(string reason = "Harmony Ring", Player? ringWinner = null)
        {
            WinReason = reason;
            List<Piece> allPieces = BoardManager.Instance.GetAllPieces();
            HostScore = ScoringManager.Instance.ComputeLiveScore(Player.Host, allPieces);
            OpponentScore = ScoringManager.Instance.ComputeLiveScore(Player.Opponent, allPieces);

            // Harmony ring victory crowns the player who closed the ring, not the higher score.
            if (ringWinner.HasValue)
            {
                Winner = ringWinner.Value;
            }
            else if (HostScore > OpponentScore)
            {
                Winner = Player.Host;
            }
            else if (OpponentScore > HostScore)
            {
                Winner = Player.Opponent;
            }
            else
            {
                Winner = null;
            }

            DebugLogger.Log("======== FINAL SCORES ========");
            DebugLogger.Log($"Host: {HostScore}");
            DebugLogger.Log($"Opponent: {OpponentScore}");
            DebugLogger.Log(Winner.HasValue ? $"Winner: {Winner.Value}" : "Draw");

            string logPath = GameLogManager.Instance?.ExportMatchLog("match-end");
            if (!string.IsNullOrEmpty(logPath) && !HeadlessActionExecutor.IsActive)
                GameplayFeedback.Show("Match log saved.", 3.5f);

            AiStudyLibrary.Invalidate();
            GameSummaryManager.Instance?.GenerateSummary();
        }
    }
}
