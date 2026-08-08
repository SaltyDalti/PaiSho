using System.Collections.Generic;

namespace PaiSho.Pieces
{
    public readonly struct PieceHarmonyProfile
    {
        public readonly HashSet<PieceType> Harmonic;
        public readonly HashSet<PieceType> Disharmonic;

        public PieceHarmonyProfile(HashSet<PieceType> harmonic, HashSet<PieceType> disharmonic)
        {
            Harmonic = harmonic;
            Disharmonic = disharmonic;
        }

        public static PieceHarmonyProfile Empty => new(
            new HashSet<PieceType>(),
            new HashSet<PieceType>());
    }

    public static class PieceHarmonyProfiles
    {
        private static readonly Dictionary<PieceType, PieceHarmonyProfile> Profiles = new()
        {
            [PieceType.Jasmine] = H(PieceType.Lily, PieceType.Rhododendron, PieceType.Lotus,
                PieceType.Rose, PieceType.Orchid),
            [PieceType.Rose] = H(PieceType.Jade, PieceType.Chrysanthemum, PieceType.Lotus,
                PieceType.Jasmine, PieceType.Orchid),
            [PieceType.Lily] = H(PieceType.Jasmine, PieceType.Jade, PieceType.Lotus,
                PieceType.Chrysanthemum, PieceType.Orchid),
            [PieceType.Jade] = H(PieceType.Lily, PieceType.Rose, PieceType.Lotus,
                PieceType.Rhododendron, PieceType.Orchid),
            [PieceType.Chrysanthemum] = H(PieceType.Rose, PieceType.Rhododendron, PieceType.Lotus,
                PieceType.Lily, PieceType.Orchid),
            [PieceType.Rhododendron] = H(PieceType.Chrysanthemum, PieceType.Jasmine, PieceType.Lotus,
                PieceType.Jade, PieceType.Orchid),
            [PieceType.Lotus] = H(PieceType.Jasmine, PieceType.Rose, PieceType.Lily,
                PieceType.Jade, PieceType.Chrysanthemum, PieceType.Rhododendron),
            [PieceType.Orchid] = PieceHarmonyProfile.Empty,
            [PieceType.Boat] = PieceHarmonyProfile.Empty,
            [PieceType.Rock] = PieceHarmonyProfile.Empty,
            [PieceType.Knotweed] = PieceHarmonyProfile.Empty,
            [PieceType.Wheel] = PieceHarmonyProfile.Empty,
        };

        public static PieceHarmonyProfile Get(PieceType type)
        {
            return Profiles.TryGetValue(type, out var profile)
                ? profile
                : PieceHarmonyProfile.Empty;
        }

        private static PieceHarmonyProfile H(
            PieceType h1, PieceType h2, PieceType h3,
            PieceType d1, PieceType d2)
        {
            return new PieceHarmonyProfile(
                new HashSet<PieceType> { h1, h2, h3 },
                new HashSet<PieceType> { d1, d2 });
        }

        private static PieceHarmonyProfile H(
            params PieceType[] harmonic)
        {
            return new PieceHarmonyProfile(
                new HashSet<PieceType>(harmonic),
                new HashSet<PieceType>());
        }
    }
}
