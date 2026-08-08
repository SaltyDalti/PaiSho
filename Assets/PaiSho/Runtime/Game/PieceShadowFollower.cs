using UnityEngine;
using PaiSho.Board;

namespace PaiSho.Game
{
    /// <summary>
    /// Travel shadows are disabled — piece shading comes from scene lighting / mesh shadows.
    /// Kept as a no-op API so drag/move callers stay simple.
    /// </summary>
    public class PieceShadowFollower : MonoBehaviour
    {
        public static PieceShadowFollower Attach(Transform piece, BoardLayout layout = null)
        {
            if (piece == null)
                return null;

            foreach (PieceShadowFollower stale in piece.GetComponents<PieceShadowFollower>())
            {
                if (stale != null)
                    stale.Detach();
            }

            return null;
        }

        public void Detach()
        {
            Destroy(this);
        }
    }
}
