using System.Collections.Generic;
using PaiSho.Pieces;

namespace PaiSho.Domain
{
    public readonly struct TurnScoreBreakdown
    {
        public int PiecePoints { get; }
        public int PoeticBonus { get; }
        public int Total => PiecePoints + PoeticBonus;
        public bool EarnedFlowBonus { get; }
        public bool EarnedEmptyHarmonyBonus { get; }

        public TurnScoreBreakdown(
            int piecePoints,
            int poeticBonus,
            bool earnedFlowBonus,
            bool earnedEmptyHarmonyBonus)
        {
            PiecePoints = piecePoints;
            PoeticBonus = poeticBonus;
            EarnedFlowBonus = earnedFlowBonus;
            EarnedEmptyHarmonyBonus = earnedEmptyHarmonyBonus;
        }
    }

    /// <summary>Deterministic turn scoring (piece values + poetic bonuses).</summary>
    public static class ScoringRules
    {
        public const int FlowBonusThreshold = 5;
        public const int FlowBonusPoints = 3;
        public const int EmptyHarmonyBonusPoints = 2;
        public const int BloomingLotusBonus = 2;
        public const int InSeasonBonus = 1;
        public const int RevivalBonus = 2;

        public static int ScorePiece(PieceStatus piece, GardenSeason season)
        {
            if (piece.IsGhost)
                return 0;

            int score = SeasonRules.BaseScoreValue(piece.Type, season, piece.PointValue);

            if (piece.Type == PieceType.Lotus && piece.LotusBlooming)
                score += BloomingLotusBonus;

            if (SeasonRules.IsInSeason(piece.Type, season))
                score += InSeasonBonus;

            if (piece.PreviousWiltLevel > piece.WiltLevel && piece.WiltLevel < 2)
                score += RevivalBonus;

            return score;
        }

        public static TurnScoreBreakdown CalculateTurnScore(
            Seat seat,
            IReadOnlyList<PieceStatus> pieces,
            GardenSeason season)
        {
            int piecePoints = 0;
            int harmonized = 0;
            int totalTiles = 0;

            if (pieces != null)
            {
                foreach (PieceStatus piece in pieces)
                {
                    if (piece.Seat != seat || piece.IsGhost)
                        continue;

                    totalTiles++;
                    piecePoints += ScorePiece(piece, season);

                    if (piece.InHarmony)
                        harmonized++;
                }
            }

            bool flow = harmonized >= FlowBonusThreshold;
            bool emptyHarmony = harmonized > 0 && harmonized == totalTiles;
            int poetic = 0;
            if (flow)
                poetic += FlowBonusPoints;
            if (emptyHarmony)
                poetic += EmptyHarmonyBonusPoints;

            return new TurnScoreBreakdown(piecePoints, poetic, flow, emptyHarmony);
        }
    }
}
