using System;
using PaiSho.Pieces;

namespace PaiSho.Domain
{
    public enum PlacementDenyReason
    {
        None = 0,
        InvalidCoordinate,
        Occupied,
        WrongPhase,
        WrongOpeningFlower,
        PieceNotInReserve
    }

    public readonly struct PlacementIntent
    {
        public MatchPhase Phase { get; }
        public Seat Seat { get; }
        public PieceType Type { get; }
        public int Coordinate { get; }
        public bool HasReserveAvailable { get; }

        public PlacementIntent(
            MatchPhase phase,
            Seat seat,
            PieceType type,
            int coordinate,
            bool hasReserveAvailable)
        {
            Phase = phase;
            Seat = seat;
            Type = type;
            Coordinate = coordinate;
            HasReserveAvailable = hasReserveAvailable;
        }
    }

    public readonly struct PlacementResult
    {
        public bool IsAllowed { get; }
        public PlacementDenyReason Reason { get; }

        private PlacementResult(bool allowed, PlacementDenyReason reason)
        {
            IsAllowed = allowed;
            Reason = reason;
        }

        public static PlacementResult Allow() => new PlacementResult(true, PlacementDenyReason.None);

        public static PlacementResult Deny(PlacementDenyReason reason) =>
            new PlacementResult(false, reason);
    }

    /// <summary>
    /// Deterministic placement legality. Occupancy is injected for Unity/tests.
    /// </summary>
    public static class PlacementRules
    {
        public static PieceType GetOpeningFlower(Seat seat) =>
            seat == Seat.Host ? PieceType.Jasmine : PieceType.Rose;

        /// <summary>
        /// Host plays from negative Z (encoded rows 0..8); Opponent from positive Z (10..18).
        /// Row 9 is the center line (z = 0) and is neither side.
        /// </summary>
        public static bool IsOnOpponentSide(int coordinate, Seat seat)
        {
            int row = coordinate / BoardCoords.CoordStride;
            if (seat == Seat.Host)
                return row > 9;
            return row < 9;
        }

        public static bool IsEmptyLegalPoint(int coordinate, Func<int, bool> isOccupied)
        {
            if (isOccupied == null)
                throw new ArgumentNullException(nameof(isOccupied));

            return BoardCoords.IsValidPointCoordinate(coordinate) && !isOccupied(coordinate);
        }

        public static PlacementResult Evaluate(PlacementIntent intent, Func<int, bool> isOccupied)
        {
            if (isOccupied == null)
                throw new ArgumentNullException(nameof(isOccupied));

            if (intent.Phase == MatchPhase.End)
                return PlacementResult.Deny(PlacementDenyReason.WrongPhase);

            if (!BoardCoords.IsValidPointCoordinate(intent.Coordinate))
                return PlacementResult.Deny(PlacementDenyReason.InvalidCoordinate);

            if (isOccupied(intent.Coordinate))
                return PlacementResult.Deny(PlacementDenyReason.Occupied);

            if (intent.Phase == MatchPhase.Spring)
            {
                if (intent.Type != GetOpeningFlower(intent.Seat))
                    return PlacementResult.Deny(PlacementDenyReason.WrongOpeningFlower);
                return PlacementResult.Allow();
            }

            // Normal play: piece must be available in reserve.
            if (!intent.HasReserveAvailable)
                return PlacementResult.Deny(PlacementDenyReason.PieceNotInReserve);

            return PlacementResult.Allow();
        }
    }
}
