using System.Collections.Generic;
using PaiSho.Domain;
using PaiSho.Game;

namespace PaiSho.Board
{
    public static class BoardUtils
    {
        public const int GridSize = 19;
        public const int GridIntervals = GridSize - 1;
        public const int NumPoints = GridSize * GridSize;
        public const int ReserveSize = 54;

        /// <summary>Stride-20 board encoding shared with Domain tests.</summary>
        public static int ToCoordinate(int x, int z) => BoardCoords.ToCoordinate(x, z);

        public static GridPos FromCoordinate(int coordinate) => BoardCoords.FromCoordinate(coordinate);


        public const int NorthGate = 332;
        public const int EastGate = 172;
        public const int SouthGate = 28;
        public const int WestGate = 188;
        public const int MiddleGate = 180;

        private static readonly HashSet<int> LegalPoints = new();
        /// <summary>
        /// White Gardens (IPSA), light-only interiors: SW + NE diagonals inside the
        /// gate-to-gate diamond (|dr|+|dc| &lt;= 8). Mid-axis borders are also light via MixedGarden.
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

        private static readonly HashSet<int> Gates = new()
        {
            SouthGate, EastGate, MiddleGate, WestGate, NorthGate
        };

        /// <summary>All five entry/movement-blocked ports: four edge gates plus mid.</summary>
        private static readonly HashSet<int> Ports = new()
        {
            SouthGate, EastGate, WestGate, NorthGate, MiddleGate
        };

        /// <summary>
        /// Red Gardens (IPSA), dark-only interiors: SE + NW diagonals, plus the same
        /// mid-axis mixed borders (shared with LightGarden).
        /// Outer diagonal neutral borders are listed in NeutralGardenBorder.
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

        /// <summary>
        /// Single-cell diagonal borders between colored gardens and open neutral ground.
        /// Both flower colors allowed here; adjacent cells remain light/dark interiors.
        /// </summary>
        private static readonly HashSet<int> NeutralGardenBorder = new()
        {
            // Light / white wedge — SW then NE (step +18 along each diagonal)
            46, 64, 82, 100, 118, 136, 154,
            206, 224, 242, 260, 278, 296, 314,
            // Dark / red wedge — SE then NW (step +20 along each diagonal)
            48, 68, 88, 108, 128, 148, 168,
            192, 212, 232, 252, 272, 292, 312
        };

        /// <summary>
        /// Eight neighbors in row/column space (3×3 square). Matches visual grid alignment.
        /// Do not use ±21 — those skew diagonally and draw knotweed zones as a parallelogram.
        /// </summary>
        private static readonly int[] AdjacencyOffsets = { -20, -19, -18, -1, 1, 18, 19, 20 };
        public static readonly int[] CardinalDirections = { -GridSize, GridSize, -1, 1 };

        static BoardUtils()
        {
            for (int i = 0; i < NumPoints; i++)
            {
                if ((24 < i && i < 32) ||
                    (41 < i && i < 53) ||
                    (59 < i && i < 73) ||
                    (77 < i && i < 93) ||
                    (96 < i && i < 112) ||
                    (114 < i && i < 132) ||
                    (133 < i && i < 151) ||
                    (152 < i && i < 170) ||
                    (171 < i && i < 189) ||
                    (190 < i && i < 208) ||
                    (209 < i && i < 227) ||
                    (228 < i && i < 246) ||
                    (248 < i && i < 264) ||
                    (267 < i && i < 283) ||
                    (287 < i && i < 301) ||
                    (307 < i && i < 319) ||
                    (328 < i && i < 336))
                {
                    LegalPoints.Add(i);
                }
            }
        }

        public static int GetHomePort(Player player) =>
            player == Player.Host ? SouthGate : NorthGate;

        public static int GetForeignPort(Player player) =>
            player == Player.Host ? NorthGate : SouthGate;

        public static int GetEastPort() => EastGate;

        public static int GetWestPort() => WestGate;

        public static int GetMidPort() => MiddleGate;

        public static bool IsEastOrWestPort(int coordinate) =>
            coordinate == EastGate || coordinate == WestGate;

