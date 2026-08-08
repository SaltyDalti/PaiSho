using PaiSho.Pieces;

namespace PaiSho.Domain
{
    /// <summary>Immutable board occupancy snapshot for pure rules evaluation.</summary>
    public readonly struct BoardPiece
    {
        public Seat Seat { get; }
        public PieceType Type { get; }
        public int Coordinate { get; }
        public bool IsGhost { get; }
        public bool LotusBlooming { get; }

        public BoardPiece(
            Seat seat,
            PieceType type,
            int coordinate,
            bool isGhost = false,
            bool lotusBlooming = false)
        {
            Seat = seat;
            Type = type;
            Coordinate = coordinate;
            IsGhost = isGhost;
            LotusBlooming = lotusBlooming;
        }
    }
}
