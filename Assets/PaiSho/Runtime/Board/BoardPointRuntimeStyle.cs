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
            // discs are the only grid. Prior thin dark wood-tone discs (HJ-30) were too easy to
            // miss on WebGL — use F8 cyan + larger diameter/lift so Dalton can see points before
            // pitch tuning. showMarkers must stay true.
            var s = new BoardPointTunerSettings
            {
                showMarkers = true,
                showLabels = false,
                colorByGarden = false,
                markerDiameterScale = 0.32f,
                markerHeightOffset = 0.05f,
                markerAlpha = 0.92f,
                markerColor = BoardAlignmentDefaults.MarkerColor
            };
            s.SyncMarkerColorAlpha();
            return s;
        }
    }
}
