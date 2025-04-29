using UnityEngine;
using PaiSho.Board;

namespace PaiSho.Game
{
    public class BoardSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private Transform boardParent;
        [SerializeField] private float tileSpacing = 1.5f;

        private void Start()
        {
            SpawnBoardTiles();
        }

        private void SpawnBoardTiles()
        {
            for (int x = -9; x <= 9; x++)
            {
                for (int z = -9; z <= 9; z++)
                {
                    Vector3 worldPosition = new Vector3(x * tileSpacing, 0.001f, z * tileSpacing);
                    Quaternion tileRotation = Quaternion.Euler(0f, 0f, 0f);

                    GameObject tileGO = Instantiate(tilePrefab, worldPosition, tileRotation, boardParent);
                    Tile tile = tileGO.GetComponent<Tile>();

                    if (tile != null)
                    {
                        tile.SetGridPosition(x, z);
                        BoardManager.Instance.RegisterTile(x, z, tile);

                        // Auto Log Check:
                        int coordinate = BoardUtils.ToCoordinate(x, z);
                        if (BoardUtils.LegalPoints.Contains(coordinate))
                        {
                            // This tile SHOULD exist, but if for some reason not properly registered:
                            if (BoardManager.Instance.GetTileAt(x, z) == null)
                            {
                                Debug.LogWarning($"[BoardSpawner] Expected legal tile missing at ({x},{z}) coordinate {coordinate}");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("[BoardSpawner] Tile prefab missing Tile.cs script!");
                    }
                }
            }
        }

    }
}
