using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Board;

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
        }

        public void RegisterPlacement(Piece piece)
        {
            placedThisTurn.Add(piece);
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
            movedThisTurn.Clear();
            placedThisTurn.Clear();
        }
    }
}
