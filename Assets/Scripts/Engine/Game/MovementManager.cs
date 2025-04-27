
using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class MovementManager : MonoBehaviour
    {
        public static MovementManager Instance;

        private const int MAX_MOVES_PER_TURN = 2;
        private HashSet<Piece> movedThisTurn = new();

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
        }

        public bool CanMoveTile(Piece piece)
        {
            Player currentPlayer = GameManager.Instance.GetCurrentPlayer();

            if (movedThisTurn.Contains(piece))
                return false;

            if (piece.Owner != currentPlayer)
                return false;

            // Bonus movement with momentum
            if (movedThisTurn.Count >= MAX_MOVES_PER_TURN)
            {
                if (movedThisTurn.Count >= 4) return false; // absolute cap

                if (MomentumManager.Instance.TrySpendMomentum(currentPlayer, "Bonus Movement"))
                    return true;

                return false;
            }

            return true;
        }

        public void RegisterMovement(Piece piece)
        {
            if (piece.IsImmovable()) {
                DebugLogger.LogWarning($"Cannot move immovable tile: {piece.Type}"); return; }
            if (CanMoveTile(piece))
            {
                DebugLogger.Log($"{piece.Type} moved by {piece.Owner}");
            movedThisTurn.Add(piece);
            EchoTileManager.Instance.OnEchoMoved(piece);
            WheelRotationManager.Instance.RotateAdjacentTiles(piece);
            WheelRotationManager.Instance.RotateAdjacentTiles(piece);
                piece.MarkAsMovedThisTurn();
            }
        }

        public bool HasMoved(Piece piece)
        {
            return movedThisTurn.Contains(piece);
        }

        public bool ExceededMoveLimit()
        {
            return movedThisTurn.Count >= MAX_MOVES_PER_TURN;
        }
    }
}
