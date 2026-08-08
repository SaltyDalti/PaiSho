using System.Collections.Generic;
using PaiSho.Pieces;

namespace PaiSho.Domain
{
    public enum CaptureDenyReason
    {
        None = 0,
        SameSeat,
        NotAdjacent,
        NotDisharmony,
        SeasonImmune,
        MissingPiece
    }

    public readonly struct CaptureOpportunity
    {
        public BoardPiece Attacker { get; }
        public BoardPiece Target { get; }

        public CaptureOpportunity(BoardPiece attacker, BoardPiece target)
        {
            Attacker = attacker;
            Target = target;
        }
    }

    /// <summary>
    /// Deterministic capture targeting: orthogonal disharmony adjacency + season immunity.
    /// Side effects (pot, destroy) stay in Unity managers.
    /// </summary>
    public static class CaptureRules
    {
        /// <summary>Boat and Knotweed are immune during Summer.</summary>
        public static bool CanBeCaptured(PieceType type, GardenSeason season)
        {
            if (season == GardenSeason.Summer
                && (type == PieceType.Boat || type == PieceType.Knotweed))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Autumn host-side flowers resist disharmony effects (see Piece.CanBeDisharmonized).
        /// Not currently applied by CaptureManager; exposed for shared rules/tests.
        /// </summary>
        public static bool CanBeDisharmonized(PieceType type, GardenSeason season)
        {
            if (season == GardenSeason.Autumn
                && (type == PieceType.Rose
                    || type == PieceType.Chrysanthemum
                    || type == PieceType.Rhododendron))
            {
                return false;
            }

            return true;
        }

        public static bool AreOrthogonallyAdjacent(int coordA, int coordB)
        {
            foreach (int neighbor in BoardCoords.GetOrthogonalNeighbors(coordA))
            {
                if (neighbor == coordB)
                    return true;
            }

            return false;
        }

        public static CaptureDenyReason EvaluatePair(
            BoardPiece attacker,
            BoardPiece target,
            GardenSeason season)
        {
            if (attacker.Seat == target.Seat)
                return CaptureDenyReason.SameSeat;

            if (!AreOrthogonallyAdjacent(attacker.Coordinate, target.Coordinate))
                return CaptureDenyReason.NotAdjacent;

            if (!HarmonyRules.IsDisharmony(
                    attacker.Seat,
                    target.Seat,
                    attacker.Type,
                    target.Type,
                    attacker.LotusBlooming,
                    target.LotusBlooming))
            {
                return CaptureDenyReason.NotDisharmony;
            }

            if (!CanBeCaptured(target.Type, season))
                return CaptureDenyReason.SeasonImmune;

            return CaptureDenyReason.None;
        }

        public static List<CaptureOpportunity> FindCaptureTargets(
            BoardPiece attacker,
            IReadOnlyList<BoardPiece> board,
            GardenSeason season)
        {
            var results = new List<CaptureOpportunity>();
            if (board == null || board.Count == 0)
                return results;

            var byCoord = new Dictionary<int, BoardPiece>();
            foreach (BoardPiece piece in board)
            {
                if (piece.IsGhost)
                    continue;
                byCoord[piece.Coordinate] = piece;
            }

            foreach (int neighborCoord in BoardCoords.GetOrthogonalNeighbors(attacker.Coordinate))
            {
                if (!byCoord.TryGetValue(neighborCoord, out BoardPiece target))
                    continue;

                if (EvaluatePair(attacker, target, season) == CaptureDenyReason.None)
                    results.Add(new CaptureOpportunity(attacker, target));
            }

            return results;
        }
    }
}
