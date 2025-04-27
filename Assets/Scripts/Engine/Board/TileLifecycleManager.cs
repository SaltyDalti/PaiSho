using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Game;
using PaiSho.Board;

namespace PaiSho.Board
{
    public class TileLifecycleManager : MonoBehaviour
    {
        public static TileLifecycleManager Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        /// <summary>
        /// Called at the start of each turn to update tile lifecycle states.
        /// </summary>
        public void OnTurnStart(List<Piece> allPieces)
        {
            foreach (var piece in allPieces)
            {
                if (piece.IsNewThisTurn)
                {
                    piece.IsNewThisTurn = false;
                    continue;
                }

                if (!piece.HasMovedThisTurn)
                    piece.TurnsSinceMoved++;
                else
                    piece.TurnsSinceMoved = 0;

                if (!piece.InHarmony)
                    piece.TurnsSinceHarmonized++;
                else
                    piece.TurnsSinceHarmonized = 0;

                if (!piece.FreezeWiltNextTurn)
                {
                    if (piece.WiltLevel < piece.PreviousWiltLevel)
                    {
                        int points = SeasonManager.Instance.GetCurrentSeason() switch
                        {
                            Season.Spring => 2,
                            Season.Summer => 1,
                            Season.Autumn => piece.InHarmony ? 3 : 1,
                            Season.Winter => 2,
                            _ => 1
                        };
                    EchoTileManager.Instance.AddRevivalPoints(piece.Owner, points);
                }

                UpdateWiltLevel(piece);
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
        else if (totalNeglect <= 2)
        {
            piece.WiltLevel = 0;
            piece.PointValue = 1;
            piece.SetVisualState("vibrant");
        }
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
