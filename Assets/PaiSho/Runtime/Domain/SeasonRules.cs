using System.Collections.Generic;
using PaiSho.Pieces;

namespace PaiSho.Domain
{
    public readonly struct SeasonalTurnContext
    {
        public GardenSeason Season { get; }
        public bool PlacedThisTurn { get; }
        public int MovedTileCount { get; }
        public int HarmoniesThisTurn { get; }
        public int WiltedRevived { get; }
        public int TileCount { get; }
        public bool HasFreezeWiltPiece { get; }
        public bool HasFreshHarmony { get; }

        public SeasonalTurnContext(
            GardenSeason season,
            bool placedThisTurn,
            int movedTileCount,
            int harmoniesThisTurn,
            int wiltedRevived,
            int tileCount,
            bool hasFreezeWiltPiece,
            bool hasFreshHarmony)
        {
            Season = season;
            PlacedThisTurn = placedThisTurn;
            MovedTileCount = movedTileCount;
            HarmoniesThisTurn = harmoniesThisTurn;
            WiltedRevived = wiltedRevived;
            TileCount = tileCount;
            HasFreezeWiltPiece = hasFreezeWiltPiece;
            HasFreshHarmony = hasFreshHarmony;
        }
    }

    public readonly struct SeasonalBonusResult
    {
        public int ScoreBonus { get; }
        public int MomentumBonus { get; }

        public SeasonalBonusResult(int scoreBonus, int momentumBonus)
        {
            ScoreBonus = scoreBonus;
            MomentumBonus = momentumBonus;
        }
    }

    /// <summary>Season calendar, in-season pieces, movement/score modifiers, turn bonuses.</summary>
    public static class SeasonRules
    {
        public const int TurnsPerSeason = 6;

        public static GardenSeason Next(GardenSeason season) =>
            (GardenSeason)(((int)season + 1) % 4);

        public static bool ShouldRotate(int turnCounterAfterIncrement) =>
            turnCounterAfterIncrement >= TurnsPerSeason;

        public static bool IsInSeason(PieceType type, GardenSeason season)
        {
            switch (season)
            {
                case GardenSeason.Spring:
                    return type == PieceType.Jasmine
                        || type == PieceType.Lily
                        || type == PieceType.Jade;
                case GardenSeason.Summer:
                    return type == PieceType.Boat || type == PieceType.Knotweed;
                case GardenSeason.Autumn:
                    return type == PieceType.Rose
                        || type == PieceType.Chrysanthemum
                        || type == PieceType.Rhododendron;
                case GardenSeason.Winter:
                    return type == PieceType.Rock
                        || type == PieceType.Wheel
                        || type == PieceType.Lotus;
                default:
                    return false;
            }
        }

        /// <summary>Base movement range multiplier used by MovementManager (1 normal, 2 in-season spring flowers).</summary>
        public static int ModifiedMovementRange(PieceType type, GardenSeason season)
        {
            if (season == GardenSeason.Spring && IsInSeason(type, GardenSeason.Spring))
                return 2;
            return 1;
        }

        /// <summary>Winter accents/lotus get +1 to their base point value.</summary>
        public static int BaseScoreValue(PieceType type, GardenSeason season, int pointValue)
        {
            if (season == GardenSeason.Winter && IsInSeason(type, GardenSeason.Winter))
                return pointValue + 1;
            return pointValue;
        }

        public static SeasonalTurnContext BuildContext(
            GardenSeason season,
            Seat seat,
            IReadOnlyList<PieceStatus> pieces,
            bool placedThisTurn,
            int movedTileCount)
        {
            int harmonies = 0;
            int wiltedRevived = 0;
            int tileCount = 0;
            bool hasFreeze = false;
            bool hasFreshHarmony = false;

            if (pieces != null)
            {
                foreach (PieceStatus piece in pieces)
                {
                    if (piece.Seat != seat || piece.IsGhost)
                        continue;

                    tileCount++;

                    if (piece.WiltLevel < piece.PreviousWiltLevel)
                        wiltedRevived++;

                    if (piece.InHarmony)
                        harmonies++;

                    if (piece.FreezeWiltNextTurn)
                        hasFreeze = true;

                    if (piece.InHarmony && piece.WiltLevel == 0 && piece.TurnsSinceMoved < 2)
                        hasFreshHarmony = true;
                }
            }

            return new SeasonalTurnContext(
                season,
                placedThisTurn,
                movedTileCount,
                harmonies,
                wiltedRevived,
                tileCount,
                hasFreeze,
                hasFreshHarmony);
        }

        public static SeasonalBonusResult EvaluateBonuses(SeasonalTurnContext ctx)
        {
            int scoreBonus = 0;
            int momentumBonus = 0;

            switch (ctx.Season)
            {
                case GardenSeason.Spring:
                    if (ctx.PlacedThisTurn)
                        scoreBonus++;
                    if (ctx.HarmoniesThisTurn >= 1)
                        momentumBonus++;
                    if (ctx.HarmoniesThisTurn >= 3)
                        scoreBonus += 2;
                    break;

                case GardenSeason.Summer:
                    if (ctx.HasFreezeWiltPiece)
                        scoreBonus++;
                    if (ctx.WiltedRevived >= 1)
                        momentumBonus++;
                    if (ctx.WiltedRevived >= 2)
                        scoreBonus += 2;
                    break;

                case GardenSeason.Autumn:
                    if (ctx.WiltedRevived >= 1)
                        scoreBonus++;
                    if (ctx.HasFreshHarmony)
                        momentumBonus++;
                    if (ctx.WiltedRevived >= 2 && ctx.HarmoniesThisTurn >= 2)
                        scoreBonus += 2;
                    break;

                case GardenSeason.Winter:
                    if (ctx.MovedTileCount == 1)
                        scoreBonus++;
                    if (ctx.MovedTileCount == 1 && ctx.HarmoniesThisTurn >= 1)
                        momentumBonus++;
                    if (ctx.HarmoniesThisTurn > 0
                        && ctx.TileCount > 0
                        && ctx.HarmoniesThisTurn == ctx.TileCount)
                    {
                        scoreBonus += 2;
                    }
                    break;
            }

            return new SeasonalBonusResult(scoreBonus, momentumBonus);
        }
    }
}
