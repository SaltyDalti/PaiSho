using UnityEngine;
using PaiSho.Board;
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
            if (piece == null || piece.Type != PieceType.Lotus)
                return;

            var animator = PieceStateAnimator.Ensure(piece);
            animator?.SyncFromPiece(immediate: false);
            if (piece.IsBlooming())
                animator?.NotifyHarmonyEntered();

            GameplayVisualizer.Instance?.Refresh();
        }

        public void RefreshAllLotusBlooms()
        {
            if (BoardManager.Instance == null)
                return;

            foreach (Piece piece in BoardManager.Instance.GetAllPieces())
            {
                if (piece.Type == PieceType.Lotus)
                    PieceStateAnimator.Ensure(piece)?.SyncFromPiece(immediate: false);
            }
        }
    }
}
