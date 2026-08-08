using PaiSho.Domain;
using PaiSho.Pieces;
using Xunit;

namespace PaiSho.Rules.Tests
{
    public class HarmonyRulesTests
    {
        [Theory]
        [InlineData(0, 0, 1, 0, 1)]
        [InlineData(0, 0, 1, 1, 1)] // diagonal
        [InlineData(0, 0, 2, 0, 2)]
        [InlineData(-3, 4, -3, 4, 0)]
        public void ChebyshevDistance_MatchesGrid(int ax, int az, int bx, int bz, int expected)
        {
            int a = BoardCoords.ToCoordinate(ax, az);
            int b = BoardCoords.ToCoordinate(bx, bz);
            Assert.Equal(expected, HarmonyRules.ChebyshevDistance(a, b));
        }

        [Fact]
        public void SameFlowerTypes_CanHarmonize()
        {
            Assert.True(HarmonyRules.CanHarmonizeTypes(
                PieceType.Jasmine, PieceType.Jasmine, false, false));
        }

        [Fact]
        public void DifferentFlowerTypes_CannotUnlessLotusBlooming()
        {
            Assert.False(HarmonyRules.CanHarmonizeTypes(
                PieceType.Jasmine, PieceType.Rose, false, false));

            Assert.True(HarmonyRules.CanHarmonizeTypes(
                PieceType.Lotus, PieceType.Rose, lotusABlooming: true, lotusBBlooming: false));

            Assert.False(HarmonyRules.CanHarmonizeTypes(
                PieceType.Lotus, PieceType.Rose, lotusABlooming: false, lotusBBlooming: false));
        }

        [Fact]
        public void Accents_CannotHarmonize()
        {
            Assert.False(HarmonyRules.CanHarmonizeTypes(
                PieceType.Boat, PieceType.Boat, false, false));
            Assert.False(HarmonyRules.CanHarmonizeTypes(
                PieceType.Rock, PieceType.Jasmine, false, false));
        }

        [Fact]
        public void IsHarmony_RequiresSameSeatAdjacentCompatibleTypes()
        {
            int a = BoardCoords.ToCoordinate(0, 0);
            int b = BoardCoords.ToCoordinate(1, 0);

            Assert.True(HarmonyRules.IsHarmony(
                Seat.Host, Seat.Host, PieceType.Jade, PieceType.Jade, a, b, false, false));

            Assert.False(HarmonyRules.IsHarmony(
                Seat.Host, Seat.Opponent, PieceType.Jade, PieceType.Jade, a, b, false, false));

            int far = BoardCoords.ToCoordinate(2, 0);
            Assert.False(HarmonyRules.IsHarmony(
                Seat.Host, Seat.Host, PieceType.Jade, PieceType.Jade, a, far, false, false));
        }

        [Fact]
        public void IsHarmony_AllowsDiagonalAdjacency()
        {
            int a = BoardCoords.ToCoordinate(0, 0);
            int diag = BoardCoords.ToCoordinate(1, 1);

            Assert.True(HarmonyRules.IsHarmony(
                Seat.Opponent, Seat.Opponent, PieceType.Lily, PieceType.Lily, a, diag, false, false));
        }

        [Fact]
        public void IsDisharmony_OppositeSeatsIncompatibleTypes()
        {
            Assert.True(HarmonyRules.IsDisharmony(
                Seat.Host, Seat.Opponent, PieceType.Jasmine, PieceType.Rose, false, false));

            // Same flower type across seats can "type-harmonize", so not disharmony
            // (preserves existing CaptureManager / HarmonyManager behavior).
            Assert.False(HarmonyRules.IsDisharmony(
                Seat.Host, Seat.Opponent, PieceType.Jasmine, PieceType.Jasmine, false, false));

            Assert.False(HarmonyRules.IsDisharmony(
                Seat.Host, Seat.Host, PieceType.Jasmine, PieceType.Rose, false, false));
        }

        [Fact]
        public void BloomingLotus_BlocksDisharmonyWithEnemyFlower()
        {
            Assert.False(HarmonyRules.IsDisharmony(
                Seat.Host, Seat.Opponent, PieceType.Lotus, PieceType.Lily,
                lotusABlooming: true, lotusBBlooming: false));
        }

        [Fact]
        public void PieceTraits_FlowerAndHarmonyFlags()
        {
            Assert.True(PieceTraits.IsFlower(PieceType.Orchid));
            Assert.True(PieceTraits.IsAccent(PieceType.Wheel));
            Assert.False(PieceTraits.CanFormHarmony(PieceType.Orchid));
            Assert.True(PieceTraits.BlocksHarmony(PieceType.Knotweed));
        }
    }
}
