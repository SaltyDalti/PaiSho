using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Board;

namespace PaiSho.Game
{
    public class PiecePlacementManager : MonoBehaviour
    {
        public static PiecePlacementManager Instance;

        [Header("Piece Prefabs")]
        public GameObject jasminePrefab;
        public GameObject rosePrefab;
        public GameObject lilyPrefab;
        public GameObject jadePrefab;
        public GameObject rhododendronPrefab;
        public GameObject chrysanthemumPrefab;
        public GameObject boatPrefab;
        public GameObject rockPrefab;
        public GameObject knotweedPrefab;
        public GameObject wheelPrefab;
        public GameObject lotusPrefab;
        public GameObject orchidPrefab;

        private PieceType? selectedPieceType = null;

        [Header("Host Materials")]
        public Material hostBaseMaterial;
        public Material hostInlayMaterial;

        [Header("Opponent Materials")]
        public Material opponentBaseMaterial;
        public Material opponentInlayMaterial;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public void SelectPieceToPlace(PieceType type)
        {
            Player currentPlayer = GameManager.Instance.GetCurrentPlayer();

            if (!ReserveManager.Instance.HasPieceAvailable(currentPlayer, type))
            {
                Debug.LogWarning($"No {type} pieces left for player {currentPlayer}");
                return;
            }

            selectedPieceType = type;
            Debug.Log($"Selected {type} to place.");
        }

        public bool IsPlacingPiece()
        {
            return selectedPieceType != null;
        }

        public void TryPlacePiece(Tile tile)
        {
            if (tile == null)
            {
                Debug.LogError("Clicked Tile is NULL.");
                return;
            }

            if (tile.HasPiece())
            {
                Debug.Log("Tile already occupied.");
                return;
            }

            Player player = GameManager.Instance.GetCurrentPlayer();
            PieceType typeToPlace;

            if (GameManager.Instance.IsSpringPhase())
            {
                typeToPlace = GameManager.Instance.GetOpeningFlower(player);
            }
            else
            {
                if (selectedPieceType == null)
                {
                    Debug.LogWarning("No piece type selected for normal play!");
                    return;
                }
                typeToPlace = selectedPieceType.Value;
            }

            GameObject prefab = GetPrefabForType(typeToPlace);
            if (prefab == null)
            {
                Debug.LogError($"Prefab for {typeToPlace} is not assigned!");
                return;
            }

            Vector3 spawnPosition = tile.transform.position + Vector3.up * 0.1f;
            GameObject pieceObj = Instantiate(prefab, spawnPosition, Quaternion.identity);

            if (pieceObj == null)
            {
                Debug.LogError("Failed to instantiate piece prefab!");
                return;
            }

            Piece piece = pieceObj.GetComponent<Piece>();
            if (piece == null)
            {
                Debug.LogError("Instantiated object missing Piece.cs script!");
                return;
            }

            piece.Initialize(player, typeToPlace);
            ApplyOwnershipMaterials(piece, player);

            int x = tile.GetGridPosition().x;
            int z = tile.GetGridPosition().y;
            int coordinate = BoardUtils.ToCoordinate(x, z);

            BoardManager.Instance.PlacePiece(piece, coordinate);
            tile.SetPiece(piece);

            if (!GameManager.Instance.IsSpringPhase())
            {
                ReserveManager.Instance.UsePiece(player, typeToPlace);
                selectedPieceType = null;
            }

            MovementManager.Instance.RegisterPlacement(piece);
            GameManager.Instance.MarkTurnComplete();
            GameManager.Instance.EndTurn();

            Debug.Log($"Placed {piece.Type} at {tile.GetGridPosition()}.");
        }

        private GameObject GetPrefabForType(PieceType type)
        {
            switch (type)
            {
                case PieceType.Jasmine:
                    return jasminePrefab;
                case PieceType.Rose:
                    return rosePrefab;
                case PieceType.Lily:
                    return lilyPrefab;
                case PieceType.Jade:
                    return jadePrefab;
                case PieceType.Rhododendron:
                    return rhododendronPrefab;
                case PieceType.Chrysanthemum:
                    return chrysanthemumPrefab;
                case PieceType.Boat:
                    return boatPrefab;
                case PieceType.Rock:
                    return rockPrefab;
                case PieceType.Knotweed:
                    return knotweedPrefab;
                case PieceType.Wheel:
                    return wheelPrefab;
                case PieceType.Lotus:
                    return lotusPrefab;
                case PieceType.Orchid:
                    return orchidPrefab;
                default:
                    return null;
            }
        }

        private void ApplyOwnershipMaterials(Piece piece, Player owner)
        {
            Transform basePart = piece.transform.Find("Tile");
            Transform inlayPart = piece.transform.Find("Face");

            if (basePart == null || inlayPart == null)
            {
                Debug.LogError("[PiecePlacementManager] ERROR: 'Tile' or 'Face' child not found on Piece prefab!");
                return;
            }

            MeshRenderer baseRenderer = basePart.GetComponent<MeshRenderer>();
            MeshRenderer inlayRenderer = inlayPart.GetComponent<MeshRenderer>();

            if (baseRenderer == null || inlayRenderer == null)
            {
                Debug.LogError("[PiecePlacementManager] ERROR: MeshRenderer missing on Tile or Face!");
                return;
            }

            if (MaterialManager.Instance == null)
            {
                Debug.LogError("[PiecePlacementManager] ERROR: MaterialManager.Instance is NULL!");
                return;
            }

            OwnerType ownerType = owner == Player.Host ? OwnerType.Host : OwnerType.Opponent;

            // Debug logging
            Debug.Log($"[ApplyOwnershipMaterials] Assigning materials for {ownerType}");

            // Force assign .material instead of sharedMaterial
            Material baseMat = MaterialManager.Instance.TileBaseMaterial;
            Material inlayMat = MaterialManager.Instance.GetEngravingMaterial(ownerType);

            if (baseMat == null)
                Debug.LogError("[PiecePlacementManager] ERROR: TileBaseMaterial is NULL!");

            if (inlayMat == null)
                Debug.LogError("[PiecePlacementManager] ERROR: EngravingMaterial is NULL!");

            baseRenderer.material = baseMat;
            inlayRenderer.material = inlayMat;
        }

    }
}