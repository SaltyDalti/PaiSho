using System.Collections.Generic;
using PaiSho.Domain;
using Xunit;

namespace PaiSho.Rules.Tests
{
    public class BoardCoordsTests
    {
        [Fact]
        public void Center_IsZeroZeroEncoding()
        {
            Assert.Equal(BoardCoords.ToCoordinate(0, 0), BoardCoords.CenterPortCoordinate);
            Assert.Equal(new GridPos(0, 0), BoardCoords.FromCoordinate(BoardCoords.CenterPortCoordinate));
        }

        [Theory]
        [InlineData(0, 0, true)]
        [InlineData(9, 0, true)]
        [InlineData(0, 9, true)]
        [InlineData(9, 3, true)]   // manhattan 12
        [InlineData(9, 4, false)]  // manhattan 13 — outside garden shape
        [InlineData(10, 0, false)] // outside bounding box
        public void LegalPoints_MatchGardenShape(int x, int z, bool expected)
        {
            int coord = BoardCoords.ToCoordinate(x, z);
            Assert.Equal(expected, BoardCoords.IsValidPointCoordinate(coord));
        }

        [Fact]
        public void RoundTrip_Coordinates()
        {
            for (int x = -9; x <= 9; x++)
            {
                for (int z = -9; z <= 9; z++)
                {
                    int coord = BoardCoords.ToCoordinate(x, z);
                    GridPos back = BoardCoords.FromCoordinate(coord);
                    Assert.Equal(x, back.X);
                    Assert.Equal(z, back.Z);
                }
            }
        }

        [Fact]
        public void CoordStride_IsTwenty_NotBoardWidth()
        {
            Assert.Equal(20, BoardCoords.CoordStride);
            Assert.Equal(19, BoardCoords.BoardWidth);
        }

        [Fact]
        public void Neighbors_OfCenter_AreEightLegalPoints()
        {
            List<int> neighbors = BoardCoords.GetNeighbors(BoardCoords.CenterPortCoordinate);
            Assert.Equal(8, neighbors.Count);
            Assert.All(neighbors, n => Assert.True(BoardCoords.IsValidPointCoordinate(n)));
        }
    }
}
