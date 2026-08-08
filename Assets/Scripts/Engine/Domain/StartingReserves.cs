using System.Collections.Generic;
using PaiSho.Pieces;

namespace PaiSho.Domain
{
    /// <summary>Canonical starting reserve counts per piece type.</summary>
    public static class StartingReserves
    {
        public const int FlowerCount = 6;
        public const int AccentCount = 3;

        public static readonly PieceType[] Flowers =
        {
            PieceType.Jasmine,
            PieceType.Lily,
            PieceType.Jade,
            PieceType.Rose,
            PieceType.Rhododendron,
            PieceType.Chrysanthemum
        };

        public static readonly PieceType[] Accents =
        {
            PieceType.Boat,
            PieceType.Rock,
            PieceType.Knotweed,
            PieceType.Wheel,
            PieceType.Lotus,
            PieceType.Orchid
        };

        public static Dictionary<PieceType, int> Create()
        {
            var counts = new Dictionary<PieceType, int>();
            foreach (PieceType flower in Flowers)
                counts[flower] = FlowerCount;
            foreach (PieceType accent in Accents)
                counts[accent] = AccentCount;
            return counts;
        }

        public static int CountFor(PieceType type)
        {
            foreach (PieceType flower in Flowers)
            {
                if (flower == type)
                    return FlowerCount;
            }

            foreach (PieceType accent in Accents)
            {
                if (accent == type)
                    return AccentCount;
            }

            return 0;
        }
    }
}
