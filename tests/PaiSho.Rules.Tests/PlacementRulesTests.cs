using System.Collections.Generic;
using PaiSho.Domain;
using PaiSho.Pieces;
using Xunit;

namespace PaiSho.Rules.Tests
{
    public class PlacementRulesTests
    {
        private static PlacementResult Eval(
            MatchPhase phase,
            Seat seat,
            PieceType type,
            int coord,
            bool hasReserve,
            HashSet<int> occupied = null)
        {
            occupied ??= new HashSet<int>();
            return PlacementRules.Evaluate(
                new PlacementIntent(phase, seat, type, coord, hasReserve),
                c => occupied.Contains(c));
        }

        [Fact]
        public void OpeningFlowers_AreSeatSpecific()
        {
            Assert.Equal(PieceType.Jasmine, PlacementRules.GetOpeningFlower(Seat.Host));
            Assert.Equal(PieceType.Rose, PlacementRules.GetOpeningFlower(Seat.Opponent));
        }

        [Fact]
        public void Spring_AllowsOpeningFlowerOnEmptyLegalPoint()
        {
            int coord = BoardCoords.ToCoordinate(0, -3);
            var result = Eval(MatchPhase.Spring, Seat.Host, PieceType.Jasmine, coord, hasReserve: false);
            Assert.True(result.IsAllowed);
            Assert.Equal(PlacementDenyReason.None, result.Reason);
        }

        [Fact]
        public void Spring_AllowsHostLilyOnOwnSideNeutral()
        {
            // Domain stride-20 host-side point (not in live garden sets → Neutral dual-legal).
            int coord = BoardCoords.ToCoordinate(0, -3);
            var result = Eval(MatchPhase.Spring, Seat.Host, PieceType.Lily, coord, hasReserve: false);
            Assert.True(result.IsAllowed);
            Assert.Equal(PlacementDenyReason.None, result.Reason);
        }

        [Fact]
        public void Spring_AllowsHostLilyOnLiveLightGarden()
        {
            // Live mid=180 SW light-only (GardenRules) — Host own side.
            const int coord = 46;
            Assert.Equal(GardenZone.Light, GardenRules.Classify(coord));
            var result = Eval(MatchPhase.Spring, Seat.Host, PieceType.Lily, coord, hasReserve: false);
            Assert.True(result.IsAllowed);
        }

        [Fact]
        public void Spring_RejectsHostLilyOnLiveRedOnlyGarden()
        {
            // Live SE dark-only — Host own side but white flowers forbidden.
            const int coord = 48;
            Assert.Equal(GardenZone.Dark, GardenRules.Classify(coord));
            Assert.True(PlacementRules.IsOnOwnSide(coord, Seat.Host));
            var result = Eval(MatchPhase.Spring, Seat.Host, PieceType.Lily, coord, hasReserve: true);
            Assert.False(result.IsAllowed);
            Assert.Equal(PlacementDenyReason.WrongGarden, result.Reason);
        }

        [Fact]
        public void Spring_RejectsHostLilyOnOpponentSide()
        {
            int coord = BoardCoords.ToCoordinate(0, 5);
            var result = Eval(MatchPhase.Spring, Seat.Host, PieceType.Lily, coord, hasReserve: true);
            Assert.False(result.IsAllowed);
            Assert.Equal(PlacementDenyReason.WrongSide, result.Reason);
        }

        [Fact]
        public void Spring_RejectsNonBasicFlower()
        {
            int coord = BoardCoords.ToCoordinate(0, -3);
            var result = Eval(MatchPhase.Spring, Seat.Host, PieceType.Boat, coord, hasReserve: true);
            Assert.False(result.IsAllowed);
            Assert.Equal(PlacementDenyReason.WrongOpeningFlower, result.Reason);
        }

