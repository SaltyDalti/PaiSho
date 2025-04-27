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
        /// Check if the player has formed a continuous harmonic ring around the central port.
        /// </summary>
        public bool CheckForHarmonyRingEnd(Player player, List<Piece> allPieces)
        {
            HashSet<int> playerCoordinates = new HashSet<int>();
            foreach (var piece in allPieces)
            {
                if (piece.Owner == player && !piece.IsGhost)
                    playerCoordinates.Add(piece.GetPosition());
            }

            foreach (var startCoord in playerCoordinates)
            {
                HashSet<int> visited = new HashSet<int>();
                if (IsHarmonicLoop(player, startCoord, startCoord, visited, -1, playerCoordinates))
                {
                    Debug.Log($">>> {player} completed a Harmonic Ring! Ending the game.");
                    GameManager.Instance.EndGame(player);
                    return true;
                }
            }

            return false;
        }

        private bool IsHarmonicLoop(Player player, int currentCoord, int startCoord, HashSet<int> visited, int previousCoord, HashSet<int> playerCoords)
        {
            visited.Add(currentCoord);
            foreach (int neighbor in BoardManager.Instance.GetAdjacentCoordinates(currentCoord))
            {
                if (neighbor == previousCoord) continue;
                if (!playerCoords.Contains(neighbor)) continue;

                Piece currentPiece = BoardManager.Instance.GetPieceAt(currentCoord);
                Piece neighborPiece = BoardManager.Instance.GetPieceAt(neighbor);

                if (!HarmonyManager.Instance.IsHarmony(currentPiece, neighborPiece)) continue;

                if (neighbor == startCoord && visited.Count >= 4 && IsCenterPortEncircled(visited))
                    return true;

                if (!visited.Contains(neighbor))
                    if (IsHarmonicLoop(player, neighbor, startCoord, visited, currentCoord, playerCoords))
                        return true;
            }

            visited.Remove(currentCoord);
            return false;
        }

        /// <summary>
        /// Efficiently checks if the loop encircles the center port.
        /// Assumes center port coordinate is fixed (e.g., 209 or similar).
        /// </summary>
        private bool IsCenterPortEncircled(HashSet<int> loopCoords)
        {
            int centerPort = BoardUtils.CenterPortCoordinate;
            foreach (int coord in BoardManager.Instance.GetAdjacentCoordinates(centerPort))
            {
                if (!loopCoords.Contains(coord))
                    return false;
            }
            return true;
        }
    }
}
