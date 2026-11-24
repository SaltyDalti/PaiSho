using UnityEngine;

namespace PaiSho.Board
{
    /// <summary>Player-tuned board alignment from board point tuner export (2026-11-23).</summary>
    public static class BoardAlignmentDefaults
    {
        public const float GridSpacingScale = 1.0067100524902344f;
        public const float SpacingFineTune = 1.0010299682617188f;
        public const float TileHeight = 0.13846099376678468f;
        public const float PointHeightOffset = 0.019319599494338037f;
        public const float BoardPointColliderScale = 0.8440200090408325f;

        public const float GridOffsetX = 0f;
        public const float GridOffsetZ = 0f;

        public const float BoardModelOffsetX = 0f;
        public const float BoardModelOffsetY = 0f;
        public const float BoardModelOffsetZ = 0f;

        public const float MarkerDiameterScale = 0.08900000154972077f;
        public const float MarkerHeightOffset = 0.08588030189275742f;
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
            layout.SetBoardSurfaceHeight(TileHeight);
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
            settings.showMarkers = true;
            settings.showLabels = true;
            settings.colorByGarden = true;
            settings.SyncMarkerColorAlpha();
        }
    }
}
