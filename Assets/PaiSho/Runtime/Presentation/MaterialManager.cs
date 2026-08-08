using UnityEngine;
using PaiSho;

public enum OwnerType
{
    None,
    Host,
    Opponent
}

public enum GardenType
{
    LightGarden,
    DarkGarden,
    /// <summary>Border between light and dark — counts as both (Basic Pai Sho).</summary>
    MixedGarden,
    NeutralGarden,
    Port
}

public class MaterialManager : MonoBehaviour
{
    public static MaterialManager Instance { get; private set; }

    [Header("Tile Materials")]
    public Material TileBaseMaterial;
    public Material HostEngravingMaterial;
    public Material OpponentEngravingMaterial;

    [Header("Garden Materials")]
    public Material LightGardenMaterial;
    public Material DarkGardenMaterial;
    public Material NeutralGardenMaterial;
    public Material PortMaterial;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        EnsureProceduralMaterials();
    }

    private void EnsureProceduralMaterials()
    {
        TileBaseMaterial ??= WoodTheme.CreateCeramicMaterial(WoodTheme.TileWoodBase, 0.75f);
        HostEngravingMaterial ??= WoodTheme.CreateCeramicMaterial(WoodTheme.HostCeramic, 0.84f);
        OpponentEngravingMaterial ??= WoodTheme.CreateTerracottaMaterial(WoodTheme.OpponentTerracotta, 0.68f);
        LightGardenMaterial ??= WoodTheme.CreateWoodMaterial(WoodTheme.LightGardenWood, 0.4f);
        DarkGardenMaterial ??= WoodTheme.CreateWoodMaterial(WoodTheme.DarkGardenWood, 0.4f);
        NeutralGardenMaterial ??= WoodTheme.CreateWoodMaterial(WoodTheme.NeutralPathWood, 0.4f);
        PortMaterial ??= WoodTheme.CreateWoodMaterial(WoodTheme.PortWood, 0.45f);
    }

    public Material GetEngravingMaterial(OwnerType owner)
    {
        switch (owner)
        {
            case OwnerType.Host:
                return HostEngravingMaterial;
            case OwnerType.Opponent:
                return OpponentEngravingMaterial;
            default:
                return TileBaseMaterial;
        }
    }

    public Material GetGardenMaterial(GardenType garden)
    {
        switch (garden)
        {
            case GardenType.LightGarden:
                return LightGardenMaterial;
            case GardenType.DarkGarden:
                return DarkGardenMaterial;
            case GardenType.NeutralGarden:
                return NeutralGardenMaterial;
            case GardenType.Port:
                return PortMaterial;
            default:
                return null;
        }
    }
}
