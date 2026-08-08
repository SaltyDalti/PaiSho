using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Domain;

namespace PaiSho.Game
{
    public class ScoringManager : MonoBehaviour
    {
        public static ScoringManager Instance;

        private Dictionary<Player, int> totalScores = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;

            totalScores[Player.Host] = 0;
            totalScores[Player.Opponent] = 0;
        }

        /// <summary>
        /// Calculates the current turn's earned score for a player.
        /// </summary>
        public int CalculateScore(Player player, List<Piece> pieces)
        {
            TurnScoreBreakdown breakdown = ScoringRules.CalculateTurnScore(
                PlacementValidator.ToSeat(player),
                PieceStatusFactory.FromPieces(pieces),
                SeasonMapping.Current());

            if (breakdown.EarnedFlowBonus)
                DebugLogger.Log($">>> {player} earned a Flow Bonus (+{ScoringRules.FlowBonusPoints})");

            if (breakdown.EarnedEmptyHarmonyBonus)
                DebugLogger.Log($">>> {player} earned an Empty Harmony Bonus (+{ScoringRules.EmptyHarmonyBonusPoints})");

            totalScores[player] += breakdown.Total;
            return breakdown.Total;
        }

        /// <summary>
        /// Returns a player's total cumulative score.
        /// </summary>
        public int GetTotalScore(Player player)
        {
            return totalScores.ContainsKey(player) ? totalScores[player] : 0;
        }

        /// <summary>
        /// Return full scoreboard.
        /// </summary>
        public Dictionary<Player, int> GetAllScores()
        {
            return new Dictionary<Player, int>(totalScores);
        }

        /// <summary>
        /// Award bonus points directly.
        /// </summary>
        public void AwardBonus(Player player, int points)
        {
            if (points <= 0) return;
            if (!totalScores.ContainsKey(player)) totalScores[player] = 0;
            totalScores[player] += points;
        }
    }
}
