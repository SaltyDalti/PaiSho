using System.Collections.Generic;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public enum CapturePotDisplayGroup
    {
        Flowers = 0,
        SpecialAndOther = 1
    }

    /// <summary>
    /// Capture-pot display priority. Tiles compact upward when higher-priority types are absent.
    /// </summary>
    public static class CapturePotDisplayOrder
    {
        public const string FlowersFolder = "01_Flowers";
        public const string SpecialFolder = "02_SpecialAndOther";
        public const string SlotNamePrefix = "Slot_";

        public const int SlotsPerGroup = 6;

        private static readonly PieceType[] FlowerOrder =
        {
            PieceType.Jasmine,
            PieceType.Lily,
            PieceType.Jade,
            PieceType.Rose,
            PieceType.Chrysanthemum,
            PieceType.Rhododendron
        };

        private static readonly PieceType[] SpecialOrder =
        {
            PieceType.Lotus,
            PieceType.Orchid,
            PieceType.Boat,
            PieceType.Wheel,
            PieceType.Knotweed,
            PieceType.Rock
        };

        public static IReadOnlyList<PieceType> GetPriorityOrder(CapturePotDisplayGroup group) =>
            group == CapturePotDisplayGroup.Flowers ? FlowerOrder : SpecialOrder;

        public static string GetGroupFolder(CapturePotDisplayGroup group) =>
            group == CapturePotDisplayGroup.Flowers ? FlowersFolder : SpecialFolder;

        public static CapturePotDisplayGroup GetGroup(PieceType type)
        {
            if (PieceRules.IsWhiteFlower(type) || PieceRules.IsRedFlower(type))
                return CapturePotDisplayGroup.Flowers;

            return CapturePotDisplayGroup.SpecialAndOther;
        }

        public static int GetPriorityIndex(PieceType type)
        {
            CapturePotDisplayGroup group = GetGroup(type);
            PieceType[] order = group == CapturePotDisplayGroup.Flowers ? FlowerOrder : SpecialOrder;
            for (int i = 0; i < order.Length; i++)
            {
                if (order[i] == type)
                    return i;
            }

            return int.MaxValue;
        }

        public static PieceType GetPreviewTypeForDisplaySlot(CapturePotDisplayGroup group, int displaySlot)
        {
            PieceType[] order = group == CapturePotDisplayGroup.Flowers ? FlowerOrder : SpecialOrder;
            int index = UnityEngine.Mathf.Clamp(displaySlot, 0, order.Length - 1);
            return order[index];
        }

        public static int GetMaxStackCount(PieceType type) =>
            PieceRules.IsBasicFlower(type) ? 6 : 3;

        public static int ComparePieceTypes(PieceType a, PieceType b)
        {
            int group = GetGroup(a).CompareTo(GetGroup(b));
            if (group != 0)
                return group;

            return GetPriorityIndex(a).CompareTo(GetPriorityIndex(b));
        }

        /// <summary>
        /// Compact row index among captured types in the same group (gaps collapse upward).
        /// </summary>
        public static int GetCompactDisplaySlot(PieceType type, HashSet<PieceType> capturedTypesInGroup)
        {
            if (capturedTypesInGroup == null || !capturedTypesInGroup.Contains(type))
                return -1;

            CapturePotDisplayGroup group = GetGroup(type);
            int slot = 0;
            foreach (PieceType priorityType in GetPriorityOrder(group))
            {
                if (!capturedTypesInGroup.Contains(priorityType))
                    continue;

                if (priorityType == type)
                    return slot;

                slot++;
            }

            return -1;
        }
    }
}
