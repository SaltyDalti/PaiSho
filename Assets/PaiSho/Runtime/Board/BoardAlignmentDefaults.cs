using UnityEngine;

namespace PaiSho.Board
{
    /// <summary>Player-tuned board alignment from board point tuner export (2026-07-03).</summary>
    public static class BoardAlignmentDefaults
    {
        public const float GridSpacingScale = 0.895176112651825f;
        public const float SpacingFineTune = 0.9954929351806641f;
        public const float TileHeight = 0f;
        public const float PointHeightOffset = 0.010704225860536099f;
        public const float BoardPointColliderScale = 0.8647887706756592f;

        public const float GridOffsetX = 0f;
        public const float GridOffsetZ = 0f;

        public const float BoardModelOffsetX = 0f;
        public const float BoardModelOffsetY = 0f;
        public const float BoardModelOffsetZ = 0f;

        public const float MarkerDiameterScale = 0.11999999731779099f;
        public const float MarkerHeightOffset = 0.08588027954101563f;
        public const float MarkerAlpha = 0.8500000238418579f;
        public static readonly Color MarkerColor = new Color(0.2f, 0.85f, 1f, 0.8500000238418579f);

        /// <summary>Cells south of the home gate to the player stand center.</summary>
        public const float PlayerStandSouthCells = 2.35f;

        /// <summary>Cells east of the home gate column to the player stand center.</summary>
        public const float PlayerStandEastCells = 2.5f;

        public static void ApplyTo(BoardLayout layout)
        {
            if (layout == null)
                return;

            layout.SetGridSpacingScale(GridSpacingScale);
            layout.SetTunerOverlay(SpacingFineTune, new Vector2(GridOffsetX, GridOffsetZ), PointHeightOffset);
            layout.SetGridYawDegrees(0f);
            layout.SetBoardPointColliderScale(BoardPointColliderScale);
            layout.SetBoardModelOffset(new Vector3(BoardModelOffsetX, BoardModelOffsetY, BoardModelOffsetZ));
        }

        public static void ApplyTo(BoardPointTunerSettings settings)
        {
            if (settings == null)
                return;

            settings.gridSpacingScale = GridSpacingScale;
            settings.spacingFineTune = SpacingFineTune;
            settings.gridOffsetX = GridOffsetX;
            settings.gridOffsetZ = GridOffsetZ;
            settings.gridYawDegrees = 0f;
            settings.tileHeight = TileHeight;
            settings.tileHeightOffset = PointHeightOffset;
            settings.colliderScale = BoardPointColliderScale;
            settings.boardModelOffsetX = BoardModelOffsetX;
            settings.boardModelOffsetY = BoardModelOffsetY;
            settings.boardModelOffsetZ = BoardModelOffsetZ;
            settings.markerDiameterScale = MarkerDiameterScale;
            settings.markerHeightOffset = MarkerHeightOffset;
            settings.markerAlpha = MarkerAlpha;
            settings.markerColor = MarkerColor;
            settings.showLabels = true;
            settings.colorByGarden = true;
            settings.SyncMarkerColorAlpha();
        }
    }
}
