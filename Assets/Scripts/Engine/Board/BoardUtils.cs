using System.Collections.Generic;
using UnityEngine;

namespace PaiSho.Board
{
    public static class BoardUtils
    {
        /// <summary>Playable garden spans x,z in [-9, 9] (19 cells).</summary>
        public static readonly int BoardWidth = 19;
        public static readonly int BoardHeight = 19;

        /// <summary>
        /// Encoding stride used by <see cref="ToCoordinate"/>. Intentionally 20
        /// (not 19) so row/column math stays aligned with historical board data.
        /// </summary>
        public static readonly int CoordStride = 20;

        public static readonly int BoardSize = CoordStride * CoordStride;
        public static readonly int CenterPortCoordinate = ToCoordinate(0, 0);

        public static readonly HashSet<int> LegalPoints = GenerateLegalPoints();

        // Ortho + diagonal neighbors in stride-20 encoding.
        public static readonly int[] AllDirections = { -CoordStride, CoordStride, -1, 1, -(CoordStride + 1), -(CoordStride - 1), CoordStride + 1, CoordStride - 1 };

        private static HashSet<int> GenerateLegalPoints()
        {
            HashSet<int> points = new HashSet<int>();

            for (int x = -9; x <= 9; x++)
            {
                for (int z = -9; z <= 9; z++)
                {
                    int manhattanDistance = Mathf.Abs(x) + Mathf.Abs(z);
                    if (manhattanDistance <= 12)
                    {
                        points.Add(ToCoordinate(x, z));
                    }
                }
            }

            return points;
        }

        public static bool IsValidPointCoordinate(int coord)
        {
            return LegalPoints.Contains(coord);
        }

        public static int ToCoordinate(int x, int z)
        {
            return (z + 9) * CoordStride + (x + 9);
        }

        public static Vector2Int FromCoordinate(int coordinate)
        {
            int x = (coordinate % CoordStride) - 9;
            int z = (coordinate / CoordStride) - 9;
            return new Vector2Int(x, z);
        }

        public static Vector2Int ToGrid(int coord)
        {
            return FromCoordinate(coord);
        }

        public static List<int> GetNeighbors(int coord)
        {
            List<int> neighbors = new List<int>();

            foreach (int offset in AllDirections)
            {
                int neighbor = coord + offset;
                if (IsValidPointCoordinate(neighbor))
                    neighbors.Add(neighbor);
            }

            return neighbors;
        }
    }
}
