using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Board;

namespace PaiSho.Game
{
    public class ScoringManager : MonoBehaviour
    {
        public static ScoringManager Instance;

        private Dictionary<Player, int> totalScores = new Dictionary<Player, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;

            totalScores[Player.Host] = 0;
            totalScores[Player.Opponent] = 0;
        }

        public int ComputeLiveScore(Player player, List<Piece> pieces)
        {
            int score = 0;

            foreach (Piece piece in pieces)
            {
                if (piece.Owner != player || piece.IsGhost)
                    continue;

                score += piece.GetScoreValue();

                if (piece.Type == PieceType.Lotus && piece.IsBlooming())
                    score += 2;

                if (SeasonManager.Instance.IsInSeason(piece.Type))
                    score += 1;

                if (piece.PreviousWiltLevel > piece.WiltLevel && piece.WiltLevel < 2)
                    score += 2;
            }

            score += ApplyPoeticBonuses(player, pieces);
            return score;
        }

        public int CalculateScore(Player player, List<Piece> pieces)
        {
            int score = ComputeLiveScore(player, pieces);
            totalScores[player] += score;
            return score;
        }

        public void ResetForNewMatch()
        {
            totalScores[Player.Host] = 0;
            totalScores[Player.Opponent] = 0;
        }

        private int ApplyPoeticBonuses(Player player, List<Piece> pieces)
        {
            int harmonious = 0;
            foreach (Piece piece in pieces)
            {
                if (piece.Owner == player && piece.InHarmony)
                    harmonious++;
            }

            return harmonious >= 5 ? 3 : 0;
        }

        public int GetTotalScore(Player player)
        {
            return totalScores[player];
        }

        public Dictionary<Player, int> GetAllScores()
        {
            return new Dictionary<Player, int>(totalScores);
        }

        public void AwardBonus(Player player, int points)
        {
            if (points <= 0) return;
            if (!totalScores.ContainsKey(player)) return;

            totalScores[player] += points;
        }
    }
}
