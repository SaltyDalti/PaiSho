using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Game;

namespace PaiSho.Board
{
    public class TileLifecycleManager : MonoBehaviour
    {
        public static TileLifecycleManager Instance;

        private readonly Dictionary<Player, int> revivedCount = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;

            revivedCount[Player.Host] = 0;
            revivedCount[Player.Opponent] = 0;
        }

        public void ResetForNewMatch()
        {
            revivedCount[Player.Host] = 0;
            revivedCount[Player.Opponent] = 0;
        }

        public int GetTotalRevived(Player player)
        {
            return revivedCount.TryGetValue(player, out int count) ? count : 0;
        }

        // Called from Knotweed drain effect
        public void RegisterKnotweedDrain(Player fromPlayer)
        {
            EchoTileManager.Instance.AddRevivalPoints(fromPlayer, 1);
        }

        public void OnTurnStart(List<Piece> allPieces)
        {
            // Spring placement is opening setup — neglect/wilt must not age tiles yet,
            // or early spring flowers lose bud glow before play begins.
            if (GameStateManager.Instance != null && GameStateManager.Instance.IsSpringPhase())
            {
                foreach (var piece in allPieces)
                {
                    if (piece != null && piece.IsNewThisTurn)
                        piece.IsNewThisTurn = false;
                }

                return;
            }

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
                        int points = 1;

                        switch (SeasonManager.Instance.GetCurrentSeason())
                        {
                            case Season.Spring:
                                points = 2;
                                break;
                            case Season.Summer:
                                points = 1;
                                break;
                            case Season.Autumn:
                                points = piece.InHarmony ? 3 : 1;
                                break;
                            case Season.Winter:
                                points = 2;
                                break;
                            default:
                                points = 1;
                                break;
                        }

                        EchoTileManager.Instance.AddRevivalPoints(piece.Owner, points);
                        revivedCount[piece.Owner] = GetTotalRevived(piece.Owner) + 1;
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
            piece.PreviousWiltLevel = piece.WiltLevel;

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

            PieceStateAnimator.Ensure(piece)?.SyncFromPiece(immediate: false);
        }
    }
}