        public static IEnumerable<int> GetAllPorts()
        {
            foreach (int port in Ports)
                yield return port;
        }

        public static string GetPortName(int coordinate)
        {
            if (coordinate == SouthGate) return "South (Home for Host)";
            if (coordinate == NorthGate) return "North (Home for Opponent)";
            if (coordinate == EastGate) return "East";
            if (coordinate == WestGate) return "West";
            if (coordinate == MiddleGate) return "Mid";
            return "Port";
        }

        public static string GetPortNameForPlayer(int coordinate, Player player)
        {
            if (coordinate == GetHomePort(player)) return "Home";
            if (coordinate == GetForeignPort(player)) return "Foreign";
            if (coordinate == EastGate) return "East";
            if (coordinate == WestGate) return "West";
            if (coordinate == MiddleGate) return "Mid";
            return "Port";
        }

        public static bool IsValidPointCoordinate(int coordinate)
        {
            return LegalPoints.Contains(coordinate);
        }

        public static bool IsLegalPosition(int coordinate)
        {
            return IsValidPointCoordinate(coordinate);
        }

        public static int GetRow(int coordinate) => coordinate / GridSize;
        public static int GetColumn(int coordinate) => coordinate % GridSize;

        private static HashSet<int> runtimeLightGarden;
        private static HashSet<int> runtimeDarkGarden;
        private static bool useRuntimeGardens;

        public static bool IsInLightGarden(int coordinate) =>
            ActiveLightGarden.Contains(coordinate);

        public static bool IsInDarkGarden(int coordinate) =>
            ActiveDarkGarden.Contains(coordinate);

        public static bool IsGate(int coordinate) => Gates.Contains(coordinate);
        public static bool IsPort(int coordinate) => Ports.Contains(coordinate);

        private static HashSet<int> ActiveLightGarden =>
            useRuntimeGardens && runtimeLightGarden != null ? runtimeLightGarden : LightGarden;

        private static HashSet<int> ActiveDarkGarden =>
            useRuntimeGardens && runtimeDarkGarden != null ? runtimeDarkGarden : DarkGarden;

        public static GardenType GetGardenType(int coordinate)
        {
            if (IsPort(coordinate))
                return GardenType.Port;

            bool light = IsInLightGarden(coordinate);
            bool dark = IsInDarkGarden(coordinate);
            if (light && dark)
                return GardenType.MixedGarden;

            if (NeutralGardenBorder.Contains(coordinate))
                return GardenType.NeutralGarden;

            if (light)
                return GardenType.LightGarden;
            if (dark)
                return GardenType.DarkGarden;
            return GardenType.NeutralGarden;
        }

        /// <summary>White-garden only (not mixed, not neutral border).</summary>
        public static bool IsWhiteOnlyGarden(int coordinate) =>
            GetGardenType(coordinate) == GardenType.LightGarden;

        /// <summary>Red-garden only (not mixed, not neutral border).</summary>
        public static bool IsRedOnlyGarden(int coordinate) =>
            GetGardenType(coordinate) == GardenType.DarkGarden;

        public static bool IsMixedGarden(int coordinate) =>
            GetGardenType(coordinate) == GardenType.MixedGarden;

        /// <summary>Apply tuner-painted gardens for this play session (and export).</summary>
        public static void SetRuntimeGardens(System.Collections.Generic.IEnumerable<int> light, System.Collections.Generic.IEnumerable<int> dark)
        {
            runtimeLightGarden = light != null ? new HashSet<int>(light) : new HashSet<int>();
            runtimeDarkGarden = dark != null ? new HashSet<int>(dark) : new HashSet<int>();
            // Ports are never gardens.
            runtimeLightGarden.ExceptWith(Ports);
            runtimeDarkGarden.ExceptWith(Ports);
            runtimeLightGarden.IntersectWith(LegalPoints);
            runtimeDarkGarden.IntersectWith(LegalPoints);
            // Overlap in both sets = MixedGarden (legal for white and red flowers).
            useRuntimeGardens = true;
        }

