using UnityEngine;

namespace PaiSho.Game
{
    /// <summary>Default capture-pot placement west of the grid (tunable like hand trays).</summary>
    public static class CapturePotAlignmentDefaults
    {
        /// <summary>Cells past the west gate toward open table space.</summary>
        public const float WestCells = 4.25f;

        /// <summary>Cells along the board axis from west gate (negative = toward host / south).</summary>
        public const float HostAlongCells = -2.75f;

        /// <summary>Cells along the board axis from west gate (positive = toward opponent / north).</summary>
        public const float OpponentAlongCells = 2.75f;

        /// <summary>Extra world-space lift applied after the board surface height.</summary>
        public const float SurfaceLift = 0.02f;

        /// <summary>Scale multiplier after FitPrefabToCellSpacing (pot stacks stay compact).</summary>
        public const float StackTileScale = 0.76f;

        /// <summary>Horizontal spacing between stacked groups, as a fraction of cell spacing.</summary>
        public const float StackSpacingCells = 0.34f;

        /// <summary>Vertical lift per duplicate type in the stack, as a fraction of cell spacing.</summary>
        public const float StackLiftCells = 0.07f;
    }
}
