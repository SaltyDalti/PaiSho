
using System.Collections.Generic;
using UnityEngine;
using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class WheelRotationManager : MonoBehaviour
    {
        public static WheelRotationManager Instance;

        private static readonly int[] rotationOffsets = { -20, -19, 1, 20, 19, -1, -21, -39 };

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public void RotateAdjacentTiles(Piece wheel)
        {
            if (!wheel.CausesRotation()) return;

            int center = wheel.GetPosition();
            List<int> validPositions = new();

            foreach (int offset in rotationOffsets)
            {
                int pos = center + offset;
                if (BoardManager.Instance.IsLegalPosition(pos))
                    validPositions.Add(pos);
            }

            List<Piece> pieces = new();
            foreach (int pos in validPositions)
            {
                Piece p = BoardManager.Instance.GetPieceAt(pos);
                pieces.Add(p);
            }

            for (int i = 0; i < pieces.Count; i++)
            {
                int nextIndex = (i + 1) % pieces.Count;
                if (pieces[i] != null && BoardManager.Instance.GetPieceAt(validPositions[nextIndex]) == null)
                {
                    BoardManager.Instance.MovePiece(pieces[i], validPositions[nextIndex]);
                    Debug.Log($">>> Rotated {pieces[i].Type} to {validPositions[nextIndex]} via Wheel");
                }
            }
        }
    }
}
