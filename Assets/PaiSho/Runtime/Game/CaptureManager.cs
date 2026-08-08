using PaiSho.Board;
using PaiSho.Pieces;
using UnityEngine;

namespace PaiSho.Game
{
    /// <summary>
    /// Single capture story: destination landings remove the victim to the capturer's pot.
    /// Adjacent disharmony does not capture (see house rules).
    /// </summary>
    public class CaptureManager : MonoBehaviour
    {
        public static CaptureManager Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        /// <summary>
        /// Placement seating hook — captures only happen on moves that land on an enemy tile.
        /// Kept so BoardManager call sites stay stable; intentionally a no-op.
        /// </summary>
        public void TryCapture(Piece newPiece, int coord)
        {
        }

        /// <summary>
        /// Resolve a capture when <paramref name="attacker"/> lands on <paramref name="target"/>.
        /// Returns false if the target is seasonally immune or missing.
        /// </summary>
        public bool TryResolveCapture(Piece attacker, Piece target, int capturedFromCoordinate = -1)
        {
            if (attacker == null || target == null)
                return false;

            if (!target.CanBeCaptured())
            {
                Debug.Log($">>> {target.Type} is immune to capture during {SeasonManager.Instance?.GetCurrentSeason()}");
                return false;
            }

            int from = capturedFromCoordinate >= 0 ? capturedFromCoordinate : target.BoardCoordinate;
            Player capturer = attacker.Owner;

            if (PotManager.Instance != null)
                PotManager.Instance.RecordCapture(target, capturer, from);

            if (PotVisualManager.Instance != null)
                PotVisualManager.Instance.SendToPot(target, capturer);
            else if (BoardManager.Instance != null)
                BoardManager.Instance.ReleasePieceFromBoard(target);

            GameLogManager.Instance?.Log(ActionType.Capture, capturer, target.Type, from, attacker.BoardCoordinate);
            DebugLogger.Log($"{attacker.Type} captured {target.Type} from {target.Owner}");
            Debug.Log($">>> {capturer} captured {target.Type}!");
            return true;
        }
    }
}
