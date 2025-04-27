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
            piece.Initialize(player, flowerType);

            BoardManager.Instance.PlacePiece(piece, tile.GetCoordinate());
            tile.SetPiece(piece);

            MovementManager.Instance.RegisterPlacement(piece);
            GameManager.Instance.MarkTurnComplete();
            GameManager.Instance.EndTurn();
        }
    }
}
