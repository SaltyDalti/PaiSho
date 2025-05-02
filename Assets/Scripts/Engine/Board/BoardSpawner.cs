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
            for (int x = -10; x <= 10; x++) // 20 columns
            {
                for (int z = -10; z <= 10; z++) // 20 rows
                {
                    Vector3 worldPosition = new Vector3(x * tileSpacing, 0.001f, z * tileSpacing);
                    Quaternion tileRotation = Quaternion.Euler(0f, 0f, 0f);

                    GameObject tileGO = Instantiate(tilePrefab, worldPosition, tileRotation, boardParent);
                    Tile tile = tileGO.GetComponent<Tile>();

                    if (tile != null)
                    {
                        tile.SetGridPosition(x, z);
                        BoardManager.Instance.RegisterTile(x, z, tile);

                        // Mark as decorative (non-playable) if outside legal garden
                        int coord = BoardUtils.ToCoordinate(x, z);
                        if (!BoardUtils.LegalPoints.Contains(coord))
                        {
                            tile.MarkAsDecorative(); // safe public method
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
