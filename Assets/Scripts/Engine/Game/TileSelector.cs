
using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Board;

namespace PaiSho.Game
{
    public class TileSelector : MonoBehaviour
    {
        public static TileSelector Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public void TryPlaceTile(Player player, PieceType type, int coordinate)
        {
            if (!ReserveManager.Instance.HasTile(player, type))
            {
                DebugLogger.LogWarning("Attempted to place a tile not in reserve.");
                return;
            }

            if (!PlacementValidator.Instance.CanPlace(player, type, coordinate))
            {
                DebugLogger.LogWarning("Invalid placement attempted.");
                return;
            }

            if (type == PieceType.Orchid && !PlacementValidator.Instance.IsOnOpponentSide(coordinate, player))
            {
                DebugLogger.LogWarning("Orchid must be placed on opponent's side.");
                return;
            }

            BoardManager.Instance.PlacePiece(player, type, coordinate);
            ReserveManager.Instance.RemoveFromReserve(player, type);
            DebugLogger.Log($"Placed {type} by {player} at {coordinate}");

            GameManager.Instance.MarkTurnComplete();
            GameManager.Instance.EndTurn();
        }
    }
}
