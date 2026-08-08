using System;
using PaiSho.Pieces;

namespace PaiSho.Domain
{
    /// <summary>
    /// Deterministic harmony / disharmony predicates.
    /// Lotus blooming is injected so PotManager stays out of Domain.
    /// </summary>
    public static class HarmonyRules
    {
        /// <summary>Chebyshev distance on the garden grid (max of |dx|, |dz|).</summary>
        public static int ChebyshevDistance(int coordA, int coordB)
        {
            GridPos a = BoardCoords.FromCoordinate(coordA);
            GridPos b = BoardCoords.FromCoordinate(coordB);
            int dx = Math.Abs(a.X - b.X);
            int dz = Math.Abs(a.Z - b.Z);
            return Math.Max(dx, dz);
        }

        /// <summary>
        /// Type-level harmonization (no ownership/distance).
        /// Same flower types harmonize; blooming Lotus harmonizes with any flower.
        /// </summary>
        public static bool CanHarmonizeTypes(
            PieceType typeA,
            PieceType typeB,
            bool lotusABlooming,
            bool lotusBBlooming)
        {
            if (typeA == PieceType.Lotus && lotusABlooming && PieceTraits.IsFlower(typeB))
                return true;
            if (typeB == PieceType.Lotus && lotusBBlooming && PieceTraits.IsFlower(typeA))
                return true;

            return typeA == typeB && PieceTraits.IsFlower(typeA) && PieceTraits.IsFlower(typeB);
        }

        /// <summary>
        /// Same seat, compatible types, Chebyshev distance exactly 1 (ortho or diagonal).
        /// </summary>
        public static bool IsHarmony(
            Seat seatA,
            Seat seatB,
            PieceType typeA,
            PieceType typeB,
            int coordA,
            int coordB,
            bool lotusABlooming,
            bool lotusBBlooming)
        {
            if (seatA != seatB)
                return false;

            if (!CanHarmonizeTypes(typeA, typeB, lotusABlooming, lotusBBlooming))
                return false;

            return ChebyshevDistance(coordA, coordB) == 1;
        }

        /// <summary>
        /// Opposite seats and types that cannot harmonize.
        /// Distance is the caller's responsibility (CaptureManager already filters adjacency).
        /// </summary>
        public static bool IsDisharmony(
            Seat seatA,
            Seat seatB,
            PieceType typeA,
            PieceType typeB,
            bool lotusABlooming,
            bool lotusBBlooming)
        {
            if (seatA == seatB)
                return false;

            return !CanHarmonizeTypes(typeA, typeB, lotusABlooming, lotusBBlooming);
        }
    }
}
