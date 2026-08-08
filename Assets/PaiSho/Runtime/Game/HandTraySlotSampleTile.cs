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
            // Keep the GameObject active so Transform.Find / world bounds stay valid for tray seating.
            // Only strip interaction and visibility.
            if (!Application.isPlaying)
                return;

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
        }
    }
}
