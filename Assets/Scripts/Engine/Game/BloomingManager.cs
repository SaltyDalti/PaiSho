using UnityEngine;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class BloomingManager : MonoBehaviour
    {
        public static BloomingManager Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        /// <summary>
        /// Check if the Lotus for a player is currently blooming.
        /// </summary>
        public bool IsBlooming(Player player)
        {
            return PotManager.Instance.IsLotusBlooming(player);
        }

        /// <summary>
        /// Apply a bloom visual effect to a Lotus piece if it is blooming.
        /// </summary>
        public void ApplyBloomVisualIfLotus(Piece piece)
        {
            if (piece.Type == PieceType.Lotus && IsBlooming(piece.Owner))
            {
                piece.SetVisualState("blooming"); // Hook for animation/glow later
            }
        }

        /// <summary>
        /// Add a piece to the bloom pot (placeholder for future effect hooks).
        /// </summary>
        public void AddToPot(Piece piece)
        {
            // Placeholder for now.
            Debug.Log($"Added {piece.Type} to bloom pot (no special effect yet).");
        }
    }
}
