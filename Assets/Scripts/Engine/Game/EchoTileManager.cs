using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Board;

namespace PaiSho.Game
{
    public class EchoTileManager : MonoBehaviour
    {
        public static EchoTileManager Instance;

        private Dictionary<Player, int> revivalPoints = new Dictionary<Player, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;

            revivalPoints[Player.Host] = 0;
            revivalPoints[Player.Opponent] = 0;
        }

        /// <summary>
        /// Add revival points to a player and attempt to summon Echo Tiles if enough are earned.
        /// </summary>
        public void AddRevivalPoints(Player player, int amount)
        {
            if (amount <= 0) return;

            revivalPoints[player] += amount;
            DebugLogger.Log($">>> {player} earned {amount} revival point(s). Total: {revivalPoints[player]}");

            while (revivalPoints[player] >= 10)
            {
                TrySummonEcho(player);
                revivalPoints[player] -= 10;
            }
        }

        /// <summary>
        /// Attempts to summon a ghost Echo Tile for a player.
        /// </summary>
        private void TrySummonEcho(Player player)
        {
            List<PotManager.CapturedPieceInfo> pot = PotManager.Instance.GetAllCapturedPieces();
            List<PotManager.CapturedPieceInfo> eligible = pot.FindAll(p =>
                p.Owner == player &&
                p.Type != PieceType.Lotus &&
                p.Type != PieceType.Orchid &&
                Piece.IsFlowerType(p.Type));

            if (eligible.Count == 0) return;

            var rand = new System.Random();
            var candidate = eligible[rand.Next(eligible.Count)];
            int targetPos = candidate.Coordinate;

            Piece echo = BoardManager.Instance.PlacePiece(player, candidate.Type, targetPos);
            echo.IsNewThisTurn = true;
            echo.SetVisualState("ghost");
            echo.PointValue *= 2;
            echo.IsGhost = true;

            DebugLogger.Log($">>> {player} summoned a Ghost Echo Tile: {candidate.Type} at {targetPos}. It must be moved to awaken.");
        }

        public int GetEchoCount(Player player)
        {
            return 0; // Default for now
        }

        /// <summary>
        /// Called when a ghost Echo Tile moves for the first time to awaken it.
        /// </summary>
        public void OnEchoMoved(Piece piece)
        {
            if (!piece.IsGhost) return;

            piece.IsGhost = false;
            piece.SetVisualState("vibrant");
            DebugLogger.Log($">>> Echo Tile {piece.Type} has entered play.");
        }

        /// <summary>
        /// Get the total number of Echo Tiles currently summoned.
        /// (Placeholder: returns 0 for now.)
        /// </summary>
        public int GetEchoCount()
        {
            return 0;
        }
    }
}
