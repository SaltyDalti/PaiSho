using System.Collections.Generic;
using System.Linq;
using PaiSho.Domain;
using PaiSho.Pieces;
using Xunit;

namespace PaiSho.Rules.Tests
{
    public class CaptureRulesTests
    {
        private static BoardPiece P(Seat seat, PieceType type, int x, int z, bool blooming = false) =>
            new BoardPiece(seat, type, BoardCoords.ToCoordinate(x, z), false, blooming);

        [Theory]
        [InlineData(PieceType.Boat, GardenSeason.Summer, false)]
        [InlineData(PieceType.Knotweed, GardenSeason.Summer, false)]
        [InlineData(PieceType.Boat, GardenSeason.Spring, true)]
        [InlineData(PieceType.Jasmine, GardenSeason.Summer, true)]
        public void SeasonImmunity_BoatAndKnotweedInSummer(PieceType type, GardenSeason season, bool expected)
        {
            Assert.Equal(expected, CaptureRules.CanBeCaptured(type, season));
        }

        [Theory]
        [InlineData(PieceType.Rose, GardenSeason.Autumn, false)]
        [InlineData(PieceType.Chrysanthemum, GardenSeason.Autumn, false)]
        [InlineData(PieceType.Rhododendron, GardenSeason.Autumn, false)]
        [InlineData(PieceType.Rose, GardenSeason.Spring, true)]
        [InlineData(PieceType.Jasmine, GardenSeason.Autumn, true)]
        public void AutumnDisharmonyResistance(PieceType type, GardenSeason season, bool expected)
        {
            Assert.Equal(expected, CaptureRules.CanBeDisharmonized(type, season));
        }

        [Fact]
        public void OrthoDisharmonyNeighbor_IsCaptureTarget()
        {
            BoardPiece attacker = P(Seat.Host, PieceType.Jasmine, 0, 0);
            BoardPiece enemy = P(Seat.Opponent, PieceType.Rose, 1, 0);
            var board = new List<BoardPiece> { attacker, enemy };

            List<CaptureOpportunity> targets = CaptureRules.FindCaptureTargets(
                attacker, board, GardenSeason.Spring);

            Assert.Single(targets);
            Assert.Equal(enemy.Coordinate, targets[0].Target.Coordinate);
        }

        [Fact]
        public void DiagonalEnemy_IsNotCaptureTarget()
        {
            BoardPiece attacker = P(Seat.Host, PieceType.Jasmine, 0, 0);
            BoardPiece enemy = P(Seat.Opponent, PieceType.Rose, 1, 1);
            var board = new List<BoardPiece> { attacker, enemy };

            Assert.Empty(CaptureRules.FindCaptureTargets(attacker, board, GardenSeason.Spring));
        }

        [Fact]
        public void SameTypeEnemyFlowers_NotDisharmony_NoCapture()
        {
            BoardPiece attacker = P(Seat.Host, PieceType.Jasmine, 0, 0);
            BoardPiece enemy = P(Seat.Opponent, PieceType.Jasmine, 1, 0);
            var board = new List<BoardPiece> { attacker, enemy };

            Assert.Empty(CaptureRules.FindCaptureTargets(attacker, board, GardenSeason.Spring));
            Assert.Equal(
                CaptureDenyReason.NotDisharmony,
                CaptureRules.EvaluatePair(attacker, enemy, GardenSeason.Spring));
        }

        [Fact]
        public void SummerBoat_IsSeasonImmune()
        {
            BoardPiece attacker = P(Seat.Host, PieceType.Jade, 0, 0);
            BoardPiece boat = P(Seat.Opponent, PieceType.Boat, 0, 1);
            var board = new List<BoardPiece> { attacker, boat };

            Assert.Empty(CaptureRules.FindCaptureTargets(attacker, board, GardenSeason.Summer));
            Assert.Equal(
                CaptureDenyReason.SeasonImmune,
                CaptureRules.EvaluatePair(attacker, boat, GardenSeason.Summer));
        }

        [Fact]
        public void AllyNeighbor_NotCapturable()
        {
            BoardPiece attacker = P(Seat.Host, PieceType.Lily, 0, 0);
            BoardPiece ally = P(Seat.Host, PieceType.Rose, 1, 0);
            var board = new List<BoardPiece> { attacker, ally };

            Assert.Empty(CaptureRules.FindCaptureTargets(attacker, board, GardenSeason.Spring));
            Assert.Equal(
                CaptureDenyReason.SameSeat,
                CaptureRules.EvaluatePair(attacker, ally, GardenSeason.Spring));
        }

        [Fact]
        public void BloomingLotus_BlocksCaptureOfCompatibleEnemyFlower()
        {
            BoardPiece lotus = P(Seat.Host, PieceType.Lotus, 0, 0, blooming: true);
            BoardPiece enemy = P(Seat.Opponent, PieceType.Lily, 1, 0);
            var board = new List<BoardPiece> { lotus, enemy };

            Assert.Empty(CaptureRules.FindCaptureTargets(lotus, board, GardenSeason.Spring));
        }

        [Fact]
        public void GhostPieces_IgnoredOnBoard()
        {
            BoardPiece attacker = P(Seat.Host, PieceType.Rose, 0, 0);
            var ghost = new BoardPiece(
                Seat.Opponent, PieceType.Jade, BoardCoords.ToCoordinate(1, 0), isGhost: true);
            var board = new List<BoardPiece> { attacker, ghost };

            Assert.Empty(CaptureRules.FindCaptureTargets(attacker, board, GardenSeason.Spring));
        }

        [Fact]
        public void MultipleOrthoEnemies_AllReturned()
        {
            BoardPiece attacker = P(Seat.Host, PieceType.Wheel, 0, 0);
            var board = new List<BoardPiece>
            {
                attacker,
                P(Seat.Opponent, PieceType.Boat, 1, 0),
                P(Seat.Opponent, PieceType.Rock, 0, 1),
                P(Seat.Opponent, PieceType.Jasmine, -1, 0), // same-type? Wheel vs Jasmine = disharmony
            };

            List<CaptureOpportunity> targets = CaptureRules.FindCaptureTargets(
                attacker, board, GardenSeason.Spring);

            Assert.Equal(3, targets.Count);
            Assert.Equal(3, targets.Select(t => t.Target.Coordinate).Distinct().Count());
        }
    }
}
