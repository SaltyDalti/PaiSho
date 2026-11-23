using UnityEngine;
using PaiSho;
using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    /// <summary>Shared hand-tray placement used by runtime and the board-setup baker.</summary>
    public static class HandTrayLayoutUtility
    {
        public static Transform CreateBakedTray(
            Transform standRoot,
            BoardLayout layout,
            Player player,
            string trayName,
            bool bakeSlotMarkers)
        {
            if (standRoot == null || layout == null)
                return null;

            // Authored HandTray* lives under the stand root (GamePlay / GameBoardSetup prefab).
            var trayObject = new GameObject(trayName);
            Transform trayRoot = trayObject.transform;
            trayRoot.SetParent(standRoot, false);
            trayRoot.localPosition = HandTrayAlignmentDefaults.GetAuthoredTrayLocalPosition(player);
            trayRoot.localRotation = Quaternion.Euler(HandTrayAlignmentDefaults.GetAuthoredTrayLocalEuler(player));
            trayRoot.localScale = HandTrayAlignmentDefaults.AuthoredTrayLocalScale;

            if (!bakeSlotMarkers)
                return trayRoot;

            for (int i = 0; i < HandTrayAlignmentDefaults.MaxSlots; i++)
            {
                var slot = new GameObject($"{GameBoardSetup.SlotNamePrefix}{i}");
                Transform slotTransform = slot.transform;
                slotTransform.SetParent(trayRoot, false);
                slotTransform.localPosition = HandTrayAlignmentDefaults.GetSlotPosition(i, player);
                // Scene Slot_* use identity local euler; SampleTile carries the seat pose.
                slotTransform.localRotation = Quaternion.identity;

                var marker = slot.AddComponent<HandTraySlotMarker>();
                marker.Configure(i, player);
                EnsureSamplePoseAnchor(slotTransform, player, i);
            }

            return trayRoot;
        }

        /// <summary>
        /// Seat a runtime hand tile on a baked slot marker, matching authored SampleTile pose
        /// (Desktop C# path: <see cref="BoardSideLayoutUtility.ApplySampleTilePose"/>).
        /// </summary>
        public static void ApplyRuntimeTileToSlot(
            GameObject visual,
            Transform slotMarker,
            Transform tilesRoot,
            float cellSpacing,
            PieceType pieceType)
        {
            if (visual == null || slotMarker == null || tilesRoot == null)
                return;

            Player owner = Player.Host;
            int slotIndex = 0;
            if (visual.TryGetComponent(out Piece piece))
                owner = piece.Owner;
            if (slotMarker.TryGetComponent(out HandTraySlotMarker marker))
            {
                owner = marker.Owner;
                slotIndex = marker.SlotIndex;
            }

            // Faithful C# seating under authored stand scale 0.5: SampleTile world pose + lossy
            // scale, parented under tilesRoot. WebGL keeps empty pose anchors (bake/boot strip)
            // so Safari never skins SampleTile meshes. cellSpacing unused — Desktop SampleTile
            // scale is the rack size once the stand matches GamePlay's 0.5 (HJ-39).
            _ = cellSpacing;
            Transform sample = EnsureSamplePoseAnchor(slotMarker, owner, slotIndex);
            BoardSideLayoutUtility.ApplySampleTilePose(visual.transform, sample, tilesRoot);

#if !UNITY_WEBGL || UNITY_EDITOR
            sample.gameObject.SetActive(true);
            foreach (Renderer renderer in sample.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null)
                    renderer.enabled = false;
            }
#endif

            WoodTheme.EnsureMeshLighting(visual);
            PieceMaterialUtility.ApplyPieceTheme(visual, pieceType, owner);
            BoardSideLayoutUtility.EnsureRuntimeTileVisible(visual, pieceType);
        }

        /// <summary>
        /// Empty SampleTile transform with authored local TRS. Meshes optional (Desktop);
        /// WebGL strips meshes but keeps this anchor for <see cref="ApplySampleTilePose"/>.
        /// </summary>
        public static Transform EnsureSamplePoseAnchor(Transform slotMarker, Player owner, int slotIndex)
        {
            Transform sample = FindSampleTile(slotMarker);
            if (sample != null)
                return sample;

            var go = new GameObject(HandTraySlotMarker.SampleTileName);
            sample = go.transform;
            sample.SetParent(slotMarker, false);
            ApplyAuthoredSampleLocals(sample, owner, slotIndex);
            return sample;
        }

        public static void ApplyAuthoredSampleLocals(Transform sample, Player owner, int slotIndex)
        {
            if (sample == null)
                return;

            sample.localPosition = HandTrayAlignmentDefaults.GetSampleTileLocalPosition(slotIndex, owner);
            sample.localRotation = Quaternion.Euler(
                HandTrayAlignmentDefaults.GetSampleTileLocalEuler(slotIndex, owner));
            sample.localScale = HandTrayAlignmentDefaults.AuthoredSampleTileLocalScale;
        }

        /// <summary>
        /// Remove mesh/collider/skin weight from a SampleTile while keeping the transform pose.
        /// Used on WebGL so Safari never deserializes/skins authored piece meshes at callMain.
        /// </summary>
        public static void HollowSampleTile(Transform sample)
        {
            if (sample == null)
                return;

            // Destroy nested mesh children first (piece prefab hierarchy under SampleTile).
            for (int i = sample.childCount - 1; i >= 0; i--)
            {
                Transform child = sample.GetChild(i);
                if (child != null)
                    DestroyUnityObject(child.gameObject);
            }

            StripComponents(sample.gameObject);
            sample.gameObject.name = HandTraySlotMarker.SampleTileName;
        }

        public static bool HasRenderableMesh(Transform root)
        {
            if (root == null)
                return false;

            return root.GetComponentInChildren<MeshFilter>(true) != null
                || root.GetComponentInChildren<SkinnedMeshRenderer>(true) != null
                || root.GetComponentInChildren<MeshRenderer>(true) != null;
        }

        private static void StripComponents(GameObject root)
        {
            if (root == null)
                return;

            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (collider != null)
                    DestroyUnityObject(collider);
            }

            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter != null)
                    DestroyUnityObject(filter);
            }

            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer != null)
                    DestroyUnityObject(renderer);
            }

            foreach (SkinnedMeshRenderer skinned in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (skinned != null)
                    DestroyUnityObject(skinned);
            }
        }

        private static void DestroyUnityObject(Object obj)
        {
            if (obj == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(obj);
                return;
            }
#endif
            Object.Destroy(obj);
        }

        private static Transform FindSampleTile(Transform slotMarker)
        {
            if (slotMarker == null)
                return null;

            Transform sample = slotMarker.Find(HandTraySlotMarker.SampleTileName);
            if (sample != null)
                return sample;

            var sampleMarker = slotMarker.GetComponentInChildren<HandTraySlotSampleTile>(true);
            if (sampleMarker != null)
                return sampleMarker.transform;

            foreach (Transform child in slotMarker.GetComponentsInChildren<Transform>(true))
            {
                if (child != null && child != slotMarker && child.name == HandTraySlotMarker.SampleTileName)
                    return child;
            }

            return null;
        }
    }
}
