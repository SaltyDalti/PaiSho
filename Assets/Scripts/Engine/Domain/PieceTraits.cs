using PaiSho.Pieces;

namespace PaiSho.Domain
{
    /// <summary>Pure piece-type classification used by harmony and related rules.</summary>
    public static class PieceTraits
    {
        public static bool IsFlower(PieceType type)
        {
            return type == PieceType.Jasmine
                || type == PieceType.Lily
                || type == PieceType.Jade
                || type == PieceType.Rose
                || type == PieceType.Rhododendron
                || type == PieceType.Chrysanthemum
                || type == PieceType.Lotus
                || type == PieceType.Orchid;
        }

        public static bool IsAccent(PieceType type)
        {
            return type == PieceType.Boat
                || type == PieceType.Rock
                || type == PieceType.Knotweed
                || type == PieceType.Wheel;
        }

        /// <summary>Orchid cannot form harmony (see Piece.CanFormHarmony).</summary>
        public static bool CanFormHarmony(PieceType type) => type != PieceType.Orchid;

        public static bool BlocksHarmony(PieceType type) => type == PieceType.Knotweed;
    }
}
