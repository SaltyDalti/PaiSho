using System.Collections.Generic;
using UnityEngine;
using PaiSho.Domain;

namespace PaiSho.Board
{
    /// <summary>
    /// Unity-facing facade over <see cref="BoardCoords"/>. Prefer Domain for new logic/tests.
    /// </summary>
    public static class BoardUtils
    {
        public static readonly int BoardWidth = BoardCoords.BoardWidth;
        public static readonly int BoardHeight = BoardCoords.BoardHeight;
        public static readonly int CoordStride = BoardCoords.CoordStride;
        public static readonly int BoardSize = BoardCoords.BoardSize;
        public static readonly int CenterPortCoordinate = BoardCoords.CenterPortCoordinate;

        public static readonly HashSet<int> LegalPoints = new HashSet<int>(BoardCoords.LegalPoints);

        public static readonly int[] AllDirections = BoardCoords.AllDirections;

        public static bool IsValidPointCoordinate(int coord) =>
            BoardCoords.IsValidPointCoordinate(coord);

        public static int ToCoordinate(int x, int z) =>
            BoardCoords.ToCoordinate(x, z);

        public static Vector2Int FromCoordinate(int coordinate)
        {
            GridPos g = BoardCoords.FromCoordinate(coordinate);
            return new Vector2Int(g.X, g.Z);
        }

        public static Vector2Int ToGrid(int coord) => FromCoordinate(coord);

        public static List<int> GetNeighbors(int coord) =>
            BoardCoords.GetNeighbors(coord);
    }
}
