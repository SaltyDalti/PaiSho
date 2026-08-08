using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Domain;

namespace PaiSho.Game
{
    public class VictoryManager : MonoBehaviour
    {
        public static VictoryManager Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        /// <summary>
        /// Check if the player has formed a continuous harmonic ring around the central port.
        /// </summary>
        public bool CheckForHarmonyRingEnd(Player player, List<Piece> allPieces)
        {
            Seat seat = PlacementValidator.ToSeat(player);
            var snapshot = new List<BoardPiece>(allPieces != null ? allPieces.Count : 0);

            if (allPieces != null)
            {
                foreach (Piece piece in allPieces)
                {
                    if (piece == null)
                        continue;

                    snapshot.Add(new BoardPiece(
                        PlacementValidator.ToSeat(piece.Owner),
                        piece.Type,
                        piece.GetPosition(),
                        piece.IsGhost,
                        piece.IsBlooming()));
                }
            }

            if (!VictoryRules.HasHarmonicRing(seat, snapshot))
                return false;

            Debug.Log($">>> {player} completed a Harmonic Ring! Ending the game.");
            GameManager.Instance.EndGame(player);
            return true;
        }
    }
}
