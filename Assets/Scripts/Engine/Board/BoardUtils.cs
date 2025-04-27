using System.Collections.Generic;

namespace PaiSho.Board
{
    public static class BoardUtils
    {
        // --- Legal playable coordinates ---
        public static readonly int BoardWidth = 19; // 19x19 standard
        public static readonly int BoardHeight = 19;
        public static readonly int BoardSize = BoardWidth * BoardHeight;
        public static readonly int CenterPortCoordinate = 209; // Change '209' to your board's actual center coordinate


        public static readonly int[] AllDirections = { -19, 19, -1, 1, -20, -18, 20, 18 }; // Up, down, left, right, diagonals

        /// <summary>
        /// Check if a coordinate is valid on the board.
        /// </summary>
        public static bool IsValidPointCoordinate(int coord)
        {
            return coord >= 0 && coord < BoardSize;
        }

        /// <summary>
        /// Get a list of all neighboring coordinates that are valid.
        /// </summary>
        public static List<int> GetNeighbors(int coord)
        {
            List<int> neighbors = new List<int>();

            int row = coord / BoardWidth;
            int col = coord % BoardWidth;

            // Directions: N, S, W, E, NW, NE, SW, SE
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
