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

            if (!WoodTheme.TryGetPlayerStandAnchor(standRoot.gameObject, player, out WoodTheme.PlayerStandAnchor anchor))
                return null;

            Transform mesh = anchor.StandTransform;
            Vector3 trayOffset = HandTrayAlignmentDefaults.GetTrayLocalOffset(player);
            Vector3 trayEuler = HandTrayAlignmentDefaults.GetTrayLocalEuler(player);
            Bounds localBounds = anchor.LocalBounds;

            var trayObject = new GameObject(trayName);
            Transform trayRoot = trayObject.transform;
            trayRoot.SetParent(mesh, false);
            trayRoot.localPosition = new Vector3(
                localBounds.center.x + trayOffset.x,
                localBounds.max.y + layout.PieceSurfaceLift + 0.01f + trayOffset.y,
                localBounds.center.z + trayOffset.z);
            trayRoot.localRotation = Quaternion.Euler(trayEuler);

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

            Transform sample = FindSampleTile(slotMarker);

            Vector3 worldPosition = slotMarker.position;
            Quaternion worldRotation = slotMarker.rotation;

            if (sample != null)
            {
                worldPosition = sample.position;
                worldRotation = sample.rotation;
            }

            visual.transform.SetParent(tilesRoot, false);
            visual.transform.localScale = Vector3.one;
            visual.transform.SetPositionAndRotation(worldPosition, worldRotation);

            if (sample != null)
            {
                WoodTheme.MatchPrefabFootprintToReference(visual, sample.gameObject);
                visual.transform.SetPositionAndRotation(worldPosition, worldRotation);
                WoodTheme.AlignPrefabBottomToReference(visual, sample.gameObject);
                visual.transform.position = new Vector3(
                    worldPosition.x,
                    visual.transform.position.y,
                    worldPosition.z);
                visual.transform.rotation = worldRotation;
            }
            else
            {
                WoodTheme.FitPrefabScaleOnly(visual, cellSpacing);
                visual.transform.SetPositionAndRotation(worldPosition, worldRotation);
            }

            WoodTheme.EnsureMeshLighting(visual);
            BoardSideLayoutUtility.EnsureRuntimeTileVisible(visual, pieceType);
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
