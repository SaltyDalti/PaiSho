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
        /// White Gardens (IPSA). Defaults live in <see cref="GardenRules"/> (Domain SoT);
        /// this set is seeded from those defaults and can be replaced by tuner runtime paint.
        /// </summary>
        private static readonly HashSet<int> LightGarden = CreateDefaultLightGarden();

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
        /// Red Gardens (IPSA). Defaults from <see cref="GardenRules"/>; runtime paint may replace.
        /// </summary>
        private static readonly HashSet<int> DarkGarden = CreateDefaultDarkGarden();

        private static HashSet<int> CreateDefaultLightGarden()
        {
            GardenRules.GetDefaultGardens(out int[] light, out _);
            return new HashSet<int>(light);
        }

        private static HashSet<int> CreateDefaultDarkGarden()
        {
            GardenRules.GetDefaultGardens(out _, out int[] dark);
            return new HashSet<int>(dark);
        }

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

            // Runtime paint can diverge from Domain defaults; classify from active sets only.
            // Do not promote light-only / dark-only outer-diagonal cells to Neutral (dual-legal):
            // only true Mixed (in both sets) accepts both flower colors.
            bool light = IsInLightGarden(coordinate);
            bool dark = IsInDarkGarden(coordinate);
            if (light && dark)
                return GardenType.MixedGarden;
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
