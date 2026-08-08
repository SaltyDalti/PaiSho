using System.Collections.Generic;
using UnityEngine;
using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class WheelRotationManager : MonoBehaviour
    {
        public static WheelRotationManager Instance;

        // 8-neighbor ring around the wheel in stride-20 encoding (clockwise from north).
        private static readonly int[] rotationOffsets =
        {
            -BoardUtils.CoordStride,                 // N
            -BoardUtils.CoordStride + 1,             // NE
            1,                                       // E
            BoardUtils.CoordStride + 1,              // SE
            BoardUtils.CoordStride,                  // S
            BoardUtils.CoordStride - 1,              // SW
            -1,                                      // W
            -BoardUtils.CoordStride - 1              // NW
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        /// <summary>
        /// Rotate adjacent tiles around a Wheel piece.
        /// </summary>
        public void RotateAdjacentTiles(Piece wheel)
        {
            if (!wheel.CausesRotation())
                return;

            int center = wheel.GetPosition();
            List<int> validPositions = new List<int>();

            foreach (int offset in rotationOffsets)
            {
                int pos = center + offset;
                if (BoardManager.Instance.IsLegalPosition(pos))
                    validPositions.Add(pos);
            }

            List<Piece> pieces = new List<Piece>();
            foreach (int pos in validPositions)
            {
                Piece p = BoardManager.Instance.GetPieceAt(pos);
                if (p != null)
                    pieces.Add(p);
                else
                    pieces.Add(null); // Keep indexes aligned
            }

            for (int i = 0; i < pieces.Count; i++)
            {
                int nextIndex = (i + 1) % pieces.Count;
                if (pieces[i] != null && pieces[nextIndex] == null)
                {
                    int destination = validPositions[nextIndex];
                    BoardManager.Instance.MovePiece(pieces[i], destination);
                    Debug.Log($">>> Rotated {pieces[i].Type} to {destination} via Wheel");
                }
            }
        }
    }
}
