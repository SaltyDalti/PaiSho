using System.Collections.Generic;
using PaiSho.Domain;
using PaiSho.Pieces;
using Xunit;

namespace PaiSho.Rules.Tests
{
    public class SeasonRulesTests
    {
        [Theory]
        [InlineData(GardenSeason.Spring, GardenSeason.Summer)]
        [InlineData(GardenSeason.Summer, GardenSeason.Autumn)]
        [InlineData(GardenSeason.Autumn, GardenSeason.Winter)]
        [InlineData(GardenSeason.Winter, GardenSeason.Spring)]
        public void NextSeason_Cycles(GardenSeason from, GardenSeason expected)
        {
            Assert.Equal(expected, SeasonRules.Next(from));
        }

        [Theory]
        [InlineData(5, false)]
        [InlineData(6, true)]
        [InlineData(7, true)]
        public void ShouldRotate_UsesTurnsPerSeason(int counter, bool expected)
        {
            Assert.Equal(SeasonRules.TurnsPerSeason, 6);
            Assert.Equal(expected, SeasonRules.ShouldRotate(counter));
        }

        [Theory]
        [InlineData(PieceType.Jasmine, GardenSeason.Spring, true)]
        [InlineData(PieceType.Boat, GardenSeason.Summer, true)]
        [InlineData(PieceType.Rose, GardenSeason.Autumn, true)]
        [InlineData(PieceType.Lotus, GardenSeason.Winter, true)]
        [InlineData(PieceType.Jasmine, GardenSeason.Winter, false)]
        public void IsInSeason_MatchesCanonicalSets(PieceType type, GardenSeason season, bool expected)
        {
            Assert.Equal(expected, SeasonRules.IsInSeason(type, season));
        }

        [Fact]
        public void SpringFlowers_GetMovementBonus()
        {
            Assert.Equal(2, SeasonRules.ModifiedMovementRange(PieceType.Lily, GardenSeason.Spring));
            Assert.Equal(1, SeasonRules.ModifiedMovementRange(PieceType.Lily, GardenSeason.Summer));
            Assert.Equal(1, SeasonRules.ModifiedMovementRange(PieceType.Rose, GardenSeason.Spring));
        }

        [Fact]
        public void WinterAccents_GetScoreBump()
        {
            Assert.Equal(2, SeasonRules.BaseScoreValue(PieceType.Rock, GardenSeason.Winter, 1));
            Assert.Equal(1, SeasonRules.BaseScoreValue(PieceType.Rock, GardenSeason.Spring, 1));
        }

        [Fact]
        public void SpringBonuses_PlacementAndHarmony()
        {
            var ctx = new SeasonalTurnContext(
                GardenSeason.Spring,
                placedThisTurn: true,
                movedTileCount: 0,
                harmoniesThisTurn: 3,
                wiltedRevived: 0,
                tileCount: 3,
                hasFreezeWiltPiece: false,
                hasFreshHarmony: false);

            SeasonalBonusResult result = SeasonRules.EvaluateBonuses(ctx);
            Assert.Equal(3, result.ScoreBonus); // +1 place +2 for 3 harmonies
            Assert.Equal(1, result.MomentumBonus);
        }

        [Fact]
        public void SummerBonuses_FreezeAndRevival()
        {
            var ctx = new SeasonalTurnContext(
                GardenSeason.Summer,
                false, 0, 0,
                wiltedRevived: 2,
                tileCount: 4,
                hasFreezeWiltPiece: true,
                hasFreshHarmony: false);

            SeasonalBonusResult result = SeasonRules.EvaluateBonuses(ctx);
            Assert.Equal(3, result.ScoreBonus); // +1 freeze +2 double revival
            Assert.Equal(1, result.MomentumBonus);
        }

        [Fact]
        public void WinterBonuses_SingleMoveAndFullHarmony()
        {
            var ctx = new SeasonalTurnContext(
                GardenSeason.Winter,
                false,
                movedTileCount: 1,
                harmoniesThisTurn: 2,
                wiltedRevived: 0,
                tileCount: 2,
                hasFreezeWiltPiece: false,
                hasFreshHarmony: false);

            SeasonalBonusResult result = SeasonRules.EvaluateBonuses(ctx);
            Assert.Equal(3, result.ScoreBonus); // +1 single move +2 all harmonized
            Assert.Equal(1, result.MomentumBonus);
        }

        [Fact]
        public void BuildContext_AggregatesSeatStats()
        {
            var pieces = new List<PieceStatus>
            {
                new PieceStatus(Seat.Host, PieceType.Jasmine, 1, inHarmony: true, wiltLevel: 0, previousWiltLevel: 1),
                new PieceStatus(Seat.Host, PieceType.Lily, 2, inHarmony: true, wiltLevel: 0, previousWiltLevel: 0, turnsSinceMoved: 0),
                new PieceStatus(Seat.Opponent, PieceType.Rose, 3, inHarmony: true, wiltLevel: 0, previousWiltLevel: 2),
            };

            SeasonalTurnContext ctx = SeasonRules.BuildContext(
                GardenSeason.Autumn, Seat.Host, pieces, placedThisTurn: false, movedTileCount: 0);

            Assert.Equal(2, ctx.TileCount);
            Assert.Equal(2, ctx.HarmoniesThisTurn);
            Assert.Equal(1, ctx.WiltedRevived);
            Assert.True(ctx.HasFreshHarmony);
        }
    }
}
