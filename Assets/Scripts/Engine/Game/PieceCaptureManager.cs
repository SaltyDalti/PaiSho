using UnityEngine;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    /// <summary>
    /// Deprecated shim. Capture + pot recording now lives in <see cref="CaptureManager"/>.
    /// Kept so any leftover scene references do not break; forwards to CaptureManager.
    /// </summary>
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
            if (CaptureManager.Instance == null)
                return false;
            return CaptureManager.Instance.TryCapture(attacker, target);
        }
    }
}
