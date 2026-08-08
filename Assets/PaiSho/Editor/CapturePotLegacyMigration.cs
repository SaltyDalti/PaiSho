#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using PaiSho.Game;
using PaiSho.Pieces;

namespace PaiSho.EditorTools
{
    /// <summary>
    /// Renames legacy per-type folders (01_WhiteFlowers/Jasmine/...) to display lanes
    /// (01_Flowers/Slot_0/...) without moving stack positions.
    /// </summary>
    public static class CapturePotLegacyMigration
    {
        private static readonly string[] LegacyCategoryFolders =
        {
            "01_WhiteFlowers",
            "02_RedFlowers",
            "03_SpecialFlowers",
            "04_Other"
        };

        private static readonly (PieceType Type, CapturePotDisplayGroup Group, int DisplaySlot)[] TypeToDisplaySlot =
        {
            (PieceType.Jasmine, CapturePotDisplayGroup.Flowers, 0),
            (PieceType.Lily, CapturePotDisplayGroup.Flowers, 1),
            (PieceType.Jade, CapturePotDisplayGroup.Flowers, 2),
            (PieceType.Rose, CapturePotDisplayGroup.Flowers, 3),
            (PieceType.Chrysanthemum, CapturePotDisplayGroup.Flowers, 4),
            (PieceType.Rhododendron, CapturePotDisplayGroup.Flowers, 5),
            (PieceType.Lotus, CapturePotDisplayGroup.SpecialAndOther, 0),
            (PieceType.Orchid, CapturePotDisplayGroup.SpecialAndOther, 1),
            (PieceType.Boat, CapturePotDisplayGroup.SpecialAndOther, 2),
            (PieceType.Wheel, CapturePotDisplayGroup.SpecialAndOther, 3),
            (PieceType.Knotweed, CapturePotDisplayGroup.SpecialAndOther, 4),
            (PieceType.Rock, CapturePotDisplayGroup.SpecialAndOther, 5)
        };

        public static bool HasLegacyHierarchy(Transform potRoot)
        {
            if (potRoot == null)
                return false;

            foreach (string category in LegacyCategoryFolders)
            {
                if (potRoot.Find(category) != null)
                    return true;
            }

            return false;
        }

        public static int MigratePot(Transform potRoot, Player owner)
        {
            if (potRoot == null)
                return 0;

            Undo.RegisterFullObjectHierarchyUndo(potRoot.gameObject, "Migrate Capture Pot To Display Slots");
            int moved = 0;

            foreach ((PieceType type, CapturePotDisplayGroup group, int displaySlot) in TypeToDisplaySlot)
            {
                if (TryMigrateTypeFolder(potRoot, owner, type, group, displaySlot))
                    moved++;
            }

            RemoveEmptyLegacyCategories(potRoot);
            return moved;
        }

        private static bool TryMigrateTypeFolder(
            Transform potRoot,
            Player owner,
            PieceType type,
            CapturePotDisplayGroup group,
            int displaySlot)
        {
            string slotName = $"{CapturePotDisplayOrder.SlotNamePrefix}{displaySlot}";
            string groupFolderName = CapturePotDisplayOrder.GetGroupFolder(group);
            Transform groupFolder = potRoot.Find(groupFolderName);
            if (groupFolder == null)
            {
                var groupObject = new GameObject(groupFolderName);
                groupFolder = groupObject.transform;
                Undo.RegisterCreatedObjectUndo(groupObject, "Migrate Capture Pot");
                groupFolder.SetParent(potRoot, false);
                groupFolder.localPosition = Vector3.zero;
                groupFolder.localRotation = Quaternion.identity;
                groupFolder.localScale = Vector3.one;
            }

            Transform existingSlot = groupFolder.Find(slotName);
            Transform typeFolder = FindLegacyTypeFolder(potRoot, type);
            if (typeFolder == null)
            {
                if (existingSlot != null)
                    UpdateMarkersUnderSlot(existingSlot, owner, group, displaySlot);

                return existingSlot != null;
            }

            if (existingSlot != null && existingSlot != typeFolder)
            {
                Debug.LogWarning(
                    $"Capture pot migration skipped {type}: {slotName} already exists under {groupFolderName}.");
                return false;
            }

            Undo.RecordObject(typeFolder, "Migrate Capture Pot");
            typeFolder.name = slotName;
            typeFolder.SetParent(groupFolder, true);
            UpdateMarkersUnderSlot(typeFolder, owner, group, displaySlot);
            return true;
        }

        private static Transform FindLegacyTypeFolder(Transform potRoot, PieceType type)
        {
            string typeName = type.ToString();
            foreach (Transform transform in potRoot.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name != typeName)
                    continue;

                Transform parent = transform.parent;
                if (parent == null || !IsLegacyCategoryFolder(parent.name))
                    continue;

                return transform;
            }

            return null;
        }

        private static bool IsLegacyCategoryFolder(string folderName)
        {
            foreach (string legacy in LegacyCategoryFolders)
            {
                if (folderName == legacy)
                    return true;
            }

            return false;
        }

        private static void UpdateMarkersUnderSlot(
            Transform slotFolder,
            Player owner,
            CapturePotDisplayGroup group,
            int displaySlot)
        {
            foreach (Transform stack in slotFolder)
            {
                if (!stack.name.StartsWith(CapturePotStackCatalog.StackNamePrefix, System.StringComparison.Ordinal))
                    continue;

                if (!int.TryParse(
                        stack.name.Substring(CapturePotStackCatalog.StackNamePrefix.Length),
                        out int stackIndex))
                {
                    continue;
                }

                if (!CapturePotStackCatalog.TryGetSlot(group, displaySlot, stackIndex, out CapturePotStackCatalog.SlotDefinition slot))
                    continue;

                CapturePotSlotMarker marker = stack.GetComponent<CapturePotSlotMarker>();
                if (marker == null)
                    marker = Undo.AddComponent<CapturePotSlotMarker>(stack.gameObject);

                marker.Configure(owner, slot);
            }
        }

        private static void RemoveEmptyLegacyCategories(Transform potRoot)
        {
            foreach (string category in LegacyCategoryFolders)
            {
                Transform legacy = potRoot.Find(category);
                if (legacy == null || legacy.childCount > 0)
                    continue;

                Undo.DestroyObjectImmediate(legacy.gameObject);
            }
        }
    }
}
#endif
