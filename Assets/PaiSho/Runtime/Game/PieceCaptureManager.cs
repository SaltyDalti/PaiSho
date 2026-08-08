using PaiSho.Pieces;
using UnityEngine;

namespace PaiSho.Game
{
    /// <summary>
    /// Compatibility shim — prefer <see cref="CaptureManager.TryResolveCapture"/>.
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

            return CaptureManager.Instance.TryResolveCapture(attacker, target);
        }
    }
}
