using System.Collections.Generic;
using PaiSho.Domain;
using PaiSho.Pieces;
using Xunit;

namespace PaiSho.Rules.Tests
{
    public class VictoryRulesTests
    {
        private static BoardPiece Flower(Seat seat, PieceType type, int x, int z, bool ghost = false) =>
            new BoardPiece(seat, type, BoardCoords.ToCoordinate(x, z), ghost);

        /// <summary>
        /// Ortho ring paths cannot connect the four cardinal center-neighbors directly
        /// (they only touch diagonally). A minimal encircling cycle uses the 8 cells
        /// around the center port.
        /// </summary>
        private static List<BoardPiece> EightRing(Seat seat, PieceType type)
        {
            return new List<BoardPiece>
            {
                Flower(seat, type, 1, 0),
                Flower(seat, type, 1, 1),
                Flower(seat, type, 0, 1),
                Flower(seat, type, -1, 1),
                Flower(seat, type, -1, 0),
                Flower(seat, type, -1, -1),
                Flower(seat, type, 0, -1),
                Flower(seat, type, 1, -1),
            };
        }

        [Fact]
        public void EightMatchingFlowersAroundCenter_Wins()
        {
            var pieces = EightRing(Seat.Host, PieceType.Jasmine);
            Assert.True(VictoryRules.HasHarmonicRing(Seat.Host, pieces));
            Assert.False(VictoryRules.HasHarmonicRing(Seat.Opponent, pieces));
        }

        [Fact]
        public void FourCardinalsAlone_DoNotWin_BecauseNotOrthoConnected()
        {
            var pieces = new List<BoardPiece>
            {
                Flower(Seat.Host, PieceType.Jasmine, 1, 0),
                Flower(Seat.Host, PieceType.Jasmine, -1, 0),
                Flower(Seat.Host, PieceType.Jasmine, 0, 1),
                Flower(Seat.Host, PieceType.Jasmine, 0, -1),
            };

            Assert.True(VictoryRules.IsCenterPortEncircled(new HashSet<int>
            {
                pieces[0].Coordinate, pieces[1].Coordinate, pieces[2].Coordinate, pieces[3].Coordinate
            }));
            Assert.False(VictoryRules.HasHarmonicRing(Seat.Host, pieces));
        }

        [Fact]
        public void MissingCornerBreaksOrthoCycle()
        {
            var pieces = EightRing(Seat.Host, PieceType.Jade);
            pieces.RemoveAll(p =>
            {
                GridPos g = BoardCoords.FromCoordinate(p.Coordinate);
                return g.X == 1 && g.Z == 1;
            });

            Assert.False(VictoryRules.HasHarmonicRing(Seat.Host, pieces));
        }

        [Fact]
        public void DifferentTypesAroundCenter_DoNotFormRing()
        {
            var pieces = EightRing(Seat.Host, PieceType.Jasmine);
            // Break one harmony edge by changing a corner type.
            pieces[1] = Flower(Seat.Host, PieceType.Rose, 1, 1);
            Assert.False(VictoryRules.HasHarmonicRing(Seat.Host, pieces));
        }

        [Fact]
        public void GhostPieces_AreIgnored()
        {
            var pieces = EightRing(Seat.Host, PieceType.Jasmine);
            pieces[0] = Flower(Seat.Host, PieceType.Jasmine, 1, 0, ghost: true);
            Assert.False(VictoryRules.HasHarmonicRing(Seat.Host, pieces));
        }

        [Fact]
        public void OpponentPieces_DoNotCountForSeat()
        {
            var pieces = EightRing(Seat.Host, PieceType.Lily);
            pieces[2] = Flower(Seat.Opponent, PieceType.Lily, 0, 1);
            Assert.False(VictoryRules.HasHarmonicRing(Seat.Host, pieces));
        }

        [Fact]
        public void ExtraOuterPiece_DoesNotBreakWinningRing()
        {
            var pieces = EightRing(Seat.Opponent, PieceType.Rose);
            pieces.Add(Flower(Seat.Opponent, PieceType.Rose, 2, 0));
            Assert.True(VictoryRules.HasHarmonicRing(Seat.Opponent, pieces));
        }

        [Fact]
        public void IsCenterPortEncircled_RequiresAllOrthoNeighbors()
        {
            var loop = new HashSet<int>
            {
                BoardCoords.ToCoordinate(1, 0),
                BoardCoords.ToCoordinate(-1, 0),
                BoardCoords.ToCoordinate(0, 1),
                BoardCoords.ToCoordinate(0, -1),
            };
            Assert.True(VictoryRules.IsCenterPortEncircled(loop));

            loop.Remove(BoardCoords.ToCoordinate(0, 1));
            Assert.False(VictoryRules.IsCenterPortEncircled(loop));
        }

        [Fact]
        public void EmptyOrNullBoard_NoVictory()
        {
            Assert.False(VictoryRules.HasHarmonicRing(Seat.Host, new List<BoardPiece>()));
            Assert.False(VictoryRules.HasHarmonicRing(Seat.Host, null));
        }

        [Fact]
        public void OrthogonalNeighbors_OfCenter_AreFour()
        {
            List<int> ortho = BoardCoords.GetOrthogonalNeighbors(BoardCoords.CenterPortCoordinate);
            Assert.Equal(4, ortho.Count);
            Assert.Contains(BoardCoords.ToCoordinate(1, 0), ortho);
            Assert.Contains(BoardCoords.ToCoordinate(-1, 0), ortho);
            Assert.Contains(BoardCoords.ToCoordinate(0, 1), ortho);
            Assert.Contains(BoardCoords.ToCoordinate(0, -1), ortho);
        }
    }
}
