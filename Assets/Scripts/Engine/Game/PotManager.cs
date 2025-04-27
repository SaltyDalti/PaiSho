using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class PotManager : MonoBehaviour
    {
        public static PotManager Instance;

        private Dictionary<Player, List<PieceType>> capturedPieces = new Dictionary<Player, List<PieceType>>();
        private Dictionary<Player, int> revivalPoints = new Dictionary<Player, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;

            capturedPieces[Player.Host] = new List<PieceType>();
            capturedPieces[Player.Opponent] = new List<PieceType>();
            revivalPoints[Player.Host] = 0;
            revivalPoints[Player.Opponent] = 0;
        }

        public void CapturePiece(Player capturer, Piece piece)
        {
            capturedPieces[capturer].Add(piece.Type);
            Debug.Log($"{capturer} captured {piece.Type} at {piece.GetPosition()}.");
        }

        public List<PieceType> GetCapturedPieces(Player player)
        {
            return new List<PieceType>(capturedPieces[player]);
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

        /// <summary>
        /// Struct representing captured piece data.
        /// </summary>
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

        /// <summary>
        /// Return all captured piece information for echo summoning.
        /// </summary>
        public List<CapturedPieceInfo> GetAllCapturedPieces()
        {
            List<CapturedPieceInfo> allCaptured = new List<CapturedPieceInfo>();


            foreach (var player in capturedPieces.Keys)
            {
                foreach (var type in capturedPieces[player])
                {
                    allCaptured.Add(new CapturedPieceInfo(player, type, -1));
                }
            }

            return allCaptured;
        }
    }
}
