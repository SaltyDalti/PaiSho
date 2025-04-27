using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;

namespace PaiSho.Board
{
    public class BoardManager : MonoBehaviour
    {
        public static BoardManager Instance;

        private Dictionary<int, Piece> piecesOnBoard = new ();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public Piece GetPieceAt(int coord)
        {
            piecesOnBoard.TryGetValue(coord, out var piece);
            return piece;
        }

        public void PlacePiece(int coord, Piece piece)
        {
            piecesOnBoard[coord] = piece;
        }

        public void RemovePiece(Piece piece)
        {
            piecesOnBoard.Remove(piece.GetPosition());
        }

        public List<Piece> GetAllPieces()
        {
            return new List<Piece>(piecesOnBoard.Values);
        }
    }
}
