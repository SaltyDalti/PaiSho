using System;
using System.Collections.Generic;

namespace PaiSho.Domain
{
    /// <summary>
    /// Pure board coordinate math. No Unity dependencies.
    /// Encoding stride is intentionally 20 (not 19) for historical alignment.
    /// </summary>
    public static class BoardCoords
    {
        public const int BoardWidth = 19;
        public const int BoardHeight = 19;
        public const int CoordStride = 20;
        public const int BoardSize = CoordStride * CoordStride;
        public const int GardenMin = -9;
        public const int GardenMax = 9;
        public const int MaxManhattanFromCenter = 12;

        public static readonly int CenterPortCoordinate = ToCoordinate(0, 0);

        public static readonly int[] OrthoDirections =
        {
            -CoordStride, CoordStride, -1, 1
        };

        public static readonly int[] AllDirections =
        {
            -CoordStride, CoordStride, -1, 1,
            -(CoordStride + 1), -(CoordStride - 1),
            CoordStride + 1, CoordStride - 1
        };

        public static readonly IReadOnlyCollection<int> LegalPoints = GenerateLegalPoints();

        private static HashSet<int> GenerateLegalPoints()
        {
            var points = new HashSet<int>();
            for (int x = GardenMin; x <= GardenMax; x++)
            {
                for (int z = GardenMin; z <= GardenMax; z++)
                {
                    if (Math.Abs(x) + Math.Abs(z) <= MaxManhattanFromCenter)
                        points.Add(ToCoordinate(x, z));
                }
            }
            return points;
        }

        public static bool IsValidPointCoordinate(int coord) =>
            ((HashSet<int>)LegalPoints).Contains(coord);

        public static int ToCoordinate(int x, int z) =>
            (z + 9) * CoordStride + (x + 9);

        public static GridPos FromCoordinate(int coordinate)
        {
            int x = (coordinate % CoordStride) - 9;
            int z = (coordinate / CoordStride) - 9;
            return new GridPos(x, z);
        }

        public static List<int> GetNeighbors(int coord)
        {
            var neighbors = new List<int>(8);
            foreach (int offset in AllDirections)
            {
                int neighbor = coord + offset;
                if (IsValidPointCoordinate(neighbor))
                    neighbors.Add(neighbor);
            }
            return neighbors;
        }

        /// <summary>
        /// Orthogonal neighbors only (N/S/E/W). Used by capture adjacency and harmonic-ring paths.
        /// </summary>
        public static List<int> GetOrthogonalNeighbors(int coord)
        {
            var neighbors = new List<int>(4);
            foreach (int offset in OrthoDirections)
            {
                int neighbor = coord + offset;
                if (IsValidPointCoordinate(neighbor))
                    neighbors.Add(neighbor);
            }
            return neighbors;
        }
    }
}
