using System.Collections.Generic;

namespace PaiSho.Domain
{
    /// <summary>
    /// Live-board garden membership (BoardUtils / mid=180 encoding), not BoardCoords stride-20.
    /// Mixed = in both light and dark sets. Adjacent non-mixed cells stay exclusive.
    /// </summary>
    public enum GardenZone
    {
        Neutral = 0,
        Light = 1,
        Dark = 2,
        Mixed = 3,
        Port = 4
    }

    /// <summary>
    /// Deterministic garden classification for Basic Pai Sho flower placement/landing.
    /// White flowers may not sit on Dark-only; Red flowers may not sit on Light-only.
    /// Mixed and Neutral accept both colors. Ports accept neither for spring garden plant.
    /// </summary>
    public static class GardenRules
    {
        public const int NorthGate = 332;
        public const int EastGate = 172;
        public const int SouthGate = 28;
        public const int WestGate = 188;
        public const int MiddleGate = 180;

        private static readonly HashSet<int> Ports = new()
        {
            SouthGate, EastGate, WestGate, NorthGate, MiddleGate
        };

        /// <summary>
        /// White Gardens (IPSA): SW + NE light-only interiors plus mid-axis mixed borders.
        /// </summary>
        private static readonly HashSet<int> LightGarden = new()
        {
            // SW light-only
            46, 64, 65, 82, 83, 84, 100, 101, 102, 103, 118, 119, 120, 121, 122,
            136, 137, 138, 139, 140, 141, 154, 155, 156, 157, 158, 159, 160,
            // NE light-only
            200, 201, 202, 203, 204, 205, 206, 219, 220, 221, 222, 223, 224,
            238, 239, 240, 241, 242, 257, 258, 259, 260, 276, 277, 278, 295, 296, 314,
            // Mixed borders (count as both light and dark)
            47, 66, 85, 104, 123, 142, 161, 173, 174, 175, 176, 177, 178, 179,
            181, 182, 183, 184, 185, 186, 187, 199, 218, 237, 256, 275, 294, 313
        };

        /// <summary>
        /// Red Gardens (IPSA): SE + NW dark-only interiors plus the same mid-axis mixed borders.
        /// </summary>
        private static readonly HashSet<int> DarkGarden = new()
        {
            // SE dark-only
            48, 67, 68, 86, 87, 88, 105, 106, 107, 108, 124, 125, 126, 127, 128,
            143, 144, 145, 146, 147, 148, 162, 163, 164, 165, 166, 167, 168,
            // NW dark-only
            192, 193, 194, 195, 196, 197, 198, 212, 213, 214, 215, 216, 217,
            232, 233, 234, 235, 236, 252, 253, 254, 255, 272, 273, 274, 292, 293, 312,
            // Mixed borders (count as both light and dark)
            47, 66, 85, 104, 123, 142, 161, 173, 174, 175, 176, 177, 178, 179,
            181, 182, 183, 184, 185, 186, 187, 199, 218, 237, 256, 275, 294, 313
        };

        public static bool IsPort(int coordinate) => Ports.Contains(coordinate);

        public static bool IsInLightGarden(int coordinate) => LightGarden.Contains(coordinate);

        public static bool IsInDarkGarden(int coordinate) => DarkGarden.Contains(coordinate);

        public static GardenZone Classify(int coordinate)
        {
            if (IsPort(coordinate))
                return GardenZone.Port;

            bool light = IsInLightGarden(coordinate);
            bool dark = IsInDarkGarden(coordinate);
            if (light && dark)
                return GardenZone.Mixed;
            if (light)
                return GardenZone.Light;
            if (dark)
                return GardenZone.Dark;
            return GardenZone.Neutral;
        }

        public static bool IsWhiteOnly(int coordinate) => Classify(coordinate) == GardenZone.Light;

        public static bool IsRedOnly(int coordinate) => Classify(coordinate) == GardenZone.Dark;

        public static bool IsMixed(int coordinate) => Classify(coordinate) == GardenZone.Mixed;

        /// <summary>
        /// Basic Pai Sho: white flowers forbidden on red-only; red flowers forbidden on white-only.
        /// Mixed and Neutral allow both. Ports allow neither for garden plant checks.
        /// </summary>
        public static bool AllowsFlowerColor(bool whiteFlower, int coordinate)
        {
            if (IsPort(coordinate))
                return false;

            GardenZone zone = Classify(coordinate);
            if (whiteFlower)
                return zone != GardenZone.Dark;
            return zone != GardenZone.Light;
        }

        public static void GetDefaultGardens(out int[] light, out int[] dark)
        {
            light = new int[LightGarden.Count];
            LightGarden.CopyTo(light);
            System.Array.Sort(light);

            dark = new int[DarkGarden.Count];
            DarkGarden.CopyTo(dark);
            System.Array.Sort(dark);
        }
    }
}
