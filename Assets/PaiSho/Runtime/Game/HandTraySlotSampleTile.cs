using PaiSho.Pieces;
using UnityEngine;

namespace PaiSho.Game
{
    /// <summary>Baked literal piece prefab under a hand-tray slot. Hidden during play.</summary>
    [DisallowMultipleComponent]
    public class HandTraySlotSampleTile : MonoBehaviour
    {
        [SerializeField] private PieceType pieceType;

        public PieceType PieceType => pieceType;

        public void SetPieceType(PieceType type) => pieceType = type;

        private void Start()
        {
            if (!Application.isPlaying)
                return;

#if UNITY_WEBGL && !UNITY_EDITOR
            // Keep the transform as a seating pose anchor; only strip meshes (HJ-37).
            HandTrayLayoutUtility.HollowSampleTile(transform);
#else
            // Desktop: keep GO active so Transform.Find / world bounds stay valid for seating.
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null)
                    renderer.enabled = false;
            }

            foreach (Collider collider in GetComponentsInChildren<Collider>(true))
            {
                if (collider != null)
                    collider.enabled = false;
            }
#endif
        }
    }
}
