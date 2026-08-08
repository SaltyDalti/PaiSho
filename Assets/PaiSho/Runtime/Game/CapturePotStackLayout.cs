using UnityEngine;
using PaiSho.Board;

namespace PaiSho.Game
{
    /// <summary>Default local offsets for capture stack markers (matches PotVisualManager math).</summary>
    public static class CapturePotStackLayout
    {
        public static Vector3 ComputeDefaultLocalOffset(BoardLayout layout, in CapturePotStackCatalog.SlotDefinition slot)
        {
            float spacing = ResolveSpacing(layout);
            float rowOffset = slot.DisplaySlot * spacing * CapturePotAlignmentDefaults.StackSpacingCells;
            float lift = slot.StackIndex * spacing * CapturePotAlignmentDefaults.StackLiftCells;
            return new Vector3(rowOffset, lift, 0f);
        }

        public static Vector3 ComputeWorldPosition(
            Transform potRoot,
            BoardLayout layout,
            CapturePotDisplayGroup group,
            int displaySlot,
            int stackIndex)
        {
            if (potRoot == null)
                return Vector3.zero;

            if (GameBoardSetup.TryGetCaptureStackMarker(potRoot, group, displaySlot, stackIndex, out Transform marker))
                return marker.position;

            float spacing = ResolveSpacing(layout);
            Transform slotRoot = FindSlotRoot(potRoot, group, displaySlot);
            if (slotRoot != null)
            {
                float lift = stackIndex * spacing * CapturePotAlignmentDefaults.StackLiftCells;
                return slotRoot.position + Vector3.up * lift;
            }

            float rowOffset = displaySlot * spacing * CapturePotAlignmentDefaults.StackSpacingCells;
            float fallbackLift = stackIndex * spacing * CapturePotAlignmentDefaults.StackLiftCells;
            return potRoot.position + potRoot.right * rowOffset + Vector3.up * fallbackLift;
        }

        private static Transform FindSlotRoot(Transform potRoot, CapturePotDisplayGroup group, int displaySlot)
        {
            string groupFolder = CapturePotDisplayOrder.GetGroupFolder(group);
            string slotFolder = $"{CapturePotDisplayOrder.SlotNamePrefix}{displaySlot}";
            Transform groupRoot = GameBoardSetup.FindDirectChild(potRoot, groupFolder);
            return groupRoot != null ? GameBoardSetup.FindDirectChild(groupRoot, slotFolder) : null;
        }

        private static float ResolveSpacing(BoardLayout layout)
        {
            if (layout == null)
                return 0.42f;

            return layout.CellSpacing * layout.SpacingFineTune;
        }
    }
}
