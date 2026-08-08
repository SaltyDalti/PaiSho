using System.Collections.Generic;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    /// <summary>
    /// Baked capture-pot markers: two groups x six display lanes x vertical stacks.
    /// Editor preview uses the priority type for each lane; runtime compacts occupied lanes.
    /// </summary>
    public static class CapturePotStackCatalog
    {
        public const string StackNamePrefix = "Stack_";
        public const string SampleTileName = "SampleTile";

        public readonly struct SlotDefinition
        {
            public readonly CapturePotDisplayGroup Group;
            public readonly int DisplaySlot;
            public readonly int StackIndex;
            public readonly PieceType PreviewPieceType;
            public readonly int GlobalSortIndex;

            public SlotDefinition(
                CapturePotDisplayGroup group,
                int displaySlot,
                int stackIndex,
                PieceType previewPieceType,
                int globalSortIndex)
            {
                Group = group;
                DisplaySlot = displaySlot;
                StackIndex = stackIndex;
                PreviewPieceType = previewPieceType;
                GlobalSortIndex = globalSortIndex;
            }

            public string GroupFolder => CapturePotDisplayOrder.GetGroupFolder(Group);
            public string SlotFolder => $"{CapturePotDisplayOrder.SlotNamePrefix}{DisplaySlot}";
            public string StackFolder => $"{StackNamePrefix}{StackIndex}";
        }

        public static int GetMaxStackCount(PieceType type) => CapturePotDisplayOrder.GetMaxStackCount(type);

        public static IReadOnlyList<SlotDefinition> GetAllSlots()
        {
            var slots = new List<SlotDefinition>(54);
            int globalIndex = 0;
            AppendGroup(slots, CapturePotDisplayGroup.Flowers, ref globalIndex);
            AppendGroup(slots, CapturePotDisplayGroup.SpecialAndOther, ref globalIndex);
            return slots;
        }

        public static bool TryGetSlot(
            CapturePotDisplayGroup group,
            int displaySlot,
            int stackIndex,
            out SlotDefinition slot)
        {
            foreach (SlotDefinition candidate in GetAllSlots())
            {
                if (candidate.Group == group &&
                    candidate.DisplaySlot == displaySlot &&
                    candidate.StackIndex == stackIndex)
                {
                    slot = candidate;
                    return true;
                }
            }

            slot = default;
            return false;
        }

        public static int CountSlots() => GetAllSlots().Count;

        private static void AppendGroup(
            List<SlotDefinition> slots,
            CapturePotDisplayGroup group,
            ref int globalIndex)
        {
            for (int displaySlot = 0; displaySlot < CapturePotDisplayOrder.SlotsPerGroup; displaySlot++)
            {
                PieceType previewType = CapturePotDisplayOrder.GetPreviewTypeForDisplaySlot(group, displaySlot);
                int maxStack = GetMaxStackCount(previewType);
                for (int stackIndex = 0; stackIndex < maxStack; stackIndex++)
                {
                    slots.Add(new SlotDefinition(group, displaySlot, stackIndex, previewType, globalIndex));
                    globalIndex++;
                }
            }
        }
    }
}
