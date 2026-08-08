using PaiSho.Pieces;

namespace PaiSho.Domain
{
    /// <summary>
    /// Rich piece snapshot for season bonuses and scoring (extends occupancy with wilt/harmony state).
    /// </summary>
    public readonly struct PieceStatus
    {
        public Seat Seat { get; }
        public PieceType Type { get; }
        public int Coordinate { get; }
        public bool IsGhost { get; }
        public bool LotusBlooming { get; }
        public bool InHarmony { get; }
        public int WiltLevel { get; }
        public int PreviousWiltLevel { get; }
        public int TurnsSinceMoved { get; }
        public bool FreezeWiltNextTurn { get; }
        public int PointValue { get; }

        public PieceStatus(
            Seat seat,
            PieceType type,
            int coordinate,
            bool isGhost = false,
            bool lotusBlooming = false,
            bool inHarmony = false,
            int wiltLevel = 0,
            int previousWiltLevel = 0,
            int turnsSinceMoved = 0,
            bool freezeWiltNextTurn = false,
            int pointValue = 1)
        {
            Seat = seat;
            Type = type;
            Coordinate = coordinate;
            IsGhost = isGhost;
            LotusBlooming = lotusBlooming;
            InHarmony = inHarmony;
            WiltLevel = wiltLevel;
            PreviousWiltLevel = previousWiltLevel;
            TurnsSinceMoved = turnsSinceMoved;
            FreezeWiltNextTurn = freezeWiltNextTurn;
            PointValue = pointValue;
        }

        public BoardPiece ToBoardPiece() =>
            new BoardPiece(Seat, Type, Coordinate, IsGhost, LotusBlooming);
    }
}
