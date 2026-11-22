using UnityEngine;

namespace PaiSho.Board
{
    /// <summary>
    /// Gameplay board-point style. Resources <c>Board.glb</c> omits BoardLines, so
    /// intersection discs draw the grid (see WoodTheme.ValidateBoardPrefab). Colliders
    /// stay active either way.
    /// </summary>
    public static class BoardPointRuntimeStyle
    {
        private static BoardPointTunerSettings gameplay;

        public static BoardPointTunerSettings Gameplay => gameplay ??= CreateGameplay();

        private static BoardPointTunerSettings CreateGameplay()
        {
            // Diameter tracks F8 BoardAlignmentDefaults. Height stays a thin disc lift — the
            // tuner MarkerHeightOffset (~0.086) is for editor spheres and floated WebGL discs
            // off the wood after #23 (HJ-30).
            var s = new BoardPointTunerSettings
            {
                showMarkers = true,
                showLabels = false,
                colorByGarden = false,
                markerDiameterScale = BoardAlignmentDefaults.MarkerDiameterScale,
                markerHeightOffset = 0.018f,
                markerAlpha = 0.72f,
                markerColor = new Color(0.22f, 0.2f, 0.18f, 0.72f)
            };
            s.SyncMarkerColorAlpha();
            return s;
        }
    }
}
