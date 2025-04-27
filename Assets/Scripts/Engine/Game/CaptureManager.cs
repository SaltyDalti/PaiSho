using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Board;

namespace PaiSho.Game
{
    public class CaptureManager : MonoBehaviour
    {
        public static CaptureManager Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        /// <summary>
        /// Try capturing enemy tiles after placing a new piece.
        /// </summary>
        public void TryCapture(Piece newPiece, int coord)
        {
            List<Piece> toCapture = new List<Piece>();

            // Direct capture (placed onto enemy)
            Piece occupying = BoardManager.Instance.GetPieceAt(coord);
            if (occupying != null && occupying.Owner != newPiece.Owner)
            {
                if (HarmonyManager.Instance.IsDisharmony(newPiece, occupying))
                    toCapture.Add(occupying);
            }

            // Radiant capture (adjacent disharmony)
            int[] directions = { -1, 1, -19, 19, -20, -18, 18, 20 };
            foreach (int offset in directions)
            {
                int neighborCoord = coord + offset;
                Piece neighbor = BoardManager.Instance.GetPieceAt(neighborCoord);

                if (neighbor != null && neighbor.Owner != newPiece.Owner)
                {
                    if (HarmonyManager.Instance.IsDisharmony(newPiece, neighbor))
                        toCapture.Add(neighbor);
                }
            }

            // Execute captures
            foreach (var piece in toCapture)
            {
                DebugLogger.Log($">>> {piece.Type} at {piece.GetPosition()} captured by {newPiece.Type} at {coord}");

                PotManager.Instance.CapturePiece(newPiece.Owner, piece); // Proper capture recording
                BoardManager.Instance.RemovePiece(piece);
            }
        }
    }
}
