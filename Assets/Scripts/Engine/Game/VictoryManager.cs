using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Board;

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
        /// Check if a harmonic ring has been formed around the center port.
        /// </summary>
        public bool CheckForHarmonyRingEnd(Player player, List<Piece> allPieces)
        {
            List<int> centerRing = new List<int> { 171, 172, 173, 191, 210, 229, 248, 247, 246, 227, 208, 189 };
            HashSet<int> playerCoordinates = new ();

            foreach (var piece in allPieces)
            {
                if (piece.Owner != player || piece.IsGhost) continue;
                playerCoordinates.Add(piece.GetPosition());
            }

            bool harmonicRing = true;
            foreach (var coord in centerRing)
            {
                bool found = false;
                foreach (var piece in allPieces)
                {
                    if (piece.Owner == player && HarmonyManager.Instance.IsHarmony(piece, BoardManager.Instance.GetPieceAt(coord)))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    harmonicRing = false;
                    break;
                }
            }

            if (harmonicRing)
            {
                Debug.Log($">>> {player} completed a Harmonic Ring! Ending the game.");
                GameManager.Instance.EndGame(player);
                return true;
            }

            return false;
        }

        /// <summary>
        /// A fallback quick harmony check, used for bonus scoring.
        /// </summary>
        public int CountPlayerHarmonies(Player player, List<Piece> allPieces)
        {
            int count = 0;

            foreach (var a in allPieces)
            {
                if (a.Owner != player) continue;

                foreach (var b in allPieces)
                {
                    if (a == b || b.Owner != player) continue;

                    if (HarmonyManager.Instance.IsHarmony(a, b))
                        count++;
                }
            }

            return count / 2; // Each harmony counted twice
        }
    }
}
