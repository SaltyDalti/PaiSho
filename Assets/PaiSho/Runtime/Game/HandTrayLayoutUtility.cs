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
                slotTransform.localRotation = Quaternion.Euler(HandTrayAlignmentDefaults.GetSlotEuler(i, player));

                var marker = slot.AddComponent<HandTraySlotMarker>();
                marker.Configure(i, player);
            }

            return trayRoot;
        }

        /// <summary>Seat a runtime hand tile on a baked slot marker, matching SampleTile when present.</summary>
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

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL destroys SampleTiles at boot (Safari). Seat from authored Slot_* locals +
            // HandTrayAlignmentDefaults euler, scaled to CellSpacing like board tiles — not
            // inactive SampleTile lossyScale (authored under stand scale 0.5) (HJ-32).
            SeatFromAuthoredSlot(visual, slotMarker, tilesRoot, cellSpacing, owner, slotIndex);
#else
            Transform sample = FindSampleTile(slotMarker);
            if (sample != null)
            {
                BoardSideLayoutUtility.ApplySampleTilePose(visual.transform, sample, tilesRoot);
                sample.gameObject.SetActive(true);
                foreach (Renderer renderer in sample.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer != null)
                        renderer.enabled = false;
                }
            }
            else
            {
                SeatFromAuthoredSlot(visual, slotMarker, tilesRoot, cellSpacing, owner, slotIndex);
            }
#endif

            WoodTheme.EnsureMeshLighting(visual);
            PieceMaterialUtility.ApplyPieceTheme(visual, pieceType, owner);
            BoardSideLayoutUtility.EnsureRuntimeTileVisible(visual, pieceType);
        }

        private static void SeatFromAuthoredSlot(
            GameObject visual,
            Transform slotMarker,
            Transform tilesRoot,
            float cellSpacing,
            Player owner,
            int slotIndex)
        {
            visual.transform.SetParent(tilesRoot, false);
            visual.transform.localScale = Vector3.one;

            // Scene Slot_* often have identity local euler; tilt lives in SampleTile / defaults.
            Quaternion authoredTilt = Quaternion.Euler(HandTrayAlignmentDefaults.GetSlotEuler(slotIndex, owner));
            bool slotHasAuthoredTilt = slotMarker.localEulerAngles.sqrMagnitude > 0.01f;
            Quaternion worldRotation = slotHasAuthoredTilt
                ? slotMarker.rotation
                : slotMarker.rotation * authoredTilt;

            visual.transform.SetPositionAndRotation(slotMarker.position, worldRotation);
            WoodTheme.FitPrefabScaleOnly(visual, cellSpacing);
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
