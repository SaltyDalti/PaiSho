using PaiSho.Domain;
using PaiSho.Pieces;
using Xunit;

namespace PaiSho.Rules.Tests
{
    public class MatchRulesTests
    {
        [Fact]
        public void PortfolioDefault_ParksHouseExtras()
        {
            using var _ = new HouseExtrasScope(enabled: false);

            Assert.False(MatchRules.HouseExtrasEnabled);
            Assert.False(SeasonRules.IsInSeason(PieceType.Jasmine, GardenSeason.Spring));
            Assert.Equal(1, SeasonRules.ModifiedMovementRange(PieceType.Lily, GardenSeason.Spring));
            Assert.Equal(1, SeasonRules.BaseScoreValue(PieceType.Rock, GardenSeason.Winter, 1));
            Assert.False(SeasonRules.ShouldRotate(6));
            Assert.True(CaptureRules.CanBeCaptured(PieceType.Boat, GardenSeason.Summer));
            Assert.True(CaptureRules.CanBeDisharmonized(PieceType.Rose, GardenSeason.Autumn));

            var ctx = new SeasonalTurnContext(
                GardenSeason.Spring,
                placedThisTurn: true,
                movedTileCount: 0,
                harmoniesThisTurn: 3,
                wiltedRevived: 0,
                tileCount: 3,
                hasFreezeWiltPiece: false,
                hasFreshHarmony: false);
            SeasonalBonusResult bonuses = SeasonRules.EvaluateBonuses(ctx);
            Assert.Equal(0, bonuses.ScoreBonus);
            Assert.Equal(0, bonuses.MomentumBonus);

            var jasmine = new PieceStatus(Seat.Host, PieceType.Jasmine, 0, wiltLevel: 1, previousWiltLevel: 2);
            // Blooming lotus bonus still applies; in-season and revival do not.
            Assert.Equal(1, ScoringRules.ScorePiece(jasmine, GardenSeason.Spring));
        }

        [Fact]
        public void EnableHouseExtras_RestoresSeasonEffects()
        {
            using var _ = new HouseExtrasScope(enabled: true);

            Assert.True(MatchRules.HouseExtrasEnabled);
            Assert.True(SeasonRules.IsInSeason(PieceType.Jasmine, GardenSeason.Spring));
            Assert.Equal(2, SeasonRules.ModifiedMovementRange(PieceType.Lily, GardenSeason.Spring));
            Assert.False(CaptureRules.CanBeCaptured(PieceType.Boat, GardenSeason.Summer));
            Assert.True(SeasonRules.ShouldRotate(6));
        }

        [Fact]
        public void CanonicalSeasonTable_SurvivesWhenParked()
        {
            using var _ = new HouseExtrasScope(enabled: false);

            Assert.True(SeasonRules.IsInSeasonCanonical(PieceType.Jasmine, GardenSeason.Spring));
            Assert.True(SeasonRules.IsInSeasonCanonical(PieceType.Boat, GardenSeason.Summer));
            Assert.False(SeasonRules.IsInSeasonCanonical(PieceType.Jasmine, GardenSeason.Winter));
        }
    }
}
