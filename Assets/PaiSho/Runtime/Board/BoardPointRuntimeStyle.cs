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
            // Product default: markers hidden in normal play (colliders stay active). F8
            // BoardPointTuner can still force Show markers for alignment. Size/color retained
            // for that debug path; pitch/spacing live in BoardAlignmentDefaults only.
            var s = new BoardPointTunerSettings
            {
                showMarkers = false,
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
