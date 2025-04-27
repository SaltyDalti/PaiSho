
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

        public bool IsBlooming(Player player)
        {
            return PotManager.Instance.IsLotusBlooming(player);
        }

        public void ApplyBloomVisualIfLotus(Piece piece)
        {
            if (piece.Type == PieceType.Lotus && IsBlooming(piece.Owner))
            {
                piece.SetVisualState("blooming"); // Hook for animation/glow later
            }
        }
    }
}
