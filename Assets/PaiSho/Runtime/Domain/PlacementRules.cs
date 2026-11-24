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
        PieceNotInReserve,
        WrongSide,
        WrongGarden
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
    /// Spring garden membership uses <see cref="GardenRules"/> (live mid=180 / BoardUtils
    /// encoding) — same integers the match board paints. Stride-20 Domain points that are not
    /// in those sets classify as Neutral and remain dual-legal.
    /// </summary>
    public static class PlacementRules
    {
        public const int LiveBoardStride = 19;

        public static PieceType GetOpeningFlower(Seat seat) =>
            seat == Seat.Host ? PieceType.Jasmine : PieceType.Rose;

        /// <summary>
        /// Host plays from negative Z (encoded rows 0..8); Opponent from positive Z (10..18).
        /// Row 9 is the center line (z = 0) and is neither side.
        /// </summary>
        public static bool IsOnOpponentSide(int coordinate, Seat seat)
        {
            int row = SideRow(coordinate);
            if (seat == Seat.Host)
                return row > 9;
            return row < 9;
        }

        /// <summary>Own half only — center row is neither side (spring / Lotus).</summary>
        public static bool IsOnOwnSide(int coordinate, Seat seat)
        {
            int row = SideRow(coordinate);
            if (row == 9)
                return false;
            return !IsOnOpponentSide(coordinate, seat);
        }

        public static bool IsBasicFlower(PieceType type) =>
            type == PieceType.Jasmine
            || type == PieceType.Rose
            || type == PieceType.Lily
            || type == PieceType.Jade
            || type == PieceType.Chrysanthemum
            || type == PieceType.Rhododendron;

        public static bool IsWhiteFlower(PieceType type) =>
            type == PieceType.Jasmine || type == PieceType.Lily || type == PieceType.Jade;

        public static bool IsRedFlower(PieceType type) =>
            type == PieceType.Rose
            || type == PieceType.Chrysanthemum
            || type == PieceType.Rhododendron;

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

            if (intent.Phase == MatchPhase.Spring)
                return EvaluateSpring(intent, isOccupied);

            if (!BoardCoords.IsValidPointCoordinate(intent.Coordinate))
                return PlacementResult.Deny(PlacementDenyReason.InvalidCoordinate);

            if (isOccupied(intent.Coordinate))
                return PlacementResult.Deny(PlacementDenyReason.Occupied);

            if (!intent.HasReserveAvailable)
                return PlacementResult.Deny(PlacementDenyReason.PieceNotInReserve);

            return PlacementResult.Allow();
        }

        private static PlacementResult EvaluateSpring(PlacementIntent intent, Func<int, bool> isOccupied)
        {
            // Spring accepts live mid=180 garden cells (BoardUtils) or Domain stride-20 points.
            bool livePoint = IsLiveBoardCoordinate(intent.Coordinate);
            bool domainPoint = BoardCoords.IsValidPointCoordinate(intent.Coordinate);
            if (!livePoint && !domainPoint)
                return PlacementResult.Deny(PlacementDenyReason.InvalidCoordinate);

            if (isOccupied(intent.Coordinate))
                return PlacementResult.Deny(PlacementDenyReason.Occupied);

            // RULES.md: any basic flower drawn in spring — not only the seat's ceremonial opener.
            if (!IsBasicFlower(intent.Type))
                return PlacementResult.Deny(PlacementDenyReason.WrongOpeningFlower);

            if (!IsOnOwnSide(intent.Coordinate, intent.Seat))
                return PlacementResult.Deny(PlacementDenyReason.WrongSide);

            bool white = IsWhiteFlower(intent.Type);
            if (!GardenRules.AllowsFlowerColor(white, intent.Coordinate))
                return PlacementResult.Deny(PlacementDenyReason.WrongGarden);

            return PlacementResult.Allow();
        }

        /// <summary>
        /// Live painted board (mid=180) uses stride 19 for side-of-board. Domain BoardCoords
        /// points that are not on that diamond keep stride 20.
        /// </summary>
        private static int SideRow(int coordinate) =>
            IsLiveBoardCoordinate(coordinate)
                ? coordinate / LiveBoardStride
                : coordinate / BoardCoords.CoordStride;

        private static bool IsLiveBoardCoordinate(int coordinate)
        {
            if (GardenRules.IsPort(coordinate))
                return true;
            if (GardenRules.Classify(coordinate) != GardenZone.Neutral)
                return true;
            return IsLiveLegalDiamond(coordinate);
        }

        /// <summary>
        /// Live board diamond (BoardUtils LegalPoints ranges) — spring garden tests can speak
        /// mid=180 integers without a stride converter.
        /// </summary>
        private static bool IsLiveLegalDiamond(int coordinate)
        {
            if (coordinate < 0 || coordinate >= LiveBoardStride * LiveBoardStride)
                return false;
            return (24 < coordinate && coordinate < 32)
                || (41 < coordinate && coordinate < 53)
                || (59 < coordinate && coordinate < 73)
                || (77 < coordinate && coordinate < 93)
                || (96 < coordinate && coordinate < 112)
                || (114 < coordinate && coordinate < 132)
                || (133 < coordinate && coordinate < 151)
                || (152 < coordinate && coordinate < 170)
                || (171 < coordinate && coordinate < 189)
                || (190 < coordinate && coordinate < 208)
                || (209 < coordinate && coordinate < 227)
                || (228 < coordinate && coordinate < 246)
                || (248 < coordinate && coordinate < 264)
                || (267 < coordinate && coordinate < 283)
                || (287 < coordinate && coordinate < 301)
                || (307 < coordinate && coordinate < 319)
                || (328 < coordinate && coordinate < 336);
        }
    }
}
