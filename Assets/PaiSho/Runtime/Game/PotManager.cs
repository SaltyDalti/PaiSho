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
            public Player CapturedBy;

            public CapturedPieceInfo(Piece piece, Player capturedBy, int coordinateOverride = -1)
            {
                Type = piece.Type;
                Coordinate = coordinateOverride >= 0 ? coordinateOverride : piece.GetPosition();
                Owner = piece.Owner;
                CapturedBy = capturedBy;
            }

            public CapturedPieceInfo(Player owner, PieceType type, int coordinate = -1)
            {
                Owner = owner;
                Type = type;
                Coordinate = coordinate;
                CapturedBy = owner == Player.Host ? Player.Opponent : Player.Host;
            }
        }

        private readonly List<CapturedPieceInfo> capturedTiles = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public void RecordCapture(Piece piece, Player capturedBy, int coordinateOverride = -1)
        {
            var captureInfo = new CapturedPieceInfo(piece, capturedBy, coordinateOverride);
            capturedTiles.Add(captureInfo);
            DebugLogger.Log(
                $">>> Captured: {piece.Type} from {captureInfo.Owner} by {capturedBy} at {captureInfo.Coordinate}");
        }

        public void RecordCapture(Piece piece)
        {
            RecordCapture(piece, piece.Owner == Player.Host ? Player.Opponent : Player.Host);
        }

        public void AddCapturedPiece(Player player, PieceType type)
        {
            capturedTiles.Add(new CapturedPieceInfo(player, type));
        }

        public List<CapturedPieceInfo> GetAllCapturedPieces()
        {
            return new List<CapturedPieceInfo>(capturedTiles);
        }

        public int CountCapturedBy(Player capturer)
        {
            return capturedTiles.FindAll(p => p.CapturedBy == capturer).Count;
        }

        public void ClearCaptured() => capturedTiles.Clear();

        public void ClearCapturedBy(Player capturer) =>
            capturedTiles.RemoveAll(p => p.CapturedBy == capturer);

        public bool IsLotusBlooming(Player player)
        {
            Player opponent = player == Player.Host ? Player.Opponent : Player.Host;
            return CountCapturedBy(player) > CountCapturedBy(opponent);
        }
    }
}
