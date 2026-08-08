using UnityEngine;
using PaiSho;
using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    /// <summary>Positions capture pots off the west edge of the grid, mirroring hand-tray stand placement.</summary>
    public static class CapturePotLayoutUtility
    {
        public const string HostTilesRootName = "HostCapturePotTiles";
        public const string OpponentTilesRootName = "OpponentCapturePotTiles";

        public static bool TryGetBoardAxes(BoardLayout layout, out Vector3 towardWest, out Vector3 boardAxis) =>
            BoardSideLayoutUtility.TryGetBoardAxes(layout, out towardWest, out boardAxis);

        public static Vector3 ComputeDefaultAnchor(BoardLayout layout, Player player)
        {
            if (layout == null)
                return Vector3.zero;

            if (!TryGetBoardAxes(layout, out Vector3 towardWest, out Vector3 boardAxis))
                return layout.Origin.position;

            float spacing = layout.CellSpacing * layout.SpacingFineTune;
            Vector3 westGate = layout.CoordinateToWorld(BoardUtils.WestGate);
            float alongCells = player == Player.Host
                ? CapturePotAlignmentDefaults.HostAlongCells
                : CapturePotAlignmentDefaults.OpponentAlongCells;

            Vector3 anchor = westGate
                + towardWest * (spacing * CapturePotAlignmentDefaults.WestCells)
                + boardAxis * (spacing * alongCells);

            anchor.y = layout.TileHeight + layout.PieceSurfaceLift + CapturePotAlignmentDefaults.SurfaceLift;
            return anchor;
        }

        public static Quaternion ComputeFacing(BoardLayout layout)
        {
            if (layout == null || !TryGetBoardAxes(layout, out Vector3 towardWest, out _))
                return Quaternion.identity;

            return Quaternion.LookRotation(-towardWest, Vector3.up);
        }

        public static void ApplyAnchor(Transform root, BoardLayout layout, Player player, bool preserveScenePosition)
        {
            if (root == null || layout == null)
                return;

            if (!preserveScenePosition || root.parent == null)
                root.rotation = ComputeFacing(layout);

            if (preserveScenePosition && root.parent != null)
                return;

            root.position = ComputeDefaultAnchor(layout, player);
        }

        /// <summary>
        /// Runtime captured tiles live on the board root (same as hand trays), not under the rotated pot anchor.
        /// </summary>
        public static Transform EnsureTilesRoot(Transform boardRoot, Player player)
        {
            if (boardRoot == null)
                return null;

            string tilesRootName = player == Player.Host ? HostTilesRootName : OpponentTilesRootName;
            Transform existing = boardRoot.Find(tilesRootName);
            if (existing != null)
                return existing;

            var tilesRootObject = new GameObject(tilesRootName);
            Transform tilesRoot = tilesRootObject.transform;
            tilesRoot.SetParent(boardRoot, false);
            tilesRoot.localPosition = Vector3.zero;
            tilesRoot.localRotation = Quaternion.identity;
            tilesRoot.localScale = Vector3.one;
            return tilesRoot;
        }

        /// <summary>Seat a runtime captured tile using baked SampleTile world pose (hand-tray pattern).</summary>
        public static void ApplyRuntimeTileToMarker(
            GameObject visual,
            Transform stackMarker,
            Transform tilesRoot,
            float cellSpacing,
            PieceType pieceType,
            int stackLiftDelta = 0)
        {
            if (visual == null || stackMarker == null || tilesRoot == null)
                return;

            visual.SetActive(true);
            visual.transform.SetParent(tilesRoot, true);
            visual.transform.localScale = Vector3.one;

            Transform sample = CapturePotSampleTile.FindTransform(stackMarker);
            float liftStep = stackLiftDelta > 0
                ? ResolveStackWorldStep(stackMarker.parent, cellSpacing) * stackLiftDelta
                : 0f;

            if (sample != null)
            {
                ApplySampleTilePose(visual.transform, sample, tilesRoot);
                // Hide baked preview so it doesn't look like a second captured tile.
                sample.gameObject.SetActive(false);
                if (liftStep > 0f)
                    visual.transform.position += Vector3.up * liftStep;
            }
            else
            {
                WoodTheme.FitPrefabToCellSpacing(visual, cellSpacing, alignBottomToSurface: false);
                visual.transform.SetPositionAndRotation(stackMarker.position, stackMarker.rotation);
                visual.transform.localScale *= CapturePotAlignmentDefaults.StackTileScale;
                if (liftStep > 0f)
                    visual.transform.position += Vector3.up * liftStep;
            }

            EnsureRuntimeTileVisible(visual, pieceType);
        }

        public static void FinalizeRuntimeTile(Piece piece, float cellSpacing)
        {
            if (piece == null)
                return;

            EnsureRuntimeTileVisible(piece.gameObject, piece.Type);
            WoodTheme.EnsurePiecePickCollider(piece.gameObject, cellSpacing);
        }

        public static float ResolveStackStepForPreview(Transform slotRoot, float cellSpacing) =>
            ResolveStackWorldStep(slotRoot, cellSpacing);

        private static void ApplySampleTilePose(Transform visual, Transform sample, Transform tilesRoot) =>
            BoardSideLayoutUtility.ApplySampleTilePose(visual, sample, tilesRoot);

        private static void EnsureRuntimeTileVisible(GameObject visual, PieceType pieceType) =>
            BoardSideLayoutUtility.EnsureRuntimeTileVisible(visual, pieceType);

        private static float ResolveStackWorldStep(Transform slotRoot, float cellSpacing)
        {
            if (slotRoot == null)
                return cellSpacing * CapturePotAlignmentDefaults.StackLiftCells;

            Transform stack0 = GameBoardSetup.FindDirectChild(slotRoot, $"{CapturePotStackCatalog.StackNamePrefix}0");
            Transform stack1 = GameBoardSetup.FindDirectChild(slotRoot, $"{CapturePotStackCatalog.StackNamePrefix}1");
            if (stack0 != null && stack1 != null)
                return stack1.position.y - stack0.position.y;

            return cellSpacing * CapturePotAlignmentDefaults.StackLiftCells;
        }
    }
}
