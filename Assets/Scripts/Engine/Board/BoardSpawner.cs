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
                    Vector3 worldPosition = new Vector3(x * tileSpacing, 0f, z * tileSpacing);
                    Quaternion tileRotation = Quaternion.Euler(-90f, 0f, 0f);

                    GameObject tileGO = Instantiate(tilePrefab, worldPosition, tileRotation, boardParent);
                    Tile tile = tileGO.GetComponent<Tile>();

                    if (tile != null)
                    {
                        // Instead of "coordinate", now assign x/z directly
                        tile.SetGridPosition(x, z);
                        // If needed, you can still register tiles in BoardManager
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
