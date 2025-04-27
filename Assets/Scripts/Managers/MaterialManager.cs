using UnityEngine;

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
