using System.Collections.Generic;
using UnityEngine;


namespace PaiSho.Board
{
    public static class BoardUtils
    {
        // --- Legal playable coordinates ---
        public static readonly int BoardWidth = 19; // 19x19 standard
        public static readonly int BoardHeight = 19;
        public static readonly int BoardSize = BoardWidth * BoardHeight;
        public static readonly int CenterPortCoordinate = 209; // Change '209' to your board's actual center coordinate

        public static readonly HashSet<int> LegalPoints = new HashSet<int>()
        {
            25, 26, 27, 28, 29, 30, 31,
            42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52,
            60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72,
            78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92,
            97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111,
            115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131,
            134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150,
            153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 167, 168, 169,
            172, 173, 174, 175, 176, 177, 178, 179, 180, 181, 182, 183, 184, 185, 186, 187, 188,
            191, 192, 193, 194, 195, 196, 197, 198, 199, 200, 201, 202, 203, 204, 205, 206, 207,
            210, 211, 212, 213, 214, 215, 216, 217, 218, 219, 220, 221, 222, 223, 224, 225, 226,
            229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245,
            249, 250, 251, 252, 253, 254, 255, 256, 257, 258, 259, 260, 261, 262, 263,
            268, 269, 270, 271, 272, 273, 274, 275, 276, 277, 278, 279, 280, 281, 282,
            288, 289, 290, 291, 292, 293, 294, 295, 296, 297, 298, 299, 300,
            308, 309, 310, 311, 312, 313, 314, 315, 316, 317, 318,
            329, 330, 331, 332, 333, 334, 335
        };


        public static readonly int[] AllDirections = { -19, 19, -1, 1, -20, -18, 20, 18 }; // Up, down, left, right, diagonals

        /// <summary>
        /// Check if a coordinate is valid on the board.
        /// </summary>
        public static bool IsValidPointCoordinate(int coord)
        {
            return coord >= 0 && coord < BoardSize;
        }

        /// <summary>
        /// Converts grid (x, z) position into a single integer coordinate (0-399)
        /// </summary>
        public static int ToCoordinate(int x, int z)
        {
            return (z + 9) * 20 + (x + 9);
        }

        /// <summary>
        /// Converts a single coordinate (0-399) back into (x, z) grid position
        /// </summary>
        public static Vector2Int FromCoordinate(int coordinate)
        {
            int x = (coordinate % 20) - 9;
            int z = (coordinate / 20) - 9;
            return new Vector2Int(x, z);
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
