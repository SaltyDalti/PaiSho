using System.Collections.Generic;

namespace PaiSho.Board
{
    public static class BoardUtils
    {
        // Legal board points (usable tile coordinates)
        public static readonly HashSet<int> LegalPoints = new ()
        {
            // Center out rings for a 19x19 board - just for example
            180, 181, 182, 183, 184,
            199, 200, 201, 202, 203,
            218, 219, 220, 221, 222,
            237, 238, 239, 240, 241,
            256, 257, 258, 259, 260,
            // TODO: Expand depending on your actual board!
        };

        public static readonly int[] AllDirections = { -19, 19, -1, 1, -20, -18, 20, 18 };

        public static bool IsValidPointCoordinate(int coord)
        {
            return LegalPoints.Contains(coord);
        }
    }
}