        public static void ResetRuntimeGardens()
        {
            useRuntimeGardens = false;
            runtimeLightGarden = null;
            runtimeDarkGarden = null;
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

        public static void GetActiveGardens(out int[] light, out int[] dark)
        {
            var lightSet = ActiveLightGarden;
            var darkSet = ActiveDarkGarden;
            light = new int[lightSet.Count];
            lightSet.CopyTo(light);
            System.Array.Sort(light);
            dark = new int[darkSet.Count];
            darkSet.CopyTo(dark);
            System.Array.Sort(dark);
        }

        public static void SetGardenType(int coordinate, GardenType garden)
        {
            if (!IsValidPointCoordinate(coordinate) || IsPort(coordinate))
                return;

            if (!useRuntimeGardens)
            {
                runtimeLightGarden = new HashSet<int>(LightGarden);
                runtimeDarkGarden = new HashSet<int>(DarkGarden);
                useRuntimeGardens = true;
            }

            runtimeLightGarden.Remove(coordinate);
            runtimeDarkGarden.Remove(coordinate);

            switch (garden)
            {
                case GardenType.LightGarden:
                    runtimeLightGarden.Add(coordinate);
                    break;
                case GardenType.DarkGarden:
                    runtimeDarkGarden.Add(coordinate);
                    break;
                case GardenType.MixedGarden:
                    runtimeLightGarden.Add(coordinate);
                    runtimeDarkGarden.Add(coordinate);
                    break;
            }
        }

        public static GardenType CycleGardenType(int coordinate)
        {
            if (IsPort(coordinate))
                return GardenType.Port;

            GardenType current = GetGardenType(coordinate);
            GardenType next = current switch
            {
                GardenType.NeutralGarden => GardenType.LightGarden,
                GardenType.LightGarden => GardenType.DarkGarden,
                GardenType.DarkGarden => GardenType.MixedGarden,
                _ => GardenType.NeutralGarden
            };
            SetGardenType(coordinate, next);
            return next;
        }

        public static bool IsHostSide(int coordinate)
        {
            return GetRow(coordinate) < 9;
        }

        public static bool IsOpponentSide(int coordinate)
        {
            return GetRow(coordinate) > 9;
        }

        public static IEnumerable<int> GetAdjacentCoordinates(int coordinate)
        {
            foreach (int offset in AdjacencyOffsets)
            {
                int neighbor = coordinate + offset;
                if (IsValidPointCoordinate(neighbor))
                    yield return neighbor;
            }
        }

        public static IEnumerable<int> GetCardinalCoordinates(int coordinate)
        {
            foreach (int offset in CardinalDirections)
            {
                int neighbor = coordinate + offset;
                if (IsValidPointCoordinate(neighbor))
                    yield return neighbor;
            }
        }

        public static List<int> GetClockwiseCardinalRing(int center) =>
            GetClockwiseSquareRing(center);

        /// <summary>Eight neighbors in clockwise order starting from north (3×3 square around center).</summary>
        public static List<int> GetClockwiseSquareRing(int center)
        {
            int[] clockwise =
            {
                -GridSize, -GridSize + 1, 1, GridSize + 1,
                GridSize, GridSize - 1, -1, -GridSize - 1
            };

            var ring = new List<int>(8);
            foreach (int offset in clockwise)
            {
                int neighbor = center + offset;
                if (IsValidPointCoordinate(neighbor))
                    ring.Add(neighbor);
            }

            return ring;
        }

        public static IEnumerable<int> GetReachableCoordinates(int from, int range)
        {
            var visited = new HashSet<int> { from };
            var frontier = new Queue<(int coord, int depth)>();
            frontier.Enqueue((from, 0));

            while (frontier.Count > 0)
            {
                var (current, depth) = frontier.Dequeue();
                if (depth >= range)
                    continue;

                foreach (int neighbor in GetAdjacentCoordinates(current))
                {
                    if (!visited.Add(neighbor))
                        continue;

                    yield return neighbor;
                    frontier.Enqueue((neighbor, depth + 1));
                }
            }
        }
    }
}
