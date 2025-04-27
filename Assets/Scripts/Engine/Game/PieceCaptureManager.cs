
using UnityEngine;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class PieceCaptureManager : MonoBehaviour
    {
        public static PieceCaptureManager Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public bool TryCapture(Piece attacker, Piece target)
        {
            if (!target.CanBeCaptured())
            {
                Debug.Log($">>> {target.Type} is immune to capture during {SeasonManager.Instance.GetCurrentSeason()}");
                return false;
            }

            PotManager.Instance.RecordCapture(target);
            DebugLogger.Log($"{attacker.Type} captured {target.Type} from {target.Owner}");
            BoardManager.Instance.RemovePiece(target);
            Debug.Log($">>> {attacker.Owner} captured {target.Type}!");
            return true;
        }
    }
}
