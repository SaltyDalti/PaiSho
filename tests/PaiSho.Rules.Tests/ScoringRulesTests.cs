using System.Collections.Generic;
using PaiSho.Domain;
using PaiSho.Pieces;
using Xunit;

namespace PaiSho.Rules.Tests
{
    public class ScoringRulesTests
    {
        private static PieceStatus Host(
            PieceType type,
            int coord = 0,
            bool harmony = false,
            bool blooming = false,
            int wilt = 0,
            int prevWilt = 0,
            int points = 1) =>
            new PieceStatus(
                Seat.Host, type, coord,
                lotusBlooming: blooming,
                inHarmony: harmony,
                wiltLevel: wilt,
                previousWiltLevel: prevWilt,
                pointValue: points);

        [Fact]
        public void ScorePiece_AddsInSeasonAndRevivalAndBloom()
        {
            // Winter lotus: base+1 (winter), +1 in-season, +2 blooming = 1+1+1+2 = 5
            var lotus = Host(PieceType.Lotus, blooming: true);
            Assert.Equal(5, ScoringRules.ScorePiece(lotus, GardenSeason.Winter));

            // Revival: previous wilt 2 -> wilt 1
            // base 1 + spring in-season 1 + revival 2 = 4
            var revived = Host(PieceType.Jasmine, wilt: 1, prevWilt: 2);
            Assert.Equal(4, ScoringRules.ScorePiece(revived, GardenSeason.Spring));
        }

        [Fact]
        public void Ghosts_ScoreZero()
        {
            var ghost = new PieceStatus(Seat.Host, PieceType.Jade, 0, isGhost: true);
            Assert.Equal(0, ScoringRules.ScorePiece(ghost, GardenSeason.Spring));
        }

        [Fact]
        public void FlowBonus_AtFiveHarmonies()
        {
            var pieces = new List<PieceStatus>();
            for (int i = 0; i < 5; i++)
                pieces.Add(Host(PieceType.Rose, coord: i, harmony: true));

            TurnScoreBreakdown score = ScoringRules.CalculateTurnScore(
                Seat.Host, pieces, GardenSeason.Summer);

            Assert.True(score.EarnedFlowBonus);
            // All five are in harmony → Empty Harmony also stacks.
            Assert.True(score.EarnedEmptyHarmonyBonus);
            Assert.Equal(
                ScoringRules.FlowBonusPoints + ScoringRules.EmptyHarmonyBonusPoints,
                score.PoeticBonus);
        }

        [Fact]
        public void EmptyHarmonyBonus_WhenAllTilesHarmonize()
        {
            var pieces = new List<PieceStatus>
            {
                Host(PieceType.Boat, 1, harmony: true),
                Host(PieceType.Rock, 2, harmony: true),
            };

            TurnScoreBreakdown score = ScoringRules.CalculateTurnScore(
                Seat.Host, pieces, GardenSeason.Spring);

            Assert.True(score.EarnedEmptyHarmonyBonus);
            Assert.False(score.EarnedFlowBonus);
            Assert.Equal(ScoringRules.EmptyHarmonyBonusPoints, score.PoeticBonus);
        }

        [Fact]
        public void CalculateTurnScore_IgnoresOtherSeat()
        {
            var pieces = new List<PieceStatus>
            {
                Host(PieceType.Jasmine, 1),
                new PieceStatus(Seat.Opponent, PieceType.Jasmine, 2),
            };

            TurnScoreBreakdown score = ScoringRules.CalculateTurnScore(
                Seat.Host, pieces, GardenSeason.Autumn);

            // Host jasmine out of season autumn: base 1 only
            Assert.Equal(1, score.PiecePoints);
            Assert.Equal(1, score.Total);
        }

        [Fact]
        public void BothPoeticBonuses_CanStack()
        {
            var pieces = new List<PieceStatus>();
            for (int i = 0; i < 5; i++)
                pieces.Add(Host(PieceType.Wheel, coord: i, harmony: true));

            TurnScoreBreakdown score = ScoringRules.CalculateTurnScore(
                Seat.Host, pieces, GardenSeason.Spring);

            Assert.True(score.EarnedFlowBonus);
            Assert.True(score.EarnedEmptyHarmonyBonus);
            Assert.Equal(
                ScoringRules.FlowBonusPoints + ScoringRules.EmptyHarmonyBonusPoints,
                score.PoeticBonus);
        }
    }
}
