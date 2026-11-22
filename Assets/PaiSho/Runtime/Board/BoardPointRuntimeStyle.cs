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
            // Diameter / height match BoardAlignmentDefaults (F8 export 2026-07-03) so WebGL
            // discs scale like the Desktop-tuned markers relative to CellSpacing (HJ-29).
            var s = new BoardPointTunerSettings
            {
                showMarkers = true,
                showLabels = false,
                colorByGarden = false,
                markerDiameterScale = BoardAlignmentDefaults.MarkerDiameterScale,
                markerHeightOffset = BoardAlignmentDefaults.MarkerHeightOffset,
                markerAlpha = 0.72f,
                markerColor = new Color(0.22f, 0.2f, 0.18f, 0.72f)
            };
            s.SyncMarkerColorAlpha();
            return s;
        }
    }
}
