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

        public void TryPlaceSelectedPiece(Tile tile)
        {
            if (!IsPlacingPiece())
                return;

            if (tile.HasPiece())
                return;

            Player player = GameManager.Instance.GetCurrentPlayer();
            GameObject prefab = GetPrefabForType(selectedPieceType.Value);

            if (prefab == null)
            {
                Debug.LogError("Missing prefab for piece type: " + selectedPieceType.Value);
                return;
            }

            Vector3 spawnPosition = tile.transform.position + Vector3.up * 0.1f;
            GameObject pieceObj = Instantiate(prefab, spawnPosition, Quaternion.identity);

            Piece piece = pieceObj.GetComponent<Piece>();
            piece.Initialize(player, selectedPieceType.Value);

            BoardManager.Instance.PlacePiece(piece, tile.GetCoordinate());
            tile.SetPiece(piece);
            CaptureManager.Instance.CheckForCaptures(piece);

            ReserveManager.Instance.UsePiece(player, selectedPieceType.Value);

            selectedPieceType = null; // Clear placement mode

            MovementManager.Instance.RegisterPlacement(piece);
            GameManager.Instance.MarkTurnComplete();
            GameManager.Instance.EndTurn();
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
}
}
