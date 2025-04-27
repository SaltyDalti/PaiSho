using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Game;

namespace PaiSho.Board
{
    public class BoardManager : MonoBehaviour
    {
        public static BoardManager Instance;

        private Dictionary<int, Piece> piecesByCoordinate = new Dictionary<int, Piece>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public void PlacePiece(Piece piece, int coordinate)
        {
            if (piecesByCoordinate.ContainsKey(coordinate))
                Debug.LogWarning($"Warning: Overwriting piece at {coordinate}");

            piecesByCoordinate[coordinate] = piece;
            piece.SetBoardCoordinate(coordinate);
        }

        public Piece PlacePiece(Player owner, PieceType type, int coordinate)
        {
            // Assuming you have a prefab system later; placeholder for now
            GameObject pieceObj = new GameObject(type.ToString());
            Piece piece = pieceObj.AddComponent<Piece>();
            piece.Initialize(owner, type);
            PlacePiece(piece, coordinate);
            return piece;
        }

        public void MovePiece(Piece piece, int toCoordinate)
        {
            if (!piecesByCoordinate.ContainsValue(piece))
            {
                Debug.LogError("Piece not found on board");
                return;
            }

            int currentCoordinate = piece.GetPosition();
            piecesByCoordinate.Remove(currentCoordinate);
            piecesByCoordinate[toCoordinate] = piece;
            piece.SetBoardCoordinate(toCoordinate);
        }

        public Piece GetPieceAt(int coordinate)
        {
            piecesByCoordinate.TryGetValue(coordinate, out var piece);
            return piece;
        }

        public void RemovePiece(Piece piece)
        {
            int coord = piece.GetPosition();
            piecesByCoordinate.Remove(coord);
            Destroy(piece.gameObject);
        }

        public List<Piece> GetAllPieces()
        {
            return new List<Piece>(piecesByCoordinate.Values);
        }

        public bool IsOccupied(int coordinate)
        {
            return piecesByCoordinate.ContainsKey(coordinate);
        }
    }
}
