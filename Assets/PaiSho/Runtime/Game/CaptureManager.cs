using System.Collections.Generic;
using UnityEngine;
using PaiSho.Board;
using PaiSho.Pieces;

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
        /// Captures only occur when a piece lands on an enemy tile (handled in TileSelector).
        /// Adjacent disharmony does not capture.
        /// </summary>
        public void TryCapture(Piece newPiece, int coord)
        {
        }
    }
}
