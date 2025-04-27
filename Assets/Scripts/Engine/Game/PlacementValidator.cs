using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public static class PlacementValidator
    {
        /// <summary>
        /// Check if a placement coordinate is legal for a new piece.
        /// </summary>
        public static bool CanPlace(Player player, PieceType type, int coordinate)
        {
            // Tile must be placed on an unoccupied, legal position
            return BoardManager.Instance.IsLegalPosition(coordinate) &&
                   !BoardManager.Instance.IsOccupied(coordinate);
        }

        /// <summary>
        /// Check if a coordinate is on the opponent's side of the board.
        /// </summary>
        public static bool IsOnOpponentSide(int coordinate, Player player)
        {
            // Assume board is 19 rows (0-360 approx), Host = bottom half, Opponent = top half
            int row = coordinate / 19;
            if (player == Player.Host)
                return row < 9; // Rows 0-8
            else
                return row > 9; // Rows 10-18
        }

        /// <summary>
        /// Basic validation if the coordinate is usable at all (optional extra check).
        /// </summary>
        public static bool IsValidPlacement(int coordinate)
        {
            return BoardManager.Instance.IsLegalPosition(coordinate) &&
                   !BoardManager.Instance.IsOccupied(coordinate);
        }
    }
}
