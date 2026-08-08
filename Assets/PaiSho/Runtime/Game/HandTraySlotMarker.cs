using UnityEngine;
using PaiSho;
using PaiSho.Board;
using PaiSho.Pieces;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaiSho.Game
{
    /// <summary>
    /// Hand-tray slot anchor. Move/rotate this Slot_N transform in the scene.
    /// A baked SampleTile child is a literal game piece prefab for perfect alignment.
    /// </summary>
    [DisallowMultipleComponent]
    public class HandTraySlotMarker : MonoBehaviour
    {
        public const string SampleTileName = "SampleTile";

        [SerializeField] private int slotIndex;
        [SerializeField] private Player owner = Player.Host;
        [SerializeField] private PieceType samplePieceType = PieceType.Jasmine;
        [SerializeField] private bool drawGizmo;

        public int SlotIndex => slotIndex;
        public Player Owner => owner;
        public PieceType SamplePieceType => samplePieceType;

        private void OnEnable() => RefreshVisibility();

        private void OnValidate()
        {
            ResolveSlotIndexFromName();
            ResolveOwnerFromHierarchy();
        }

        public void Configure(int index, Player trayOwner)
        {
            slotIndex = index;
            owner = trayOwner;
            samplePieceType = HandTraySlotSampleTiles.GetPieceTypeForSlot(index);
            gameObject.name = $"{GameBoardSetup.SlotNamePrefix}{index}";
            RefreshVisibility();
        }

        public void RefreshVisibility()
        {
            ResolveSlotIndexFromName();
            ResolveOwnerFromHierarchy();

            Transform sample = transform.Find(SampleTileName);
            if (sample != null)
                sample.gameObject.SetActive(!Application.isPlaying);
        }

        public void DrawEditorGizmo(bool selected)
        {
            if (!drawGizmo || Application.isPlaying)
                return;

            if (transform.Find(SampleTileName) != null)
                return;

            DrawFallbackGizmo(selected);
        }

        private void ResolveSlotIndexFromName()
        {
            string prefix = GameBoardSetup.SlotNamePrefix;
            if (!gameObject.name.StartsWith(prefix, System.StringComparison.Ordinal))
                return;

            if (int.TryParse(gameObject.name.Substring(prefix.Length), out int parsed))
                slotIndex = Mathf.Clamp(parsed, 0, HandTrayAlignmentDefaults.MaxSlots - 1);
        }

        private void ResolveOwnerFromHierarchy()
        {
            Transform current = transform;
            while (current != null)
            {
                if (current.name == GameBoardSetup.HostTrayName)
                {
                    owner = Player.Host;
                    return;
                }

                if (current.name == GameBoardSetup.OpponentTrayName)
                {
                    owner = Player.Opponent;
                    return;
                }

                current = current.parent;
            }
        }

        private float ResolveCellSpacing()
        {
            var layout = GetComponentInParent<BoardLayout>();
            if (layout == null)
                layout = FindAnyObjectByType<BoardLayout>();

            return layout != null ? layout.CellSpacing : 0.42f;
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmo || Application.isPlaying)
                return;

            DrawEditorGizmo(selected: false);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmo || Application.isPlaying)
                return;

            DrawEditorGizmo(selected: true);
        }

        private void DrawFallbackGizmo(bool selected)
        {
            float diameter = WoodTheme.GetTileDiameter(ResolveCellSpacing());
            float baseHeight = diameter * 0.18f;
            float capHeight = diameter * 0.2f;
            float totalHeight = baseHeight + capHeight;
            Vector3 center = new Vector3(0f, totalHeight * 0.5f, 0f);
            Vector3 size = new Vector3(diameter * 0.92f, totalHeight, diameter * 0.92f);

            Color fill = new Color(0.2f, 0.85f, 1f, selected ? 0.35f : 0.15f);
            Color wire = new Color(0.2f, 0.85f, 1f, 0.9f);

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = fill;
            Gizmos.DrawCube(center, size);
            Gizmos.color = wire;
            Gizmos.DrawWireCube(center, size);
            Gizmos.matrix = Matrix4x4.identity;

#if UNITY_EDITOR
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11
            };
            style.normal.textColor = wire;
            Handles.Label(transform.TransformPoint(center + Vector3.up * (size.y * 0.55f)), slotIndex.ToString(), style);
#endif
        }
    }
}
