using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class PotManager : MonoBehaviour
    {
        public static PotManager Instance;

        private readonly Dictionary<Player, List<CapturedPieceInfo>> capturedPieces =
            new Dictionary<Player, List<CapturedPieceInfo>>();
        private readonly Dictionary<Player, int> revivalPoints = new Dictionary<Player, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;

            capturedPieces[Player.Host] = new List<CapturedPieceInfo>();
            capturedPieces[Player.Opponent] = new List<CapturedPieceInfo>();
            revivalPoints[Player.Host] = 0;
            revivalPoints[Player.Opponent] = 0;
        }

        public void CapturePiece(Player capturer, Piece piece)
        {
            RecordCapture(capturer, piece);
        }

        public void RecordCapture(Player player, Piece piece)
        {
            if (piece == null)
                return;

            var info = new CapturedPieceInfo(piece.Owner, piece.Type, piece.GetPosition());
            capturedPieces[player].Add(info);
            Debug.Log($"{player} captured {piece.Type} at {info.Coordinate}.");
        }

        public List<PieceType> GetCapturedPieces(Player player)
        {
            var types = new List<PieceType>();
            foreach (var info in capturedPieces[player])
                types.Add(info.Type);
            return types;
        }

        public int CountCapturedBy(Player player)
        {
            return capturedPieces[player].Count;
        }

        public void AddRevivalPoints(Player player, int points)
        {
            if (!revivalPoints.ContainsKey(player))
                revivalPoints[player] = 0;

            revivalPoints[player] += points;
            Debug.Log($"{player} gained {points} revival points (now {revivalPoints[player]}).");
        }

        public int GetRevivalPoints(Player player)
        {
            return revivalPoints.TryGetValue(player, out int points) ? points : 0;
        }

        public bool IsLotusBlooming(Player player)
        {
            Player opponent = player == Player.Host ? Player.Opponent : Player.Host;
            return CountCapturedBy(player) < CountCapturedBy(opponent);
        }

        public struct CapturedPieceInfo
        {
            public Player Owner;
            public PieceType Type;
            public int Coordinate;

            public CapturedPieceInfo(Player owner, PieceType type, int coordinate)
            {
                Owner = owner;
                Type = type;
                Coordinate = coordinate;
            }
        }

        public List<CapturedPieceInfo> GetAllCapturedPieces()
        {
            var allCaptured = new List<CapturedPieceInfo>();
            foreach (var list in capturedPieces.Values)
                allCaptured.AddRange(list);
            return allCaptured;
        }
    }
}