        [Fact]
        public void Spring_AllowsHostRoseOnOwnSideWhenDrawn()
        {
            // Live product draws any basic flower — Rose is legal on Host side Neutral/dark/mixed.
            int coord = BoardCoords.ToCoordinate(0, -3);
            var result = Eval(MatchPhase.Spring, Seat.Host, PieceType.Rose, coord, hasReserve: true);
            Assert.True(result.IsAllowed);
        }

        [Fact]
        public void Spring_IgnoresReserveRequirement()
        {
            int coord = BoardCoords.ToCoordinate(1, 2);
            var result = Eval(MatchPhase.Spring, Seat.Opponent, PieceType.Rose, coord, hasReserve: false);
            Assert.True(result.IsAllowed);
        }

        [Fact]
        public void Play_RequiresReserve()
        {
            int coord = BoardCoords.ToCoordinate(0, 0);
            var denied = Eval(MatchPhase.Play, Seat.Host, PieceType.Lily, coord, hasReserve: false);
            Assert.False(denied.IsAllowed);
            Assert.Equal(PlacementDenyReason.PieceNotInReserve, denied.Reason);

            var allowed = Eval(MatchPhase.Play, Seat.Host, PieceType.Lily, coord, hasReserve: true);
            Assert.True(allowed.IsAllowed);
        }

        [Fact]
        public void Rejects_OccupiedAndInvalidPoints()
        {
            int legal = BoardCoords.ToCoordinate(0, 0);
            var occupied = new HashSet<int> { legal };

            var onPiece = Eval(MatchPhase.Play, Seat.Host, PieceType.Boat, legal, true, occupied);
            Assert.Equal(PlacementDenyReason.Occupied, onPiece.Reason);

            int illegal = BoardCoords.ToCoordinate(9, 4); // outside garden shape
            var bad = Eval(MatchPhase.Play, Seat.Host, PieceType.Boat, illegal, true);
            Assert.Equal(PlacementDenyReason.InvalidCoordinate, bad.Reason);
        }

        [Fact]
        public void EndPhase_RejectsPlacement()
        {
            int coord = BoardCoords.CenterPortCoordinate;
            var result = Eval(MatchPhase.End, Seat.Host, PieceType.Jasmine, coord, true);
            Assert.Equal(PlacementDenyReason.WrongPhase, result.Reason);
        }

        [Theory]
        [InlineData(0, -5, Seat.Host, false)]  // host side
        [InlineData(0, 5, Seat.Host, true)]    // opponent side for host
        [InlineData(0, 0, Seat.Host, false)]   // center line
        [InlineData(0, 5, Seat.Opponent, false)]
        [InlineData(0, -5, Seat.Opponent, true)]
        public void OpponentSide_UsesEncodedRows(int x, int z, Seat seat, bool expected)
        {
            int coord = BoardCoords.ToCoordinate(x, z);
            Assert.Equal(expected, PlacementRules.IsOnOpponentSide(coord, seat));
        }

        [Fact]
        public void OwnSide_RejectsCenterLine()
        {
            int coord = BoardCoords.ToCoordinate(0, 0);
            Assert.False(PlacementRules.IsOnOwnSide(coord, Seat.Host));
            Assert.False(PlacementRules.IsOnOwnSide(coord, Seat.Opponent));
        }

        [Fact]
        public void StartingReserves_MatchCanonicalCounts()
        {
            Dictionary<PieceType, int> reserves = StartingReserves.Create();
            Assert.Equal(6, reserves[PieceType.Jasmine]);
            Assert.Equal(6, reserves[PieceType.Rose]);
            Assert.Equal(3, reserves[PieceType.Boat]);
            Assert.Equal(3, reserves[PieceType.Lotus]);
            Assert.Equal(12, reserves.Count);
        }

        [Fact]
        public void IsEmptyLegalPoint_NullOccupancy_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                PlacementRules.IsEmptyLegalPoint(BoardCoords.CenterPortCoordinate, null));
        }
    }
}
