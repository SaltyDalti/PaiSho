using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;

namespace PaiSho.Board
{
    public class BoardManager : MonoBehaviour
    {
        public static BoardManager Instance;

        private Dictionary<int, Piece> boardPositions = new ();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public void PlacePiece(Piece piece, int coordinate)
        {
            if (boardPositions.ContainsKey(coordinate))
            {
                Debug.LogWarning($"Attempted to place {piece.Type} at occupied coordinate {coordinate}!");
                return;
            }

            boardPositions[coordinate] = piece;
            piece.SetPosition(coordinate);
            Debug.Log($"Placed {piece.Type} at {coordinate}");
        }

        public void MovePiece(Piece piece, int newCoordinate)
        {
            int oldCoordinate = piece.GetPosition();

            if (boardPositions.ContainsKey(newCoordinate))
            {
                Debug.LogWarning($"Attempted to move to occupied coordinate {newCoordinate}!");
                return;
            }

            boardPositions.Remove(oldCoordinate);
            boardPositions[newCoordinate] = piece;
            piece.SetPosition(newCoordinate);
            Debug.Log($"Moved {piece.Type} from {oldCoordinate} to {newCoordinate}");
        }

        public void RemovePiece(Piece piece)
        {
            int coordinate = piece.GetPosition();

            if (boardPositions.ContainsKey(coordinate))
            {
                boardPositions.Remove(coordinate);
                Debug.Log($"Removed {piece.Type} from {coordinate}");
            }
            else
            {
                Debug.LogWarning($"Tried to remove {piece.Type}, but it was not found on board.");
            }
        }

        public Piece GetPieceAt(int coordinate)
        {
            boardPositions.TryGetValue(coordinate, out Piece piece);
            return piece;
        }

        public List<Piece> GetAllPieces()
        {
            return new List<Piece>(boardPositions.Values);
        }

        public bool IsOccupied(int coordinate)
        {
            return boardPositions.ContainsKey(coordinate);
        }

        public void ClearBoard()
        {
            boardPositions.Clear();
            Debug.Log("Board cleared.");
        }
    }
}
