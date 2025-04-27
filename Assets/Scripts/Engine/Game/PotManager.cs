
using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class PotManager : MonoBehaviour
    {
        public static PotManager Instance;

        public class CapturedPieceInfo
        {
            public PieceType Type;
            public int Coordinate;
            public Player Owner;

            public CapturedPieceInfo(Piece piece)
            {
                Type = piece.Type;
                Coordinate = piece.GetPosition();
                Owner = piece.Owner;
            }
        }

        private List<CapturedPieceInfo> capturedTiles = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public void RecordCapture(Piece piece)
        {
            var captureInfo = new CapturedPieceInfo(piece);
            capturedTiles.Add(captureInfo);
            Debug.Log($">>> Captured: {piece.Type} from {captureInfo.Owner} at position {captureInfo.Coordinate}");
        }

        public List<CapturedPieceInfo> GetAllCapturedPieces()
        {
            return new List<CapturedPieceInfo>(capturedTiles);
        }

        public int CountCapturedBy(Player player)
        {
            return capturedTiles.FindAll(p => p.Owner == player).Count;
        }
    }
}
