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
            // Visibility-first (HJ-53 / 0.1.25): Resources Board.glb has no BoardLines, so these
            // discs are the only grid. F8 export 2026-11-23 uses diameter≈0.089; gameplay keeps a
            // larger cyan disc so Dalton can verify points. showMarkers must stay true.
            var s = new BoardPointTunerSettings
            {
                showMarkers = true,
                showLabels = false,
                colorByGarden = false,
                markerDiameterScale = 0.32f,
                markerHeightOffset = BoardAlignmentDefaults.MarkerHeightOffset,
                markerAlpha = 0.92f,
                markerColor = BoardAlignmentDefaults.MarkerColor
            };
            s.SyncMarkerColorAlpha();
            return s;
        }
    }
}
