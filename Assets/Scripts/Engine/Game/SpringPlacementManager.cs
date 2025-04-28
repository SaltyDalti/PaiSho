using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Board;

namespace PaiSho.Game
{
    public class SpringPlacementManager : MonoBehaviour
    {
        public static SpringPlacementManager Instance;

        [Header("Piece Prefabs")]
        public GameObject jasminePrefab;
        public GameObject rosePrefab;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        /// <summary>
        /// Called when the player clicks a tile during Spring Opening.
        /// </summary>
        public void TryPlaceOpeningFlower(Tile tile)
        {
            if (!GameManager.Instance.IsSpringPhase())
                return;

            if (tile.HasPiece())
                return;

            Player player = GameManager.Instance.GetCurrentPlayer();
            PieceType flowerType = GameManager.Instance.GetOpeningFlower(player);

            GameObject prefabToPlace = (flowerType == PieceType.Jasmine) ? jasminePrefab : rosePrefab;

            if (prefabToPlace == null)
            {
                Debug.LogError("Missing prefab reference for opening flower!");
                return;
            }

            Vector3 spawnPosition = tile.transform.position + Vector3.up * 0.1f; // Slightly above tile
            GameObject pieceObj = Instantiate(prefabToPlace, spawnPosition, Quaternion.identity);

            Piece piece = pieceObj.GetComponent<Piece>();
            if (piece == null)
            {
                Debug.LogError("Piece component missing from spawned prefab!");
                return;
            }



            piece.Initialize(player, flowerType);
            ApplyOwnershipMaterials(piece, player);

            Vector2Int gridPos = tile.GetGridPosition();
            int coordinate = BoardUtils.ToCoordinate(gridPos.x, gridPos.y);

            Debug.Log($"Placing piece: {piece}");
            Debug.Log($"BoardManager Instance: {BoardManager.Instance}");
            Debug.Log($"Coordinate: {coordinate}");

            BoardManager.Instance.PlacePiece(piece, coordinate);
            tile.SetPiece(piece);

            MovementManager.Instance.RegisterPlacement(piece);
            GameManager.Instance.MarkTurnComplete();
            GameManager.Instance.EndTurn();
        }

        private void ApplyOwnershipMaterials(Piece piece, Player owner)
        {
            Transform basePart = piece.transform.Find("Tile");
            Transform inlayPart = piece.transform.Find("Face");

            if (basePart == null || inlayPart == null)
            {
                Debug.LogError("[SpringPlacementManager] Tile or Face not found on Piece prefab!");
                return;
            }

            MeshRenderer baseRenderer = basePart.GetComponent<MeshRenderer>();
            MeshRenderer inlayRenderer = inlayPart.GetComponent<MeshRenderer>();

            if (baseRenderer == null || inlayRenderer == null)
            {
                Debug.LogError("[SpringPlacementManager] Missing MeshRenderer on Tile or Face!");
                return;
            }

            OwnerType ownerType = OwnerType.None;
            if (owner == Player.Host)
                ownerType = OwnerType.Host;
            else if (owner == Player.Opponent)
                ownerType = OwnerType.Opponent;

            baseRenderer.material = MaterialManager.Instance.TileBaseMaterial;
            inlayRenderer.material = MaterialManager.Instance.GetEngravingMaterial(ownerType);
        }


    }
}
