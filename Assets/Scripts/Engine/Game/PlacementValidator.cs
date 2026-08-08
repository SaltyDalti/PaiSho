using PaiSho.Board;
using PaiSho.Pieces;
using PaiSho.Domain;

namespace PaiSho.Game
{
    public static class PlacementValidator
    {
        public static Seat ToSeat(Player player) =>
            player == Player.Host ? Seat.Host : Seat.Opponent;

        public static MatchPhase CurrentPhase()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsSpringPhase())
                return MatchPhase.Spring;
            return MatchPhase.Play;
        }

        /// <summary>
        /// Check if a placement coordinate is legal for a new piece (empty legal garden point).
        /// </summary>
        public static bool CanPlace(Player player, PieceType type, int coordinate)
        {
            bool hasReserve = true;
            if (GameManager.Instance != null && !GameManager.Instance.IsSpringPhase()
                && ReserveManager.Instance != null)
            {
                hasReserve = ReserveManager.Instance.HasPieceAvailable(player, type);
            }

            PlacementResult result = PlacementRules.Evaluate(
                new PlacementIntent(
                    CurrentPhase(),
                    ToSeat(player),
                    type,
                    coordinate,
                    hasReserve),
                coord => BoardManager.Instance != null && BoardManager.Instance.IsOccupied(coord));

            return result.IsAllowed;
        }

        /// <summary>
        /// Full Domain evaluation with reason — preferred for placement managers.
        /// </summary>
        public static PlacementResult Evaluate(
            Player player,
            PieceType type,
            int coordinate,
            bool hasReserveAvailable)
        {
            return PlacementRules.Evaluate(
                new PlacementIntent(
                    CurrentPhase(),
                    ToSeat(player),
                    type,
                    coordinate,
                    hasReserveAvailable),
                coord => BoardManager.Instance != null && BoardManager.Instance.IsOccupied(coord));
        }

        /// <summary>
        /// Check if a coordinate is on the opponent's side of the board.
        /// Host plays from negative Z (lower rows); Opponent from positive Z.
        /// </summary>
        public static bool IsOnOpponentSide(int coordinate, Player player)
        {
            return PlacementRules.IsOnOpponentSide(coordinate, ToSeat(player));
        }

        /// <summary>
        /// Basic validation if the coordinate is usable at all.
        /// </summary>
        public static bool IsValidPlacement(int coordinate)
        {
            return PlacementRules.IsEmptyLegalPoint(
                coordinate,
                coord => BoardManager.Instance != null && BoardManager.Instance.IsOccupied(coord));
        }
    }
}
