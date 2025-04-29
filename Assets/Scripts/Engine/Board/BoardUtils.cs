using System.Collections.Generic;
using UnityEngine;

namespace PaiSho.Board
{
    public static class BoardUtils
    {
        public static readonly int BoardWidth = 19;
        public static readonly int BoardHeight = 19;
        public static readonly int BoardSize = BoardWidth * BoardHeight;
        public static readonly int CenterPortCoordinate = 209; // Update as needed

        public static readonly HashSet<int> LegalPoints = GenerateLegalPoints();

        private static HashSet<int> GenerateLegalPoints()
        {
            HashSet<int> points = new HashSet<int>();
            int radius = 9;

            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    if (Mathf.Abs(x) + Mathf.Abs(z) <= radius)
                    {
                        int coord = ToCoordinate(x, z);
                        points.Add(coord);
                    }
                }
            }

            return points;
        }

        public static readonly int[] AllDirections = { -19, 19, -1, 1, -20, -18, 20, 18 };

        public static bool IsValidPointCoordinate(int coord)
        {
            return coord >= 0 && coord < BoardSize;
        }

        public static int ToCoordinate(int x, int z)
        {
            return (z + 9) * 20 + (x + 9);
        }

        public static Vector2Int FromCoordinate(int coordinate)
        {
            int x = (coordinate % 20) - 9;
            int z = (coordinate / 20) - 9;
            return new Vector2Int(x, z);
        }

        public static Vector2Int ToGrid(int coord)
        {
            int x = (coord % 20) - 9;
            int z = (coord / 20) - 9;
            return new Vector2Int(x, z);
        }

        public static List<int> GetNeighbors(int coord)
        {
            List<int> neighbors = new List<int>();

            int row = coord / BoardWidth;
            int col = coord % BoardWidth;

            int[] rowOffsets = { -1, 1, 0, 0, -1, -1, 1, 1 };
            int[] colOffsets = { 0, 0, -1, 1, -1, 1, -1, 1 };

            for (int i = 0; i < rowOffsets.Length; i++)
            {
                int newRow = row + rowOffsets[i];
                int newCol = col + colOffsets[i];

                if (newRow >= 0 && newRow < BoardHeight && newCol >= 0 && newCol < BoardWidth)
                {
                    int neighborCoord = newRow * BoardWidth + newCol;
                    neighbors.Add(neighborCoord);
                }
            }

            return neighbors;
        }
    }
}
