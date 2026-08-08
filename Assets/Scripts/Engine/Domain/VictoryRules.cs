using System.Collections.Generic;

namespace PaiSho.Domain
{
    /// <summary>
    /// Harmonic-ring victory: a continuous orthogonally-linked harmony cycle
    /// that includes every orthogonal neighbor of the center port.
    /// </summary>
    public static class VictoryRules
    {
        public const int MinRingSize = 4;

        public static bool HasHarmonicRing(Seat seat, IReadOnlyList<BoardPiece> pieces)
        {
            if (pieces == null || pieces.Count == 0)
                return false;

            var byCoord = new Dictionary<int, BoardPiece>();
            var seatCoords = new HashSet<int>();

            foreach (BoardPiece piece in pieces)
            {
                if (piece.Seat != seat || piece.IsGhost)
                    continue;

                byCoord[piece.Coordinate] = piece;
                seatCoords.Add(piece.Coordinate);
            }

            if (seatCoords.Count < MinRingSize)
                return false;

            foreach (int startCoord in seatCoords)
            {
                var visited = new HashSet<int>();
                if (IsHarmonicLoop(startCoord, startCoord, visited, previousCoord: -1, seatCoords, byCoord))
                    return true;
            }

            return false;
        }

        public static bool IsCenterPortEncircled(ISet<int> loopCoords)
        {
            if (loopCoords == null)
                return false;

            foreach (int coord in BoardCoords.GetOrthogonalNeighbors(BoardCoords.CenterPortCoordinate))
            {
                if (!loopCoords.Contains(coord))
                    return false;
            }

            return true;
        }

        private static bool IsHarmonicLoop(
            int currentCoord,
            int startCoord,
            HashSet<int> visited,
            int previousCoord,
            HashSet<int> seatCoords,
            Dictionary<int, BoardPiece> byCoord)
        {
            visited.Add(currentCoord);

            foreach (int neighbor in BoardCoords.GetOrthogonalNeighbors(currentCoord))
            {
                if (neighbor == previousCoord)
                    continue;
                if (!seatCoords.Contains(neighbor))
                    continue;

                if (!AreInHarmony(byCoord[currentCoord], byCoord[neighbor]))
                    continue;

                if (neighbor == startCoord
                    && visited.Count >= MinRingSize
                    && IsCenterPortEncircled(visited))
                {
                    return true;
                }

                if (!visited.Contains(neighbor)
                    && IsHarmonicLoop(neighbor, startCoord, visited, currentCoord, seatCoords, byCoord))
                {
                    return true;
                }
            }

            visited.Remove(currentCoord);
            return false;
        }

        private static bool AreInHarmony(BoardPiece a, BoardPiece b)
        {
            return HarmonyRules.IsHarmony(
                a.Seat,
                b.Seat,
                a.Type,
                b.Type,
                a.Coordinate,
                b.Coordinate,
                a.LotusBlooming,
                b.LotusBlooming);
        }
    }
}
