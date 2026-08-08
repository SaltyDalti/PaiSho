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
            return BoardManager.Instance.IsLegalPosition(coordinate) &&
                   !BoardManager.Instance.IsOccupied(coordinate);
        }

        /// <summary>
        /// Check if a coordinate is on the opponent's side of the board.
        /// Host plays from negative Z (lower rows); Opponent from positive Z.
        /// </summary>
        public static bool IsOnOpponentSide(int coordinate, Player player)
        {
            int row = coordinate / BoardUtils.CoordStride; // 0..18 for z -9..9
            if (player == Player.Host)
                return row > 9; // Opponent half (positive Z)
            return row < 9; // Host half (negative Z)
        }

        /// <summary>
        /// Basic validation if the coordinate is usable at all.
        /// </summary>
        public static bool IsValidPlacement(int coordinate)
        {
            return BoardManager.Instance.IsLegalPosition(coordinate) &&
                   !BoardManager.Instance.IsOccupied(coordinate);
        }
    }
}
