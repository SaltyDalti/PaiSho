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
        /// Checks adjacent pieces around the placed or moved piece, and captures enemy pieces in disharmony.
        /// </summary>
        public void CheckForCaptures(Piece placedPiece)
        {
            List<int> neighbors = BoardManager.Instance.GetAdjacentCoordinates(placedPiece.GetPosition());

            foreach (int coord in neighbors)
            {
                Piece neighbor = BoardManager.Instance.GetPieceAt(coord);

                if (neighbor == null)
                    continue;

                if (neighbor.Owner != placedPiece.Owner)
                {
                    if (HarmonyManager.Instance.IsDisharmony(placedPiece, neighbor))
                    {
                        Debug.Log($"[CaptureManager] Captured enemy piece: {neighbor.Type} at coordinate {coord}");

                        BoardManager.Instance.RemovePiece(neighbor);

                        // TODO: Add captured piece to PotManager if tracking captured pieces.
                        // PotManager.Instance.AddCapturedPiece(neighbor);
                    }
                }
            }
        }
    }
}
