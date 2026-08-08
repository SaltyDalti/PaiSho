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
            return PotManager.Instance != null && PotManager.Instance.IsLotusBlooming(player);
        }

        public void ApplyBloomVisualIfLotus(Piece piece)
        {
            if (piece == null || piece.Type != PieceType.Lotus)
                return;

            if (IsBlooming(piece.Owner))
                piece.SetVisualState("blooming");
            else
                piece.SetVisualState("vibrant");
        }

        /// <summary>
        /// Hook used when a capture feeds bloom/pot economy. Records via PotManager
        /// and refreshes Lotus bloom visuals for the capturer.
        /// </summary>
        public void AddToPot(Piece piece)
        {
            if (piece == null || PotManager.Instance == null)
                return;

            // Capture recording is owned by CaptureManager; this remains an effect hook.
            Debug.Log($"[BloomingManager] Pot effect hook for {piece.Type}.");

            foreach (var lotus in BoardManagerPiecesOfType(PieceType.Lotus))
                ApplyBloomVisualIfLotus(lotus);
        }

        private static System.Collections.Generic.List<Piece> BoardManagerPiecesOfType(PieceType type)
        {
            var result = new System.Collections.Generic.List<Piece>();
            if (PaiSho.Board.BoardManager.Instance == null)
                return result;

            foreach (var piece in PaiSho.Board.BoardManager.Instance.GetAllPieces())
            {
                if (piece != null && piece.Type == type)
                    result.Add(piece);
            }
            return result;
        }
    }
}
