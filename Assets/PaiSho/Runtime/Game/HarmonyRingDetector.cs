using System.Collections.Generic;
using System.Linq;
using PaiSho.Board;
using PaiSho.Pieces;
using UnityEngine;

namespace PaiSho.Game
{
    /// <summary>
    /// Detects harmony rings: cycles of InHarmony pieces that enclose the middle gate.
    /// Progress uses enclosing cycles of length 3+ so "one from victory" is visible to the AI.
    /// </summary>
    public static class HarmonyRingDetector
    {
        public const int MinRingSize = 4;
        private const int MinTrackedCycleSize = 3;
        private const int MaxRingSearchSize = 10;

        private static readonly float CenterColumn = BoardUtils.GetColumn(BoardUtils.MiddleGate);
        private static readonly float CenterRow = BoardUtils.GetRow(BoardUtils.MiddleGate);

        public static bool HasCompleteRing(Player player) => GetRingProgress(player) >= MinRingSize;

        /// <summary>
        /// Enclosing-cycle length, capped at <see cref="MinRingSize"/> for victory.
        /// Returns 3 when one harmonious link from a winning ring.
        /// </summary>
        public static int GetRingProgress(Player player)
        {
            int length = GetEnclosingCycleLength(player);
            if (length <= 0)
                return 0;

            return length >= MinRingSize ? MinRingSize : length;
        }

        public static int GetEnclosingCycleLength(Player player)
        {
            List<Piece> cycle = FindLongestEnclosingCycle(player);
            return cycle?.Count ?? 0;
        }

        /// <summary>Longest harmony cycle of size 3+, whether or not it encloses the gate.</summary>
        public static int GetLongestCycleLength(Player player)
        {
            List<Piece> cycle = FindLongestCycle(player, requireEnclosing: false);
            return cycle?.Count ?? 0;
        }

        /// <summary>
        /// How much of the compass harmonic seats already cover (0-360).
        /// High span with no enclosing cycle means the gap must be filled.
        /// </summary>
        public static float GetHarmonicAngularSpanDegrees(Player player)
        {
            if (BoardManager.Instance == null)
                return 0f;

            var angles = new List<float>();
            foreach (Piece piece in BoardManager.Instance.GetAllPieces())
            {
                if (piece == null || piece.Owner != player || piece.IsGhost || !piece.InHarmony)
                    continue;

                float dx = BoardUtils.GetColumn(piece.BoardCoordinate) - CenterColumn;
                float dy = BoardUtils.GetRow(piece.BoardCoordinate) - CenterRow;
                float deg = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                angles.Add(((deg % 360f) + 360f) % 360f);
            }

            if (angles.Count < 2)
                return 0f;

            angles.Sort();
            float largestGap = 0f;
            for (int i = 0; i < angles.Count; i++)
            {
                float a = angles[i];
                float b = i + 1 < angles.Count ? angles[i + 1] : angles[0] + 360f;
                float gap = b - a;
                if (gap > largestGap)
                    largestGap = gap;
            }

            return Mathf.Clamp(360f - largestGap, 0f, 360f);
        }

        public static string GetRingProgressLabel(Player player)
        {
            int progress = GetRingProgress(player);
            if (progress >= MinRingSize)
                return $"{player}: Victory ring complete!";
            if (progress == MinRingSize - 1)
                return $"{player}: One harmonious piece from victory!";
            if (progress > 0)
                return $"{player}: Enclosing ring {progress}/{MinRingSize}";
            return null;
        }

        public static bool IsOnePieceFromVictory(Player player) =>
            GetRingProgress(player) == MinRingSize - 1;

        public static bool TryGetCompleteRing(Player player, out List<Piece> ringPieces)
        {
            ringPieces = FindLongestEnclosingCycle(player);
            return ringPieces != null && ringPieces.Count >= MinRingSize;
        }

        public static bool TryGetBestEnclosingCycle(Player player, out List<Piece> ringPieces)
        {
            ringPieces = FindLongestEnclosingCycle(player);
            return ringPieces != null && ringPieces.Count > 0;
        }

        public static bool IsPieceInBestEnclosingCycle(Player player, Piece piece)
        {
            if (piece == null || !TryGetBestEnclosingCycle(player, out List<Piece> cycle) || cycle == null)
                return false;

            return cycle.Contains(piece);
        }

        private static List<Piece> FindLongestEnclosingCycle(Player player) =>
            FindLongestCycle(player, requireEnclosing: true);

