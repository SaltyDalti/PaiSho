using UnityEngine;
using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    /// <summary>
    /// Shared board-axis math for off-board seats (capture pots today; trays may adopt later).
    /// </summary>
    public static class BoardSideLayoutUtility
    {
        public static bool TryGetBoardAxes(BoardLayout layout, out Vector3 towardWest, out Vector3 boardAxis)
        {
            towardWest = Vector3.left;
            boardAxis = Vector3.forward;

            if (layout == null)
                return false;

            Vector3 westGate = layout.CoordinateToWorld(BoardUtils.WestGate);
            Vector3 middle = layout.CoordinateToWorld(BoardUtils.MiddleGate);
            towardWest = westGate - middle;
            towardWest.y = 0f;
            if (towardWest.sqrMagnitude < 0.0001f)
                towardWest = layout.Origin.right;
            towardWest.Normalize();

            Vector3 southGate = layout.CoordinateToWorld(BoardUtils.SouthGate);
            Vector3 northGate = layout.CoordinateToWorld(BoardUtils.NorthGate);
            boardAxis = northGate - southGate;
            boardAxis.y = 0f;
            if (boardAxis.sqrMagnitude < 0.0001f)
                boardAxis = layout.Origin.forward;
            boardAxis.Normalize();

            return true;
        }

        /// <summary>Match a visual to a baked sample's world pose while parenting under <paramref name="tilesRoot"/>.</summary>
        public static void ApplySampleTilePose(Transform visual, Transform sample, Transform tilesRoot)
        {
            if (visual == null || sample == null || tilesRoot == null)
                return;

            Vector3 parentLossy = tilesRoot.lossyScale;
            Vector3 sampleLossy = sample.lossyScale;
            visual.SetPositionAndRotation(sample.position, sample.rotation);
            visual.localScale = new Vector3(
                sampleLossy.x / Mathf.Max(parentLossy.x, 0.0001f),
                sampleLossy.y / Mathf.Max(parentLossy.y, 0.0001f),
                sampleLossy.z / Mathf.Max(parentLossy.z, 0.0001f));
        }

        public static void EnsureRuntimeTileVisible(GameObject visual, PieceType pieceType)
        {
            if (visual == null)
                return;

            visual.SetActive(true);
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = true;

            if (visual.TryGetComponent(out Piece piece))
                PieceMaterialUtility.ApplyPieceTheme(visual, pieceType, piece.Owner);
        }
    }
}
