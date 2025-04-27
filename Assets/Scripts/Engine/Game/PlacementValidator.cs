using PaiSho.Board;

namespace PaiSho.Game
{
    public static class PlacementValidator
    {
        /// <summary>
        /// Check if a placement coordinate is legal for a new piece.
        /// </summary>
        public static bool IsValidPlacement(int coordinate)
        {
            // Simplified: Must be a legal board point and not occupied.
            return BoardManager.Instance.IsOccupied(coordinate) == false;
        }
    }
}
