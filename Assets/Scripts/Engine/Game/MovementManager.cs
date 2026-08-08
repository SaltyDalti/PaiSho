using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Board;
using PaiSho.Domain;

namespace PaiSho.Game
{
    public class MovementManager : MonoBehaviour
    {
        public static MovementManager Instance;

        private HashSet<Piece> movedThisTurn = new HashSet<Piece>();
        private HashSet<Piece> placedThisTurn = new HashSet<Piece>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public void RegisterMove(Piece piece)
        {
            movedThisTurn.Add(piece);
            piece.HasMovedThisTurn = true;
        }

        public void RegisterPlacement(Piece piece)
        {
            placedThisTurn.Add(piece);
        }

        public List<int> GetLegalMoves(Piece piece)
        {
            if (piece == null)
                return new List<int>();

            int currentCoord = piece.GetPosition();
            // GetModifiedMovementRange returns 1 normally, 2 in-season for spring flowers (+1 bonus).
            int seasonalBonus = Mathf.Max(0, piece.GetModifiedMovementRange() - 1);

            return MovementRules.GetLegalMoves(
                piece.Type,
                currentCoord,
                seasonalBonus,
                coord => BoardManager.Instance != null && BoardManager.Instance.IsOccupied(coord));
        }

        public bool PlacedThisTurn(Player player)
        {
            foreach (var piece in placedThisTurn)
            {
                if (piece.Owner == player)
                    return true;
            }
            return false;
        }

        public int GetMovedTileCount()
        {
            return movedThisTurn.Count;
        }

        public void ClearTurnData()
        {
            foreach (var piece in movedThisTurn)
            {
                piece.HasMovedThisTurn = false;
            }
            movedThisTurn.Clear();
            placedThisTurn.Clear();
        }
    }
}
