using UnityEngine;
using PaiSho.Pieces;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaiSho.Game
{
    /// <summary>
    /// Capture-pot stack anchor under Slot_N/Stack_N. Move anchors in the prefab; runtime compacts lanes.
    /// </summary>
    [DisallowMultipleComponent]
    public class CapturePotSlotMarker : MonoBehaviour
    {
        [SerializeField] private Player owner = Player.Host;
        [SerializeField] private CapturePotDisplayGroup displayGroup = CapturePotDisplayGroup.Flowers;
        [SerializeField] private int displaySlot;
        [SerializeField] private int stackIndex;
        [SerializeField] private PieceType previewPieceType = PieceType.Jasmine;
        [SerializeField] private bool drawGizmo = true;

        public Player Owner => owner;
        public CapturePotDisplayGroup DisplayGroup => displayGroup;
        public int DisplaySlot => displaySlot;
        public int StackIndex => stackIndex;
        public PieceType PreviewPieceType => previewPieceType;

        private void OnEnable() => RefreshVisibility();

        private void OnValidate()
        {
            ResolveFromHierarchy();
            ResolveFromName();
        }

        public void Configure(Player potOwner, in CapturePotStackCatalog.SlotDefinition slot)
        {
            owner = potOwner;
            displayGroup = slot.Group;
            displaySlot = slot.DisplaySlot;
            stackIndex = slot.StackIndex;
            previewPieceType = slot.PreviewPieceType;
            gameObject.name = slot.StackFolder;
            RefreshVisibility();
        }

        public void RefreshVisibility()
        {
            ResolveFromHierarchy();
            ResolveFromName();

            Transform sample = CapturePotSampleTile.FindTransform(transform);
            if (sample != null)
                sample.gameObject.SetActive(!Application.isPlaying);
        }

        private void ResolveFromHierarchy()
        {
            Transform current = transform;
            while (current != null)
            {
                if (current.name == GameBoardSetup.HostCapturePotName)
                {
                    owner = Player.Host;
                    break;
                }

                if (current.name == GameBoardSetup.OpponentCapturePotName)
                {
                    owner = Player.Opponent;
                    break;
                }

                current = current.parent;
            }

            Transform slotFolder = transform.parent;
            if (slotFolder != null &&
                slotFolder.name.StartsWith(CapturePotDisplayOrder.SlotNamePrefix, System.StringComparison.Ordinal) &&
                int.TryParse(slotFolder.name.Substring(CapturePotDisplayOrder.SlotNamePrefix.Length), out int parsedSlot))
            {
                displaySlot = parsedSlot;
            }

            Transform groupFolder = slotFolder != null ? slotFolder.parent : null;
            if (groupFolder != null)
            {
                if (groupFolder.name == CapturePotDisplayOrder.FlowersFolder)
                    displayGroup = CapturePotDisplayGroup.Flowers;
                else if (groupFolder.name == CapturePotDisplayOrder.SpecialFolder)
                    displayGroup = CapturePotDisplayGroup.SpecialAndOther;
            }

            previewPieceType = CapturePotDisplayOrder.GetPreviewTypeForDisplaySlot(displayGroup, displaySlot);
        }

        private void ResolveFromName()
        {
            string prefix = CapturePotStackCatalog.StackNamePrefix;
            if (!gameObject.name.StartsWith(prefix, System.StringComparison.Ordinal))
                return;

            if (int.TryParse(gameObject.name.Substring(prefix.Length), out int parsed))
                stackIndex = Mathf.Max(0, parsed);
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmo || Application.isPlaying)
                return;

            if (CapturePotSampleTile.HasSample(transform))
                return;

            DrawFallbackGizmo(selected: false);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmo || Application.isPlaying)
                return;

            if (CapturePotSampleTile.HasSample(transform))
                return;

            DrawFallbackGizmo(selected: true);
        }

        private void DrawFallbackGizmo(bool selected)
        {
            float diameter = 0.32f;
            Vector3 center = Vector3.up * (diameter * 0.1f);
            Vector3 size = new Vector3(diameter * 0.85f, diameter * 0.2f, diameter * 0.85f);

            Color fill = GroupColor(displayGroup);
            fill.a = selected ? 0.35f : 0.15f;
            Color wire = fill;
            wire.a = 0.95f;

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = fill;
            Gizmos.DrawCube(center, size);
            Gizmos.color = wire;
            Gizmos.DrawWireCube(center, size);
            Gizmos.matrix = Matrix4x4.identity;

#if UNITY_EDITOR
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10
            };
            style.normal.textColor = wire;
            string label = stackIndex == 0
                ? previewPieceType.ToString()
                : $"{previewPieceType} x{stackIndex + 1}";
            Handles.Label(transform.TransformPoint(center + Vector3.up * (diameter * 0.2f)), label, style);
#endif
        }

        private static Color GroupColor(CapturePotDisplayGroup group) => group switch
        {
            CapturePotDisplayGroup.Flowers => new Color(0.95f, 0.85f, 0.55f),
            _ => new Color(0.65f, 0.6f, 0.85f)
        };
    }
}
