using PaiSho.Domain;
using Xunit;

namespace PaiSho.Rules.Tests
{
    public class GardenRulesTests
    {
        [Theory]
        [InlineData(47)]
        [InlineData(66)]
        [InlineData(180 - 7)] // 173 mid-row mixed
        [InlineData(199)]
        [InlineData(313)]
        public void MixedBorders_AreDualLegal(int coordinate)
        {
            Assert.Equal(GardenZone.Mixed, GardenRules.Classify(coordinate));
            Assert.True(GardenRules.AllowsFlowerColor(whiteFlower: true, coordinate));
            Assert.True(GardenRules.AllowsFlowerColor(whiteFlower: false, coordinate));
        }

        [Theory]
        // Outer-diagonal light cells that previously inherited Neutral (dual) via NeutralGardenBorder.
        [InlineData(46)]
        [InlineData(64)]
        [InlineData(154)]
        [InlineData(206)]
        [InlineData(314)]
        // Interior light-only adjacent to mixed
        [InlineData(65)]
        [InlineData(84)]
        public void LightOnly_AllowsWhite_NotRed(int coordinate)
        {
            Assert.Equal(GardenZone.Light, GardenRules.Classify(coordinate));
            Assert.True(GardenRules.IsWhiteOnly(coordinate));
            Assert.True(GardenRules.AllowsFlowerColor(whiteFlower: true, coordinate));
            Assert.False(GardenRules.AllowsFlowerColor(whiteFlower: false, coordinate));
        }

        [Theory]
        [InlineData(48)]
        [InlineData(68)]
        [InlineData(168)]
        [InlineData(192)]
        [InlineData(312)]
        [InlineData(67)]
        [InlineData(86)]
        public void DarkOnly_AllowsRed_NotWhite(int coordinate)
        {
            Assert.Equal(GardenZone.Dark, GardenRules.Classify(coordinate));
            Assert.True(GardenRules.IsRedOnly(coordinate));
            Assert.True(GardenRules.AllowsFlowerColor(whiteFlower: false, coordinate));
            Assert.False(GardenRules.AllowsFlowerColor(whiteFlower: true, coordinate));
        }

        [Fact]
        public void PointsTouchingMixed_DoNotInheritDualLegality()
        {
            // 66 is mixed; 65 (light) and 67 (dark) touch it and must stay exclusive.
            Assert.Equal(GardenZone.Mixed, GardenRules.Classify(66));
            Assert.Equal(GardenZone.Light, GardenRules.Classify(65));
            Assert.Equal(GardenZone.Dark, GardenRules.Classify(67));

            Assert.False(GardenRules.AllowsFlowerColor(whiteFlower: false, 65));
            Assert.False(GardenRules.AllowsFlowerColor(whiteFlower: true, 67));
        }

        [Theory]
        [InlineData(GardenRules.SouthGate)]
        [InlineData(GardenRules.MiddleGate)]
        [InlineData(GardenRules.NorthGate)]
        public void Ports_RejectBothFlowerColors(int coordinate)
        {
            Assert.Equal(GardenZone.Port, GardenRules.Classify(coordinate));
            Assert.False(GardenRules.AllowsFlowerColor(whiteFlower: true, coordinate));
            Assert.False(GardenRules.AllowsFlowerColor(whiteFlower: false, coordinate));
        }

        [Fact]
        public void OpenNeutral_AllowsBothColors()
        {
            // Legal diamond cell in neither garden set (not a port).
            const int coordinate = 26;
            Assert.Equal(GardenZone.Neutral, GardenRules.Classify(coordinate));
            Assert.True(GardenRules.AllowsFlowerColor(whiteFlower: true, coordinate));
            Assert.True(GardenRules.AllowsFlowerColor(whiteFlower: false, coordinate));
        }
    }
}