        private static List<Piece> FindLongestCycle(Player player, bool requireEnclosing)
        {
            if (BoardManager.Instance == null || HarmonyManager.Instance == null)
                return null;

            List<Piece> candidates = GetQualifyingPieces(player);
            if (candidates.Count < MinTrackedCycleSize)
                return null;

            Dictionary<Piece, List<Piece>> graph = BuildHarmonyGraph(candidates);
            var cycles = new List<List<Piece>>();
            var seen = new HashSet<string>();

            foreach (Piece start in candidates)
            {
                if (graph[start].Count == 0)
                    continue;

                FindCycles(
                    start,
                    start,
                    new List<Piece> { start },
                    new HashSet<(Piece, Piece)>(),
                    graph,
                    cycles,
                    seen);
            }

            List<Piece> best = null;
            foreach (List<Piece> cycle in cycles)
            {
                if (requireEnclosing && !EnclosesMiddleGate(cycle))
                    continue;

                if (best == null || cycle.Count > best.Count)
                    best = cycle;
            }

            return best;
        }

        private static List<Piece> GetQualifyingPieces(Player player)
        {
            var list = new List<Piece>();
            foreach (Piece piece in BoardManager.Instance.GetAllPieces())
            {
                if (piece.Owner == player && piece.InHarmony && !piece.IsGhost)
                    list.Add(piece);
            }

            return list;
        }

        private static Dictionary<Piece, List<Piece>> BuildHarmonyGraph(List<Piece> candidates)
        {
            var graph = new Dictionary<Piece, List<Piece>>();
            foreach (Piece piece in candidates)
                graph[piece] = new List<Piece>();

            for (int i = 0; i < candidates.Count; i++)
            {
                for (int j = i + 1; j < candidates.Count; j++)
                {
                    Piece a = candidates[i];
                    Piece b = candidates[j];
                    if (!HarmonyManager.Instance.IsHarmony(a, b))
                        continue;

                    graph[a].Add(b);
                    graph[b].Add(a);
                }
            }

            return graph;
        }

        private static void FindCycles(
            Piece start,
            Piece current,
            List<Piece> path,
            HashSet<(Piece, Piece)> usedEdges,
            Dictionary<Piece, List<Piece>> graph,
            List<List<Piece>> results,
            HashSet<string> seen)
        {
            foreach (Piece neighbor in graph[current])
            {
                var edge = UndirectedEdge(current, neighbor);
                if (usedEdges.Contains(edge))
                    continue;

                if (neighbor == start)
                {
                    if (path.Count >= MinTrackedCycleSize)
                        AddCycle(path, results, seen);
                    continue;
                }

                if (path.Contains(neighbor) || path.Count >= MaxRingSearchSize)
                    continue;

                usedEdges.Add(edge);
                path.Add(neighbor);
                FindCycles(start, neighbor, path, usedEdges, graph, results, seen);
                path.RemoveAt(path.Count - 1);
                usedEdges.Remove(edge);
            }
        }

        private static void AddCycle(List<Piece> path, List<List<Piece>> results, HashSet<string> seen)
        {
            string key = string.Join(",", path.Select(p => p.BoardCoordinate).OrderBy(c => c));
            if (!seen.Add(key))
                return;

            results.Add(new List<Piece>(path));
        }

        private static (Piece, Piece) UndirectedEdge(Piece a, Piece b) =>
            a.BoardCoordinate <= b.BoardCoordinate ? (a, b) : (b, a);

        private static bool EnclosesMiddleGate(List<Piece> cycle)
        {
            if (cycle == null || cycle.Count < 3)
                return false;

            var ordered = cycle
                .OrderBy(p => System.Math.Atan2(
                    BoardUtils.GetRow(p.BoardCoordinate) - CenterRow,
                    BoardUtils.GetColumn(p.BoardCoordinate) - CenterColumn))
                .ToList();

            return PointInPolygon(CenterColumn, CenterRow, ordered);
        }

        private static bool PointInPolygon(float x, float y, List<Piece> polygon)
        {
            bool inside = false;
            int count = polygon.Count;

            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                float xi = BoardUtils.GetColumn(polygon[i].BoardCoordinate);
                float yi = BoardUtils.GetRow(polygon[i].BoardCoordinate);
                float xj = BoardUtils.GetColumn(polygon[j].BoardCoordinate);
                float yj = BoardUtils.GetRow(polygon[j].BoardCoordinate);

                bool intersects = (yi > y) != (yj > y) &&
                                    x < (xj - xi) * (y - yi) / (yj - yi + float.Epsilon) + xi;
                if (intersects)
                    inside = !inside;
            }

            return inside;
        }
    }
}
