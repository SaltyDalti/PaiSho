using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class ScoringManager : MonoBehaviour
    {
        public static ScoringManager Instance;

        private Dictionary<Player, int> totalScores = new ();

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
            int score = 0;

            foreach (Piece piece in pieces)
            {
                if (piece.Owner != player || piece.IsGhost) continue;

                score += piece.GetScoreValue();

                if (piece.Type == PieceType.Lotus && piece.IsBlooming())
                    score += 2;

                if (SeasonManager.Instance.IsInSeason(piece.Type))
                    score += 1;

                if (piece.PreviousWiltLevel > piece.WiltLevel && piece.WiltLevel < 2)
                    score += 2; // Revival bonus
            }

            score += ApplyPoeticBonuses(player, pieces);

            totalScores[player] += score;
            return score;
        }

        /// <summary>
        /// Poetic bonuses reward graceful board choreography.
        /// </summary>
        private int ApplyPoeticBonuses(Player player, List<Piece> pieces)
        {
            int bonus = 0;

            int harmonizedTiles = pieces.FindAll(p => p.Owner == player && p.InHarmony).Count;
            int totalTiles = pieces.FindAll(p => p.Owner == player).Count;

            // Flow Bonus: many tiles harmonizing
            if (harmonizedTiles >= 5)
            {
                bonus += 3;
                DebugLogger.Log($">>> {player} earned a Flow Bonus (+3)");
            }

            // Empty Harmony Bonus: solo harmonies with no neighbors
            if (harmonizedTiles > 0 && harmonizedTiles == totalTiles)
            {
                bonus += 2;
                DebugLogger.Log($">>> {player} earned an Empty Harmony Bonus (+2)");
            }

            // Future bonus types can be added here

            return bonus;
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
