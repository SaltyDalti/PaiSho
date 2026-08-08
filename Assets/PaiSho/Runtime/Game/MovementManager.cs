using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class MovementManager : MonoBehaviour
    {
        public static MovementManager Instance;

        private const int MaxMovesPerTurn = 1;
        private readonly HashSet<Piece> movedThisTurn = new();
        private readonly Dictionary<Player, int> movedCountByPlayer = new();
        private readonly Dictionary<Player, bool> placedThisTurn = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public void StartTurn()
        {
            movedThisTurn.Clear();
            movedCountByPlayer.Clear();
            placedThisTurn.Clear();
        }

        public void RegisterPlacement(Player player)
        {
            placedThisTurn[player] = true;
        }

        public bool PlacedThisTurn(Player player)
        {
            return placedThisTurn.TryGetValue(player, out bool placed) && placed;
        }

        public int GetMovedTileCount(Player player)
        {
            return movedCountByPlayer.TryGetValue(player, out int count) ? count : 0;
        }

        public int GetMovedTileCount()
        {
            int total = 0;
            foreach (var count in movedCountByPlayer.Values)
                total += count;
            return total;
        }

        public bool CanMoveTile(Piece piece)
        {
            Player currentPlayer = GameManager.Instance.GetCurrentPlayer();

            if (piece == null || piece.IsImmovable())
                return false;

            if (movedThisTurn.Contains(piece))
                return false;

            if (piece.Owner != currentPlayer)
                return false;

            if (movedThisTurn.Count >= MaxMovesPerTurn)
            {
                // Second move only when Extra Move was already purchased (Momentum spent up front).
                return GameManager.Instance != null && GameManager.Instance.PendingBonusMove;
            }

            return true;
        }

        public void RegisterMovement(Piece piece)
        {
            if (piece.IsImmovable())
            {
                DebugLogger.LogWarning($"Cannot move immovable tile: {piece.Type}");
                return;
            }

            if (!CanMoveTile(piece))
                return;

            movedThisTurn.Add(piece);
            if (!movedCountByPlayer.ContainsKey(piece.Owner))
                movedCountByPlayer[piece.Owner] = 0;
            movedCountByPlayer[piece.Owner]++;
            EchoTileManager.Instance.OnEchoMoved(piece);
            piece.MarkAsMovedThisTurn();
            DebugLogger.Log($"{piece.Type} moved by {piece.Owner}");
        }

        public void RegisterWheelRotation(Piece wheel)
        {
            if (wheel == null || wheel.Type != PieceType.Wheel)
                return;

            if (!CanMoveTile(wheel))
                return;

            movedThisTurn.Add(wheel);
            if (!movedCountByPlayer.ContainsKey(wheel.Owner))
                movedCountByPlayer[wheel.Owner] = 0;
            movedCountByPlayer[wheel.Owner]++;
            wheel.MarkAsMovedThisTurn();
            DebugLogger.Log($"Wheel rotated by {wheel.Owner}");
        }

        public bool HasMoved(Piece piece) => movedThisTurn.Contains(piece);

        public bool ExceededMoveLimit() => movedThisTurn.Count >= MaxMovesPerTurn;
    }
}
