using PaiSho.Pieces;

namespace PaiSho.Game
{
    /// <summary>Maps hand-tray slot indices to the literal piece prefabs used for editor placement guides.</summary>
    public static class HandTraySlotSampleTiles
    {
        public static PieceType GetPieceTypeForSlot(int slotIndex) =>
            (PieceType)UnityEngine.Mathf.Clamp(slotIndex, 0, HandTrayAlignmentDefaults.MaxSlots - 1);

        public static string GetPrefabAssetPath(PieceType type) => type switch
        {
            PieceType.Jasmine => "Assets/Prefabs/Pieces/Tile_Jasmine.prefab",
            PieceType.Rose => "Assets/Prefabs/Pieces/Tile_Rose.prefab",
            PieceType.Lily => "Assets/Prefabs/Pieces/Tile_Lily.prefab",
            PieceType.Jade => "Assets/Prefabs/Pieces/Tile_Jade.prefab",
            PieceType.Chrysanthemum => "Assets/Prefabs/Pieces/Tile_Chrys.prefab",
            PieceType.Rhododendron => "Assets/Prefabs/Pieces/Tile_Rhod.prefab",
            PieceType.Boat => "Assets/Prefabs/Pieces/Tile_Boat.prefab",
            PieceType.Rock => "Assets/Prefabs/Pieces/Tile_Rock.prefab",
            PieceType.Knotweed => "Assets/Prefabs/Pieces/Tile_Knot.prefab",
            PieceType.Wheel => "Assets/Prefabs/Pieces/Tile_Wheel.prefab",
            PieceType.Lotus => "Assets/Prefabs/Pieces/Tile_Lotus.prefab",
            PieceType.Orchid => "Assets/Prefabs/Pieces/Tile_Orchid.prefab",
            _ => "Assets/Prefabs/Pieces/Tile_Jasmine.prefab"
        };
    }
}
