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
        /// Check adjacent pieces around the placed/moved piece and capture enemies in disharmony.
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
                        Debug.Log($"Captured enemy piece at {coord}!");

                        BoardManager.Instance.RemovePiece(neighbor);

                        // (Optional) Later: PotManager.Instance.AddCapturedPiece(neighbor);
                    }
                }
            }
        }
    }
}
