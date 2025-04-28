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

                    // No rotation needed anymore
                    GameObject tileGO = Instantiate(tilePrefab, worldPosition, Quaternion.identity, boardParent);
                    Tile tile = tileGO.GetComponent<Tile>();

                    if (tile != null)
                    {
                        tile.SetGridPosition(x, z);
                        BoardManager.Instance.RegisterTile(x, z, tile);
                    }
                    else
                    {
                        Debug.LogError("Tile prefab missing Tile.cs script!");
                    }
                }
            }
        }
    }
}
