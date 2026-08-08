using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Board;
using PaiSho.Domain;

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
        /// Prefers <see cref="PiecePlacementManager"/> when present; otherwise validates via Domain.
        /// </summary>
        public void TryPlaceOpeningFlower(Tile tile)
        {
            if (!GameManager.Instance.IsSpringPhase())
                return;

            if (tile == null || tile.HasPiece())
                return;

            // Primary path already handles spring via PiecePlacementManager + PlacementRules.
            if (PiecePlacementManager.Instance != null)
            {
                PiecePlacementManager.Instance.TryPlacePiece(tile);
                return;
            }

            Player player = GameManager.Instance.GetCurrentPlayer();
            PieceType flowerType = GameManager.Instance.GetOpeningFlower(player);
            Vector2Int gridPos = tile.GetGridPosition();
            int coordinate = BoardUtils.ToCoordinate(gridPos.x, gridPos.y);

            PlacementResult decision = PlacementValidator.Evaluate(
                player, flowerType, coordinate, hasReserveAvailable: true);
            if (!decision.IsAllowed)
            {
                Debug.LogWarning($"Illegal spring placement at {gridPos}: {decision.Reason}");
                return;
            }

            GameObject prefabToPlace = (flowerType == PieceType.Jasmine) ? jasminePrefab : rosePrefab;

            if (prefabToPlace == null)
            {
                Debug.LogError("Missing prefab reference for opening flower!");
                return;
            }

            Vector3 spawnPosition = tile.transform.position + Vector3.up * 0.1f;
            GameObject pieceObj = Instantiate(prefabToPlace, spawnPosition, Quaternion.identity);

            Piece piece = pieceObj.GetComponent<Piece>();
            if (piece == null)
            {
                Debug.LogError("Piece component missing from spawned prefab!");
                return;
            }

            piece.Initialize(player, flowerType);
            ApplyOwnershipMaterials(piece, player);

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
                foreach (Transform child in piece.transform)
                {
                    string name = child.name;
                    if (basePart == null && name == "Tile")
                        basePart = child;
                    if (inlayPart == null && (name == "Face" || name.EndsWith(" Face") || name.Contains("Face")))
                        inlayPart = child;
                }
            }

            if (basePart == null || inlayPart == null)
            {
                Debug.LogWarning("[SpringPlacementManager] Tile/Face children not found; keeping prefab materials.");
                return;
            }

            MeshRenderer baseRenderer = basePart.GetComponent<MeshRenderer>();
            MeshRenderer inlayRenderer = inlayPart.GetComponent<MeshRenderer>();

            if (baseRenderer == null || inlayRenderer == null || MaterialManager.Instance == null)
            {
                Debug.LogWarning("[SpringPlacementManager] Missing renderer or MaterialManager; skipping ownership tint.");
                return;
            }

            OwnerType ownerType = owner == Player.Host ? OwnerType.Host : OwnerType.Opponent;
            Material baseMat = MaterialManager.Instance.TileBaseMaterial;
            Material inlayMat = MaterialManager.Instance.GetEngravingMaterial(ownerType);
            if (baseMat == null || inlayMat == null)
                return;

            baseRenderer.material = baseMat;
            inlayRenderer.material = inlayMat;
        }


    }
}
