using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Game;

namespace PaiSho.Board
{
    public class TileLifecycleManager : MonoBehaviour
    {
        public static TileLifecycleManager Instance;

        private readonly Dictionary<Player, int> revivedCounts = new Dictionary<Player, int>();
        private readonly Dictionary<Player, int> movedCounts = new Dictionary<Player, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;

            revivedCounts[Player.Host] = 0;
            revivedCounts[Player.Opponent] = 0;
            movedCounts[Player.Host] = 0;
            movedCounts[Player.Opponent] = 0;
        }

        /// <summary>
        /// Called at the start of each turn to update tile lifecycle states.
        /// </summary>
        public void OnTurnStart(List<Piece> allPieces)
        {
            Debug.Log($"[TileLifecycleManager] Checking managers...");

            if (SeasonManager.Instance == null)
                Debug.LogError("[TileLifecycleManager] SeasonManager.Instance is NULL!");

            if (EchoTileManager.Instance == null)
                Debug.LogError("[TileLifecycleManager] EchoTileManager.Instance is NULL!");

            for (int i = 0; i < allPieces.Count; i++)
            {
                var piece = allPieces[i];

                if (piece == null)
                {
                    Debug.LogWarning($"[TileLifecycleManager] Null Piece detected in allPieces list at index {i} / {allPieces.Count}. Skipping.");
                    continue;
                }

                if (piece.IsNewThisTurn)
                {
                    piece.IsNewThisTurn = false;
                    continue;
                }

                if (!piece.HasMovedThisTurn)
                {
                    piece.TurnsSinceMoved++;
                }
                else
                {
                    piece.TurnsSinceMoved = 0;
                    movedCounts[piece.Owner] = movedCounts.GetValueOrDefault(piece.Owner) + 1;
                }

                if (!piece.InHarmony)
                    piece.TurnsSinceHarmonized++;
                else
                    piece.TurnsSinceHarmonized = 0;

                if (!piece.FreezeWiltNextTurn)
                {
                    int previousWilt = piece.WiltLevel;
                    UpdateWiltLevel(piece);

                    if (piece.WiltLevel < previousWilt)
                    {
                        int points = 1;
                        Season currentSeason = SeasonManager.Instance.GetCurrentSeason();

                        if (currentSeason == Season.Spring)
                            points = 2;
                        else if (currentSeason == Season.Summer)
                            points = 1;
                        else if (currentSeason == Season.Autumn)
                            points = piece.InHarmony ? 3 : 1;
                        else if (currentSeason == Season.Winter)
                            points = 2;

                        revivedCounts[piece.Owner] = revivedCounts.GetValueOrDefault(piece.Owner) + 1;
                        EchoTileManager.Instance?.AddRevivalPoints(piece.Owner, points);
                    }

                    piece.PreviousWiltLevel = piece.WiltLevel;
                }
                else
                {
                    piece.FreezeWiltNextTurn = false;
                }
            }
        }

        private void UpdateWiltLevel(Piece piece)
        {
            int totalNeglect = Mathf.Max(piece.TurnsSinceMoved, piece.TurnsSinceHarmonized);

            if (totalNeglect >= 4)
            {
                piece.WiltLevel = 2;
                piece.PointValue = -1;
                piece.SetVisualState("fully-wilted");
            }
            else if (totalNeglect == 3)
            {
                piece.WiltLevel = 1;
                piece.PointValue = 0;
                piece.SetVisualState("wilted");
            }
            else // 0, 1, or 2 turns of neglect
            {
                piece.WiltLevel = 0;
                piece.PointValue = 1;
                piece.SetVisualState("vibrant");
            }
        }

        public int GetTotalRevived(Player player)
        {
            return revivedCounts.TryGetValue(player, out int count) ? count : 0;
        }

        public int GetMovedTileCount(Player player)
        {
            return movedCounts.TryGetValue(player, out int count) ? count : 0;
        }

        /// <summary>
        /// Adds a revival point when Knotweed drains occur.
        /// </summary>
        public void RegisterKnotweedDrain(Player fromPlayer)
        {
            EchoTileManager.Instance.AddRevivalPoints(fromPlayer, 1);
        }
    }
}
