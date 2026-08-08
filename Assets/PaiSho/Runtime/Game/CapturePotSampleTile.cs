using UnityEngine;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    /// <summary>Baked literal piece prefab under a capture-pot stack marker. Hidden during play.</summary>
    [DisallowMultipleComponent]
    public class CapturePotSampleTile : MonoBehaviour
    {
        [SerializeField] private PieceType pieceType;
        [SerializeField] private int stackIndex;

        public PieceType PieceType => pieceType;
        public int StackIndex => stackIndex;

        public void Configure(PieceType type, int stack)
        {
            pieceType = type;
            stackIndex = stack;
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
                gameObject.SetActive(false);
        }

        public static Transform FindTransform(Transform stack)
        {
            if (stack == null)
                return null;

            foreach (CapturePotSampleTile marker in stack.GetComponentsInChildren<CapturePotSampleTile>(true))
                return marker.transform;

            return stack.Find(CapturePotStackCatalog.SampleTileName);
        }

        public static bool HasSample(Transform stack) => FindTransform(stack) != null;
    }
}
