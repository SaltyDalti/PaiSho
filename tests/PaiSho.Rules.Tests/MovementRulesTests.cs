using System.Collections.Generic;
using System.Linq;
using PaiSho.Domain;
using PaiSho.Pieces;
using Xunit;

namespace PaiSho.Rules.Tests
{
    public class MovementRulesTests
    {
        private static readonly HashSet<int> Empty = new HashSet<int>();

        private static bool Occupied(HashSet<int> occupied, int coord) => occupied.Contains(coord);

        [Fact]
        public void Rock_HasNoMoves()
        {
            int start = BoardCoords.ToCoordinate(0, 0);
            var moves = MovementRules.GetLegalMoves(PieceType.Rock, start, 0, c => false);
            Assert.Empty(moves);
        }

        [Fact]
        public void Jasmine_MovesOrthogonallyUpToThree()
        {
            int start = BoardCoords.ToCoordinate(0, 0);
            var moves = MovementRules.GetLegalMoves(PieceType.Jasmine, start, 0, c => Occupied(Empty, c));

            Assert.Contains(BoardCoords.ToCoordinate(3, 0), moves);
            Assert.Contains(BoardCoords.ToCoordinate(0, 3), moves);
            Assert.DoesNotContain(BoardCoords.ToCoordinate(4, 0), moves);
            Assert.DoesNotContain(BoardCoords.ToCoordinate(1, 1), moves); // diagonal not allowed
        }

        [Fact]
        public void Jasmine_SeasonalBonus_ExtendsRange()
        {
            int start = BoardCoords.ToCoordinate(0, 0);
            var moves = MovementRules.GetLegalMoves(PieceType.Jasmine, start, seasonalBonus: 1, c => false);
            Assert.Contains(BoardCoords.ToCoordinate(4, 0), moves);
        }

        [Fact]
        public void StraightLine_BlockedByOccupant()
        {
            int start = BoardCoords.ToCoordinate(0, 0);
            var occupied = new HashSet<int> { BoardCoords.ToCoordinate(2, 0) };

            var moves = MovementRules.GetLegalMoves(PieceType.Jasmine, start, 0, c => Occupied(occupied, c));

            Assert.Contains(BoardCoords.ToCoordinate(1, 0), moves);
            Assert.DoesNotContain(BoardCoords.ToCoordinate(2, 0), moves);
            Assert.DoesNotContain(BoardCoords.ToCoordinate(3, 0), moves);
        }

        [Fact]
        public void Orchid_CanJumpOverOccupant()
        {
            int start = BoardCoords.ToCoordinate(0, 0);
            var occupied = new HashSet<int> { BoardCoords.ToCoordinate(2, 0) };

            var moves = MovementRules.GetLegalMoves(PieceType.Orchid, start, 0, c => Occupied(occupied, c));

            Assert.DoesNotContain(BoardCoords.ToCoordinate(2, 0), moves);
            Assert.Contains(BoardCoords.ToCoordinate(3, 0), moves);
        }

        [Fact]
        public void Lily_LShape_RequiresClearMidAndTarget()
        {
            int start = BoardCoords.ToCoordinate(0, 0);
            var open = MovementRules.GetLegalMoves(PieceType.Lily, start, 0, c => false);
            Assert.Contains(BoardCoords.ToCoordinate(2, 1), open);

            var blockedMid = new HashSet<int> { BoardCoords.ToCoordinate(1, 0) }; // mid for (2,1) when dx=2
            var blocked = MovementRules.GetLegalMoves(PieceType.Lily, start, 0, c => Occupied(blockedMid, c));
            Assert.DoesNotContain(BoardCoords.ToCoordinate(2, 1), blocked);
        }

        [Fact]
        public void Boat_CanPassThroughOneOccupiedThenLandBeyond()
        {
            int start = BoardCoords.ToCoordinate(0, 0);
            int blocker = BoardCoords.ToCoordinate(1, 0);
            var occupied = new HashSet<int> { blocker };

            var moves = MovementRules.GetLegalMoves(PieceType.Boat, start, 0, c => Occupied(occupied, c));

            Assert.DoesNotContain(blocker, moves);
            Assert.Contains(BoardCoords.ToCoordinate(2, 0), moves);
        }

        [Fact]
        public void Jade_IncludesDiagonal()
        {
            int start = BoardCoords.ToCoordinate(0, 0);
            var moves = MovementRules.GetLegalMoves(PieceType.Jade, start, 0, c => false);
            Assert.Contains(BoardCoords.ToCoordinate(1, 1), moves);
            Assert.True(moves.Count > 8);
        }

        [Fact]
        public void GetLegalMoves_NullOccupancy_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                MovementRules.GetLegalMoves(PieceType.Jasmine, BoardCoords.CenterPortCoordinate, 0, null));
        }
    }
}
