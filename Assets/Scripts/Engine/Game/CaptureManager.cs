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

        public void TryCapture(Piece newPiece, int coord)
        {
            List<Piece> toCapture = new List<Piece>();

            // Check if capturing tile placed directly on an enemy
            Piece occupying = BoardManager.Instance.GetPieceAt(coord);
            if (occupying != null && occupying.Owner != newPiece.Owner)
            {
                if (HarmonyManager.Instance.IsDisharmony(newPiece, occupying))
                {
                    toCapture.Add(occupying);
                }
            }

            // Check adjacent tiles for disharmony-based capture (radiant capture)
            int[] directions = { -1, 1, -19, 19, -20, -18, 18, 20 };
            foreach (int offset in directions)
            {
                int neighborCoord = coord + offset;
                if (!BoardUtils.IsValidPointCoordinate(neighborCoord)) continue;

                Piece neighbor = BoardManager.Instance.GetPieceAt(neighborCoord);
                if (neighbor != null && neighbor.Owner != newPiece.Owner)
                {
                    if (HarmonyManager.Instance.IsDisharmony(newPiece, neighbor))
                    {
                        toCapture.Add(neighbor);
                    }
                }
            }

            // Execute captures
            foreach (var piece in toCapture)
            {
                Debug.Log($"{piece.Type} at {piece.GetPosition()} captured by {newPiece.Type} at {coord}");
                BoardManager.Instance.RemovePiece(piece);
                BloomingManager.Instance.AddToPot(piece.Owner);
            }
        }
    }
}
