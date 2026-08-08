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

        public void ClearSelection()
        {
            selectedPieceType = null;
        }

        public void TryPlacePiece(Tile tile)
        {
            if (tile == null)
            {
                Debug.LogError("Clicked Tile is NULL.");
                return;
            }

            if (tile.IsDecorative)
            {
                Debug.Log("Cannot place on a decorative tile.");
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

            int x = tile.GetGridPosition().x;
            int z = tile.GetGridPosition().y;
            int coordinate = BoardUtils.ToCoordinate(x, z);

            if (!PlacementValidator.CanPlace(player, typeToPlace, coordinate))
            {
                Debug.LogWarning($"Illegal placement for {typeToPlace} at {tile.GetGridPosition()} (coord {coordinate}).");
                return;
            }

            Vector3 spawnPosition = tile.transform.position + Vector3.up * 0.1f;
            GameObject prefab = GetPrefabForType(typeToPlace);
            GameObject pieceObj;

            if (prefab != null)
            {
                pieceObj = Instantiate(prefab, spawnPosition, Quaternion.identity);
            }
            else
            {
                Debug.LogWarning($"[PiecePlacementManager] Prefab missing for {typeToPlace}; spawning procedural placeholder.");
                pieceObj = CreateProceduralPiece(typeToPlace, spawnPosition);
            }

            if (pieceObj == null)
            {
                Debug.LogError("Failed to instantiate piece!");
                return;
            }

            Piece piece = pieceObj.GetComponent<Piece>();
            if (piece == null)
                piece = pieceObj.AddComponent<Piece>();

            piece.Initialize(player, typeToPlace);
            ApplyOwnershipMaterials(piece, player);

            BoardManager.Instance.PlacePiece(piece, coordinate);
            tile.SetPiece(piece);

            if (!GameManager.Instance.IsSpringPhase())
            {
                ReserveManager.Instance.UsePiece(player, typeToPlace);
                selectedPieceType = null;
            }

            MovementManager.Instance.RegisterPlacement(piece);
            if (GameLogManager.Instance != null)
                GameLogManager.Instance.LogPlacement(player, typeToPlace, coordinate);

            HarmonyManager.Instance?.UpdateHarmoniesFor(piece);
            CaptureManager.Instance?.CheckForCaptures(piece);

            GameManager.Instance.MarkTurnComplete();
            if (piece.CausesRotation())
                WheelRotationManager.Instance?.RotateAdjacentTiles(piece);

            if (!VictoryManager.Instance.CheckForHarmonyRingEnd(player, BoardManager.Instance.GetAllPieces()))
                GameManager.Instance.EndTurn();

            Debug.Log($"Placed {piece.Type} at {tile.GetGridPosition()}.");
        }

        private GameObject GetPrefabForType(PieceType type)
        {
            GameObject prefab = type switch
            {
                PieceType.Jasmine => jasminePrefab,
                PieceType.Rose => rosePrefab,
                PieceType.Lily => lilyPrefab,
                PieceType.Jade => jadePrefab,
                PieceType.Rhododendron => rhododendronPrefab,
                PieceType.Chrysanthemum => chrysanthemumPrefab,
                PieceType.Boat => boatPrefab,
                PieceType.Rock => rockPrefab,
                PieceType.Knotweed => knotweedPrefab,
                PieceType.Wheel => wheelPrefab,
                PieceType.Lotus => lotusPrefab,
                PieceType.Orchid => orchidPrefab,
                _ => null
            };

            // Nested Blender model prefabs sometimes deserialize as Missing in the
            // inspector even when YAML guids look correct — fall back to AssetDatabase.
            if (prefab == null)
                prefab = LoadPiecePrefabAsset(type);

            return prefab;
        }

        private static string PrefabAssetName(PieceType type)
        {
            return type switch
            {
                PieceType.Jasmine => "Tile_Jasmine",
                PieceType.Rose => "Tile_Rose",
                PieceType.Lily => "Tile_Lily",
                PieceType.Jade => "Tile_Jade",
                PieceType.Rhododendron => "Tile_Rhod",
                PieceType.Chrysanthemum => "Tile_Chrys",
                PieceType.Boat => "Tile_Boat",
                PieceType.Rock => "Tile_Rock",
                PieceType.Knotweed => "Tile_Knot",
                PieceType.Wheel => "Tile_Wheel",
                PieceType.Lotus => "Tile_Lotus",
                PieceType.Orchid => "Tile_Orchid",
                _ => null
            };
        }

        private static GameObject LoadPiecePrefabAsset(PieceType type)
        {
            string assetName = PrefabAssetName(type);
            if (assetName == null)
                return null;

#if UNITY_EDITOR
            string path = $"Assets/Prefabs/Pieces/{assetName}.prefab";
            var loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (loaded != null)
                return loaded;

            Debug.LogWarning($"[PiecePlacementManager] AssetDatabase could not load {path}");
#endif
            return Resources.Load<GameObject>($"Pieces/{assetName}");
        }

        /// <summary>
        /// Nested Blender prefabs are unreliable in this project (Missing refs).
        /// Procedural pieces keep the rules loop playable with a visible marker.
        /// </summary>
        private static GameObject CreateProceduralPiece(PieceType type, Vector3 position)
        {
            GameObject root = new GameObject($"Proc_{type}");
            root.transform.position = position;

            GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tile.name = "Tile";
            tile.transform.SetParent(root.transform, false);
            tile.transform.localScale = new Vector3(1.1f, 0.12f, 1.1f);
            Object.Destroy(tile.GetComponent<Collider>());

            GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cube);
            face.name = "Face";
            face.transform.SetParent(root.transform, false);
            face.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            face.transform.localScale = new Vector3(0.55f, 0.08f, 0.55f);
            Object.Destroy(face.GetComponent<Collider>());

            if (MaterialManager.Instance != null)
            {
                var tileMr = tile.GetComponent<MeshRenderer>();
                var faceMr = face.GetComponent<MeshRenderer>();
                if (tileMr != null && MaterialManager.Instance.TileBaseMaterial != null)
                    tileMr.sharedMaterial = MaterialManager.Instance.TileBaseMaterial;
                if (faceMr != null && MaterialManager.Instance.HostEngravingMaterial != null)
                    faceMr.sharedMaterial = MaterialManager.Instance.HostEngravingMaterial;
            }

            root.AddComponent<Piece>();
            return root;
        }

        private void ApplyOwnershipMaterials(Piece piece, Player owner)
        {
            if (!TryResolvePieceRenderers(piece.transform, out MeshRenderer baseRenderer, out MeshRenderer inlayRenderer))
            {
                Debug.LogWarning($"[PiecePlacementManager] Could not resolve Tile/Face renderers on {piece.name}; keeping prefab materials.");
                return;
            }

            if (MaterialManager.Instance == null)
            {
                Debug.LogError("[PiecePlacementManager] ERROR: MaterialManager.Instance is NULL!");
                return;
            }

            OwnerType ownerType = owner == Player.Host ? OwnerType.Host : OwnerType.Opponent;
            Material baseMat = MaterialManager.Instance.TileBaseMaterial;
            Material inlayMat = MaterialManager.Instance.GetEngravingMaterial(ownerType);

            if (baseMat == null || inlayMat == null)
            {
                Debug.LogError("[PiecePlacementManager] Ownership materials are not assigned on MaterialManager.");
                return;
            }

            baseRenderer.material = baseMat;
            inlayRenderer.material = inlayMat;
        }

        /// <summary>
        /// Blender exports use child names like "Tile" + "Jasmine Face". Accept exact
        /// "Tile"/"Face" or any child whose name contains "Face".
        /// </summary>
        private static bool TryResolvePieceRenderers(Transform root, out MeshRenderer baseRenderer, out MeshRenderer inlayRenderer)
        {
            baseRenderer = null;
            inlayRenderer = null;

            Transform basePart = root.Find("Tile");
            Transform inlayPart = root.Find("Face");

            if (basePart == null || inlayPart == null)
            {
                foreach (Transform child in root)
                {
                    string name = child.name;
                    if (basePart == null && name == "Tile")
                        basePart = child;
                    if (inlayPart == null && (name == "Face" || name.EndsWith(" Face") || name.Contains("Face")))
                        inlayPart = child;
                }
            }

            if (basePart != null)
                baseRenderer = basePart.GetComponent<MeshRenderer>();
            if (inlayPart != null)
                inlayRenderer = inlayPart.GetComponent<MeshRenderer>();

            return baseRenderer != null && inlayRenderer != null;
        }

    }
}