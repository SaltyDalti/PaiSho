using System.Collections.Generic;
using UnityEngine;
using PaiSho;
using PaiSho.Game;
using PaiSho.Pieces;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaiSho.Board
{
    [DefaultExecutionOrder(-50)]
    public class BoardManager : MonoBehaviour
    {
        public static BoardManager Instance;

        [System.Serializable]
        public struct PiecePrefabEntry
        {
            public PieceType Type;
            public GameObject Prefab;
        }

        [SerializeField] private BoardLayout boardLayout;
        [SerializeField] private Transform piecesRoot;
        [SerializeField] private GameObject boardPointPrefab;
        [SerializeField] private PiecePrefabEntry[] piecePrefabs;
        [SerializeField] private bool usePrebuiltLayout;
        [SerializeField] private bool preserveSceneAuthored = true;

        private readonly Dictionary<int, Piece> piecesOnBoard = new();
        private readonly Dictionary<PieceType, GameObject> prefabLookup = new();
        private readonly Dictionary<PieceType, string> prefabSourcePaths = new();
        private Transform pointsRoot;
        private Transform gridRoot;
        private Transform rimRoot;
        private Transform hostPlayerStand;
        private Transform opponentPlayerStand;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            BuildPrefabLookup();
        }

        public void SetUsePrebuiltLayout(bool enabled)
        {
            usePrebuiltLayout = enabled;
        }

        public void SetPreserveSceneAuthored(bool preserve)
        {
            preserveSceneAuthored = preserve;
        }

        public void Initialize(BoardLayout layout, Transform piecesContainer)
        {
            if (layout != null)
                boardLayout = layout;

            if (piecesContainer != null)
                piecesRoot = piecesContainer;

            if (boardLayout == null)
                boardLayout = GetComponentInChildren<BoardLayout>();

            if (piecesRoot == null)
            {
                var existingPieces = transform.Find("Pieces");
                if (existingPieces != null)
                    piecesRoot = existingPieces;
                else
                {
                    var root = new GameObject("Pieces");
                    root.transform.SetParent(transform, false);
                    piecesRoot = root.transform;
                }
            }

            if (prefabLookup.Count == 0)
                BuildPrefabLookup();

            CalibrateLayoutFromPieces();
            GenerateBoardPoints();

            if (prefabLookup.Count == 0)
                DebugLogger.LogWarning("No piece prefabs loaded — hand/board tiles will use placeholders.");
            else
                DebugLogger.Log($"Board ready. Stands host={hostPlayerStand != null} opponent={opponentPlayerStand != null}, pieces={prefabLookup.Count}");

            HandTrayController.Instance?.Refresh();
            RefreshGameplayBoardPoints();
        }

        public void RefreshGameplayBoardPoints()
        {
            RefreshAllBoardPoints(null);
        }

        private void CalibrateLayoutFromPieces()
        {
            if (boardLayout == null)
                return;

            GameObject reference = GetReferencePiecePrefab();
            if (reference == null)
                return;

            var metrics = WoodTheme.MeasurePiecePrefab(reference);
            if (boardLayout.UseModelBoard)
            {
                // Centered prefab pivots: AlignPrefabToSurface seats the bottom on the surface.
                boardLayout.SetPieceSurfaceLift(0f);
                DebugLogger.Log(
                    $"3D board drives spacing ({boardLayout.CellSpacing:F2}); piece lift handled by prefab align");
            }
            else
            {
                boardLayout.SetPieceMetrics(metrics.CellSpacing, boardLayout.TileHeight, metrics.SurfaceLift);
                DebugLogger.Log(
                    $"Board spacing set from piece art: {metrics.CellSpacing:F2} (footprint {metrics.Footprint:F2})");
            }
        }

        private GameObject GetReferencePiecePrefab()
        {
            if (prefabLookup.TryGetValue(PieceType.Lily, out GameObject lily))
                return lily;

            if (prefabLookup.TryGetValue(PieceType.Rose, out GameObject rose))
                return rose;

            foreach (GameObject prefab in prefabLookup.Values)
                return prefab;

            return null;
        }

        private void BuildPrefabLookup()
        {
            prefabLookup.Clear();
            prefabSourcePaths.Clear();

#if UNITY_EDITOR
            EnsureDefaultPiecePrefabs();
#else
            EnsureRuntimePiecePrefabs();
#endif

            if (piecePrefabs != null)
            {
                foreach (var entry in piecePrefabs)
                {
                    if (entry.Prefab == null || prefabLookup.ContainsKey(entry.Type))
                        continue;

                    prefabLookup[entry.Type] = entry.Prefab;
                }
            }

            DebugLogger.Log($"Piece prefabs loaded: {prefabLookup.Count}");
        }

#if !UNITY_EDITOR
        private void EnsureRuntimePiecePrefabs()
        {
            EnsureRuntimePiecePrefab(PieceType.Jasmine, "Assets/Resources/PieceVisuals/Tile_Jasmine.glb");
            EnsureRuntimePiecePrefab(PieceType.Rose, "Assets/Resources/PieceVisuals/Tile_Rose.glb");
            EnsureRuntimePiecePrefab(PieceType.Lily, "Assets/Resources/PieceVisuals/Tile_Lily.glb");
            EnsureRuntimePiecePrefab(PieceType.Jade, "Assets/Resources/PieceVisuals/Tile_Jade.glb");
            EnsureRuntimePiecePrefab(PieceType.Chrysanthemum, "Assets/Resources/PieceVisuals/Tile_Chrys.glb");
            EnsureRuntimePiecePrefab(PieceType.Rhododendron, "Assets/Resources/PieceVisuals/Tile_Rhod.glb");
            EnsureRuntimePiecePrefab(PieceType.Boat, "Assets/Resources/PieceVisuals/Tile_Boat.glb");
            EnsureRuntimePiecePrefab(PieceType.Rock, "Assets/Resources/PieceVisuals/Tile_Rock.glb");
            EnsureRuntimePiecePrefab(PieceType.Knotweed, "Assets/Resources/PieceVisuals/Tile_Knot.glb");
            EnsureRuntimePiecePrefab(PieceType.Wheel, "Assets/Resources/PieceVisuals/Tile_Wheel.glb");
            EnsureRuntimePiecePrefab(PieceType.Lotus, "Assets/Resources/PieceVisuals/Tile_Lotus.glb");
            EnsureRuntimePiecePrefab(PieceType.Orchid, "Assets/Resources/PieceVisuals/Tile_Orchid.glb");
        }

        private void EnsureRuntimePiecePrefab(PieceType type, string resourcesAssetPath)
        {
            if (prefabLookup.ContainsKey(type))
                return;

            if (!PieceVisualLoader.TryLoadVisual(resourcesAssetPath, out GameObject visual))
                return;

            prefabLookup[type] = visual;
            prefabSourcePaths[type] = resourcesAssetPath;
        }
#endif

#if UNITY_EDITOR
        private void EnsureDefaultPiecePrefabs()
        {
            EnsureTexturedPiecePrefab(PieceType.Jasmine, "Tile_Jasmine");
            EnsureTexturedPiecePrefab(PieceType.Rose, "Tile_Rose");
            EnsureTexturedPiecePrefab(PieceType.Lily, "Tile_Lily");
            EnsureTexturedPiecePrefab(PieceType.Jade, "Tile_Jade");
            EnsureTexturedPiecePrefab(PieceType.Chrysanthemum, "Tile_Chrys");
            EnsureTexturedPiecePrefab(PieceType.Rhododendron, "Tile_Rhod");
            EnsureTexturedPiecePrefab(PieceType.Boat, "Tile_Boat");
            EnsureTexturedPiecePrefab(PieceType.Rock, "Tile_Rock");
            EnsureTexturedPiecePrefab(PieceType.Knotweed, "Tile_Knot");
            EnsureTexturedPiecePrefab(PieceType.Wheel, "Tile_Wheel");
            EnsureTexturedPiecePrefab(PieceType.Lotus, "Tile_Lotus");
            EnsureTexturedPiecePrefab(PieceType.Orchid, "Tile_Orchid");
        }

        private void EnsureTexturedPiecePrefab(PieceType type, string assetName)
        {
            string prefabPath = $"Assets/Prefabs/Pieces/{assetName}.prefab";
            string modelPath = $"Assets/Models/Pieces/{assetName}.glb";
            string resourcesPath = $"Assets/Resources/PieceVisuals/{assetName}.glb";
            string blendPath = $"Assets/BlenderFiles/Pieces/{assetName}.blend";

            EnsurePiecePrefab(
                type,
                prefabPath,
                modelFallbackPath: modelPath,
                resourcesFallbackPath: resourcesPath,
                blendFallbackPath: blendPath);
        }

        private void EnsurePiecePrefab(
            PieceType type,
            string prefabPath,
            string modelFallbackPath = null,
            string resourcesFallbackPath = null,
            string blendFallbackPath = null,
            bool preferModel = false)
        {
            if (prefabSourcePaths.TryGetValue(type, out string loadedFrom) &&
                loadedFrom == prefabPath &&
                prefabLookup.TryGetValue(type, out GameObject existing) &&
                PieceVisualLoader.HasRenderableGeometry(existing))
            {
                return;
            }

            if (PieceVisualLoader.TryLoadVisual(prefabPath, out GameObject prefab))
            {
                prefabLookup[type] = prefab;
                prefabSourcePaths[type] = prefabPath;
                return;
            }

            if (TryLoadFirstAvailable(type, modelFallbackPath, resourcesFallbackPath, blendFallbackPath))
                return;

            DebugLogger.LogWarning($"Could not load art for {type} (prefab or model missing).");
        }

        private bool TryLoadFirstAvailable(
            PieceType type,
            string modelFallbackPath,
            string resourcesFallbackPath,
            string blendFallbackPath)
        {
            string[] paths =
            {
                modelFallbackPath,
                resourcesFallbackPath,
                blendFallbackPath
            };

            foreach (string path in paths)
            {
                if (string.IsNullOrEmpty(path))
                    continue;

                if (!PieceVisualLoader.TryLoadVisual(path, out GameObject model))
                    continue;

                prefabLookup[type] = model;
                prefabSourcePaths[type] = path;
                DebugLogger.Log($"Loaded {type} from: {path}");
                return true;
            }

            return false;
        }

#endif

        private void GenerateBoardPoints()
        {
            if (pointsRoot != null)
                Destroy(pointsRoot.gameObject);

            if (!TryBindPrebuiltVisuals())
                BuildBoardVisuals();

            Transform boardTransform = boardLayout != null ? boardLayout.transform : transform;
            pointsRoot = new GameObject("BoardPoints").transform;
            pointsRoot.SetParent(boardTransform, false);

            for (int i = 0; i < BoardUtils.NumPoints; i++)
            {
                if (!BoardUtils.IsValidPointCoordinate(i))
                    continue;

                var pointObject = CreateBoardPointObject(i);
                pointObject.transform.SetParent(pointsRoot, false);
            }
        }

        private bool TryBindPrebuiltVisuals()
        {
            if (!usePrebuiltLayout)
                return false;

            Transform boardTransform = boardLayout != null ? boardLayout.transform : transform;
            Transform surface = FindChildByName(boardTransform, "BoardModelSurface")
                                ?? FindChildByName(boardTransform, "Board");
            if (surface != null)
                gridRoot = surface;

            hostPlayerStand = FindChildByName(boardTransform, "PlayerStand");
            opponentPlayerStand = FindChildByName(boardTransform, "PlayerStandOpponent");

            // Stands may live as siblings under the scene root, not only under GameBoardSetup.
            if (hostPlayerStand == null)
                hostPlayerStand = FindSceneObjectByName("PlayerStand");
            if (opponentPlayerStand == null)
                opponentPlayerStand = FindSceneObjectByName("PlayerStandOpponent");

            if (gridRoot == null && hostPlayerStand == null && opponentPlayerStand == null)
                return false;

            if (gridRoot == null)
                gridRoot = boardTransform;

            if (!preserveSceneAuthored)
                BoardAlignmentDefaults.ApplyTo(boardLayout);

            Transform placedModel = FindBoardModel(gridRoot) ?? FindSceneBoardModel();
            if (placedModel != null)
            {
                gridRoot = placedModel.parent != null && placedModel.parent.name == "BoardModelSurface"
                    ? placedModel.parent
                    : placedModel;

                // Match logical grid to the visible board (spacing + surface height).
                if (WoodTheme.CalibrateLayoutToSceneBoard(boardLayout, placedModel.gameObject))
                {
                    DebugLogger.Log(
                        $"Calibrated layout from scene board: spacing={boardLayout.CellSpacing:F3}, " +
                        $"surfaceY={boardLayout.TileHeight:F3}, span={boardLayout.GridSpan:F3}, " +
                        $"yaw={boardLayout.GridYawDegrees:F1}°");
                }
                else
                {
                    DebugLogger.LogWarning(
                        "Could not calibrate layout from scene board — pieces may sit under the mesh.");
                }

                // Always preload materials (even when preserving transforms) to avoid cyan flash.
                WoodTheme.PreloadVisualMaterials(placedModel.gameObject);
                WoodTheme.HideSquareBoardBaseAndEmbeddedStand(placedModel.gameObject);

                if (!preserveSceneAuthored)
                    WoodTheme.RefreshPrebuiltBoardVisuals(boardLayout, placedModel.gameObject);
            }
            else if (gridRoot != null)
            {
                WoodTheme.PreloadVisualMaterials(gridRoot.gameObject);
            }

            if (hostPlayerStand != null)
            {
                WoodTheme.PreloadVisualMaterials(hostPlayerStand.gameObject);
                if (!preserveSceneAuthored)
                    WoodTheme.RefreshPrebuiltStandVisuals(hostPlayerStand.gameObject);
            }

            if (opponentPlayerStand != null)
            {
                WoodTheme.PreloadVisualMaterials(opponentPlayerStand.gameObject);
                if (!preserveSceneAuthored)
                    WoodTheme.RefreshPrebuiltStandVisuals(opponentPlayerStand.gameObject);
            }

            return true;
        }

        private static Transform FindBoardModel(Transform boardSurface)
        {
            if (boardSurface == null)
                return null;

            Transform named = FindChildByName(boardSurface, "BoardModel")
                              ?? FindChildByName(boardSurface, "Board");
            if (named != null)
                return named;

            // Prefer a child that actually contains board parts.
            foreach (Transform child in boardSurface.GetComponentsInChildren<Transform>(true))
            {
                if (child == boardSurface)
                    continue;
                if (HasBoardMarkers(child.gameObject))
                    return child;
            }

            return boardSurface.childCount > 0 ? boardSurface.GetChild(0) : null;
        }

        private static Transform FindSceneBoardModel()
        {
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform candidate in all)
            {
                if (candidate.name is "Board" or "BoardModel" or "BoardModelSurface")
                {
                    if (HasBoardMarkers(candidate.gameObject))
                        return candidate.name == "BoardModelSurface" && candidate.childCount > 0
                            ? candidate.GetChild(0)
                            : candidate;
                }
            }

            foreach (Transform candidate in all)
            {
                if (HasBoardMarkers(candidate.gameObject) &&
                    candidate.GetComponentInParent<BoardManager>() == null)
                    return candidate;
            }

            return null;
        }

        private static bool HasBoardMarkers(GameObject root)
        {
            if (root == null)
                return false;

            bool hasLines = false;
            bool hasGate = false;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                string name = child.name;
                if (name.IndexOf("BoardLines", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    hasLines = true;
                if (name.IndexOf("GateHome", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("GateForeign", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    hasGate = true;
            }

            return hasLines || hasGate;
        }

        private static Transform FindChildByName(Transform root, string objectName)
        {
            if (root == null || string.IsNullOrEmpty(objectName))
                return null;

            if (root.name == objectName)
                return root;

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child != root && child.name == objectName)
                    return child;
            }

            return null;
        }

        private static Transform FindSceneObjectByName(string objectName)
        {
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform candidate in all)
            {
                if (candidate.name == objectName)
                    return candidate;
            }

            return null;
        }

        private void BuildBoardVisuals()
        {
            if (boardLayout == null)
                boardLayout = GetComponent<BoardLayout>();

            if (boardLayout == null)
            {
                DebugLogger.LogWarning("BuildBoardVisuals skipped — BoardLayout is missing.");
                return;
            }

            if (gridRoot != null)
                Destroy(gridRoot.gameObject);

            if (rimRoot != null)
                Destroy(rimRoot.gameObject);

            if (hostPlayerStand != null)
                Destroy(hostPlayerStand.gameObject);

            if (opponentPlayerStand != null)
                Destroy(opponentPlayerStand.gameObject);

            Transform boardTransform = boardLayout != null ? boardLayout.transform : transform;

            if (boardLayout.UseModelBoard && BoardVisualLoader.TryLoadModel(out GameObject boardModel, out string boardSource))
            {
                BoardAlignmentDefaults.ApplyTo(boardLayout);

                var modelSurface = WoodTheme.CreateModelBoardSurface(
                    boardLayout, boardModel, boardSource, out float gridSpan, out float surfaceY);
                gridRoot = modelSurface.transform;
                if (gridSpan > 0f)
                {
                    boardLayout.RememberCalibratedGridSpan(gridSpan);
                    boardLayout.CalibrateFromBoardGridSpan(gridSpan);
                }
                if (surfaceY > 0f)
                    boardLayout.SetBoardSurfaceHeight(surfaceY);

                Transform placedModel = FindBoardModel(gridRoot);
                if (placedModel != null)
                {
                    WoodTheme.FineAlignBoardModel(placedModel.gameObject, boardLayout);
                    if (WoodTheme.TryMeasureNineteenByNineteenGridSpan(placedModel.gameObject, out float span))
                    {
                        boardLayout.RememberCalibratedGridSpan(span);
                        boardLayout.CalibrateFromBoardGridSpan(span);
                    }
                    if (WoodTheme.TryGetBoardSurfaceY(
                            placedModel.gameObject, boardLayout.Origin, out float alignedSurfaceY))
                        boardLayout.SetBoardSurfaceHeight(alignedSurfaceY);

                    WoodTheme.HideSquareBoardBaseAndEmbeddedStand(placedModel.gameObject);
                }

                if (boardLayout.UseModelBoard && PlayerStandLoader.IsAvailable())
                {
                    hostPlayerStand = WoodTheme.SpawnPlayerStand(boardLayout, boardTransform, Player.Host);
                    opponentPlayerStand = WoodTheme.SpawnPlayerStand(boardLayout, boardTransform, Player.Opponent);
                    RefreshPlayerStand(Player.Host);
                    RefreshPlayerStand(Player.Opponent);
                }
            }
            else if (boardLayout.UsePhotoBoard && BoardTextureLoader.IsAvailable())
            {
                Texture2D photo = BoardTextureLoader.Load();
                var photoSurface = WoodTheme.CreatePhotoBoardSurface(boardLayout, photo);
                gridRoot = photoSurface.transform;
                CreatePhotoRim(boardTransform);
            }
            else
            {
                var grid = WoodTheme.CreateBoardSurface(boardLayout);
                gridRoot = grid.transform;
            }

            if (gridRoot == null)
            {
                DebugLogger.LogWarning("BuildBoardVisuals finished without a board surface.");
                return;
            }

            gridRoot.SetParent(boardTransform, false);
        }

#if UNITY_EDITOR
        public void EditorRebuildVisuals()
        {
            usePrebuiltLayout = false;

            if (boardLayout == null)
                boardLayout = GetComponent<BoardLayout>();

            if (piecesRoot == null)
            {
                Transform existingPieces = transform.Find("Pieces");
                if (existingPieces != null)
                    piecesRoot = existingPieces;
            }

            if (prefabLookup.Count == 0)
                BuildPrefabLookup();

            if (pointsRoot != null)
            {
                DestroyImmediate(pointsRoot.gameObject);
                pointsRoot = null;
            }

            BuildBoardVisuals();
        }
#endif

        private void CreatePhotoRim(Transform boardTransform)
        {
            float boardDiameter = boardLayout.GetPhotoBoardWorldSize();
            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "BoardRim";
            rim.transform.SetParent(boardTransform, false);
            rim.transform.localScale = new Vector3(boardDiameter + 0.06f, 0.05f, boardDiameter + 0.06f);
            rim.transform.localPosition = new Vector3(0f, -0.024f, 0f);

            var collider = rim.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            WoodTheme.ApplyTexturedWood(rim.GetComponent<Renderer>(), "Scene/dark_oak", WoodTheme.FrameWood, 0.48f, 2.5f);
            rimRoot = rim.transform;
        }

        private GameObject CreateBoardPointObject(int coordinate)
        {
            GameObject pointObject;
            if (boardPointPrefab != null)
            {
                pointObject = Instantiate(boardPointPrefab, pointsRoot);
            }
            else
            {
                pointObject = new GameObject($"Point_{coordinate}");
                var collider = pointObject.AddComponent<BoxCollider>();
                collider.size = new Vector3(
                    boardLayout.CellSpacing * boardLayout.BoardPointColliderScale,
                    Mathf.Max(0.24f, boardLayout.CellSpacing * boardLayout.BoardPointColliderScale * 0.42f),
                    boardLayout.CellSpacing * boardLayout.BoardPointColliderScale);
                float pointHeight = collider.size.y;
                collider.center = new Vector3(0f, pointHeight * 0.42f, 0f);
                collider.isTrigger = false;
                pointObject.AddComponent<BoardPoint>();
            }

            var boardPoint = pointObject.GetComponent<BoardPoint>() ?? pointObject.AddComponent<BoardPoint>();
            boardPoint.Initialize(coordinate, boardLayout);

            return pointObject;
        }

        public BoardLayout GetBoardLayout() => boardLayout;

        public Transform GetBoardModelTransform()
        {
            return gridRoot != null ? FindBoardModel(gridRoot) : null;
        }

        public Transform GetPlayerStandTransform(Player player)
        {
            return player == Player.Host ? hostPlayerStand : opponentPlayerStand;
        }

        public void RefreshPlayerStand(Player player)
        {
            if (usePrebuiltLayout)
                return;

            Transform stand = GetPlayerStandTransform(player);
            if (stand == null || boardLayout == null)
                return;

            HandTrayTunerSettings tuner = null;
            if (HandTrayTuner.Instance != null && HandTrayTuner.Instance.IsPanelOpen)
                tuner = HandTrayTuner.Instance.Settings;

            WoodTheme.ApplyPlayerStandTuning(stand, boardLayout, player, tuner);
        }

        public void RefreshAllPlayerStands()
        {
            RefreshPlayerStand(Player.Host);
            RefreshPlayerStand(Player.Opponent);
        }

        public void RefreshAllBoardPoints(BoardPointTunerSettings settings)
        {
            if (pointsRoot == null || boardLayout == null)
                return;

            foreach (BoardPoint point in pointsRoot.GetComponentsInChildren<BoardPoint>(true))
                point.Refresh(boardLayout, settings);
        }

        public void RefreshAllPiecePositions()
        {
            foreach (Piece piece in piecesOnBoard.Values)
            {
                if (piece != null)
                    SeatPieceOnBoard(piece, piece.BoardCoordinate);
            }
        }

        public bool IsLegalPosition(int coordinate) => BoardUtils.IsValidPointCoordinate(coordinate);

        public Piece GetPieceAt(int coordinate)
        {
            piecesOnBoard.TryGetValue(coordinate, out var piece);
            return piece;
        }

        public List<Piece> GetAllPieces() => new List<Piece>(piecesOnBoard.Values);

        public Piece PlacePiece(Player owner, PieceType type, int coordinate, GameObject existingVisual = null, bool preserveWorldPose = false)
        {
            if (!BoardUtils.IsValidPointCoordinate(coordinate))
            {
                DebugLogger.LogWarning($"Tried to place {type} on invalid coordinate {coordinate}");
                return null;
            }

            if (piecesOnBoard.ContainsKey(coordinate))
            {
                DebugLogger.LogWarning($"Coordinate {coordinate} is already occupied.");
                return null;
            }

            if (piecesRoot == null)
            {
                var root = new GameObject("Pieces");
                root.transform.SetParent(transform, false);
                piecesRoot = root.transform;
            }

            bool fromPrefab;
            GameObject pieceObject = PreparePlacedVisual(type, existingVisual, out fromPrefab, preserveWorldPose);

            var piece = pieceObject.GetComponent<Piece>() ?? pieceObject.AddComponent<Piece>();
            piece.UsesPrefabVisual = fromPrefab;
            if (preserveWorldPose)
                piece.SetBoardYawDegrees(pieceObject.transform.eulerAngles.y);
            else
                piece.AssignRandomBoardYaw();
            piece.Configure(owner, type, coordinate, seatOnBoard: !preserveWorldPose);
            PieceStateAnimator.Ensure(piece)?.RefreshAfterBoardSeat();
            SeatPieceOnBoard(piece, coordinate, immediateVisual: !preserveWorldPose);

            piecesOnBoard[coordinate] = piece;
            RefreshHarmonyForPiece(piece);
            CaptureManager.Instance?.TryCapture(piece, coordinate);
            BloomingManager.Instance?.ApplyBloomVisualIfLotus(piece);

            DebugLogger.Log(
                $"Seated {type} at {coordinate} pos={pieceObject.transform.position} " +
                $"scale={pieceObject.transform.lossyScale} " +
                $"renderers={pieceObject.GetComponentsInChildren<Renderer>(true).Length}");

            return piece;
        }

        private GameObject PreparePlacedVisual(
            PieceType type,
            GameObject existingVisual,
            out bool fromPrefab,
            bool preserveWorldPose = false)
        {
            float cellSpacing = boardLayout != null ? boardLayout.CellSpacing : 0.42f;
            GameObject pieceObject;

            if (existingVisual != null)
            {
                // Keep the hand tile mesh that already renders correctly.
                pieceObject = existingVisual;
                fromPrefab = true;

                var handle = pieceObject.GetComponent<HandTileHandle>();
                if (handle != null)
                    Destroy(handle);

                pieceObject.transform.SetParent(piecesRoot, preserveWorldPose);
                if (!preserveWorldPose)
                {
                    pieceObject.transform.localRotation = Quaternion.identity;
                    pieceObject.transform.localScale = Vector3.one;
                    WoodTheme.FitPrefabToCellSpacing(pieceObject, cellSpacing, alignBottomToSurface: false);
                }
            }
            else
            {
                pieceObject = CreatePieceObject(type, out fromPrefab, alignBottomToSurface: false);
                pieceObject.transform.SetParent(piecesRoot, false);
                pieceObject.transform.localRotation = Quaternion.identity;
                // Refit after parenting so lossy scale matches the board cell size.
                pieceObject.transform.localScale = Vector3.one;
                WoodTheme.FitPrefabToCellSpacing(pieceObject, cellSpacing, alignBottomToSurface: false);
            }

            pieceObject.SetActive(true);
            pieceObject.name = $"Tile_{type}";
            foreach (Renderer renderer in pieceObject.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null)
                    renderer.enabled = true;
            }

            return pieceObject;
        }

        public void PlacePiece(int coordinate, Piece piece)
        {
            piecesOnBoard[coordinate] = piece;
            piece.SetCoordinate(coordinate);
            SeatPieceOnBoard(piece, coordinate);
        }

        public bool MovePiece(Piece piece, int toCoordinate)
        {
            if (piece == null || !BoardUtils.IsValidPointCoordinate(toCoordinate))
                return false;

            if (piecesOnBoard.ContainsKey(toCoordinate))
                return false;

            int fromCoordinate = piece.BoardCoordinate;

            if (piece.Type == PieceType.Boat)
                BoatManager.Instance?.OnBoatSeated(piece);

            piecesOnBoard.Remove(fromCoordinate);
            piecesOnBoard[toCoordinate] = piece;
            piece.SetCoordinate(toCoordinate);
            SeatPieceOnBoard(piece, toCoordinate);

            RefreshHarmonyForPiece(piece);
            return true;
        }

        /// <summary>
        /// Relocate a piece for AI search only — no seating, boats, or visuals.
        /// Caller must revert and restore harmony flags.
        /// </summary>
        public bool TryRelocateForSearch(Piece piece, int toCoordinate)
        {
            if (piece == null || !BoardUtils.IsValidPointCoordinate(toCoordinate))
                return false;

            if (piece.BoardCoordinate == toCoordinate)
                return true;

            if (piecesOnBoard.ContainsKey(toCoordinate))
                return false;

            int fromCoordinate = piece.BoardCoordinate;
            if (fromCoordinate >= 0)
                piecesOnBoard.Remove(fromCoordinate);

            piecesOnBoard[toCoordinate] = piece;
            piece.SetCoordinate(toCoordinate);
            return true;
        }

        /// <summary>Recompute InHarmony flags without animators, audio, or overlay refresh.</summary>
        public void RefreshHarmonyFlagsOnly()
        {
            foreach (Piece piece in piecesOnBoard.Values)
            {
                if (piece != null)
                    piece.InHarmony = false;
            }

            if (HarmonyManager.Instance == null)
                return;

            List<Piece> pieces = GetAllPieces();
            for (int i = 0; i < pieces.Count; i++)
            {
                for (int j = i + 1; j < pieces.Count; j++)
                {
                    if (HarmonyManager.Instance.IsHarmony(pieces[i], pieces[j]))
                    {
                        pieces[i].InHarmony = true;
                        pieces[j].InHarmony = true;
                    }
                }
            }
        }

        public Piece LiftPiece(Piece piece)
        {
            if (piece == null)
                return null;

            piecesOnBoard.Remove(piece.BoardCoordinate);
            piece.SetCoordinate(-1);
            return piece;
        }

        public Vector3 GetPieceWorldPosition(int coordinate)
        {
            if (boardLayout == null)
                return Vector3.zero;

            Vector3 point = boardLayout.GetSurfaceWorldPosition(coordinate);

            // Always prefer the live mesh top so tiles never sit under a raised scene board.
            Transform model = GetBoardModelTransform() ?? FindSceneBoardModel();
            if (model != null && WoodTheme.TryGetPlayableSurfaceWorldY(model.gameObject, out float worldSurfaceY))
                point.y = worldSurfaceY + boardLayout.PieceSurfaceLift;

            return point;
        }

        /// <summary>Apply the same scale, rotation, and surface seating used when a tile rests on the board.</summary>
        public void ApplyBoardSeatedVisual(GameObject pieceVisual, int coordinate, float boardYawDegrees = 0f)
        {
            if (boardLayout == null || pieceVisual == null)
                return;

            float cellSpacing = boardLayout.CellSpacing;
            pieceVisual.transform.localRotation = Quaternion.Euler(0f, boardYawDegrees, 0f);
            pieceVisual.transform.localScale = Vector3.one;
            WoodTheme.FitPrefabToCellSpacing(pieceVisual, cellSpacing, alignBottomToSurface: false);
            pieceVisual.transform.position = GetPieceWorldPosition(coordinate);
            WoodTheme.SeatOnWoodSurface(pieceVisual);
            WoodTheme.EnsurePiecePickCollider(pieceVisual, cellSpacing);
        }

        /// <summary>World pivot after seating the tile bottom on the wood surface.</summary>
        public Vector3 GetSeatedPieceWorldPosition(GameObject pieceVisual, int coordinate)
        {
            if (boardLayout == null || pieceVisual == null)
                return Vector3.zero;

            Transform root = pieceVisual.transform;
            Vector3 savedPosition = root.position;
            Quaternion savedRotation = root.rotation;
            Vector3 savedScale = root.localScale;
            ChildLocalPose[] savedChildPoses = CaptureDirectChildLocalPositions(root);

            ApplyBoardSeatedVisual(pieceVisual, coordinate, pieceVisual.GetComponent<Piece>()?.BoardYawDegrees ?? 0f);
            Vector3 seated = root.position;

            RestoreDirectChildLocalPositions(savedChildPoses);
            root.SetPositionAndRotation(savedPosition, savedRotation);
            root.localScale = savedScale;

            return seated;
        }

        private readonly struct ChildLocalPose
        {
            public readonly Transform Transform;
            public readonly Vector3 LocalPosition;

            public ChildLocalPose(Transform transform, Vector3 localPosition)
            {
                Transform = transform;
                LocalPosition = localPosition;
            }
        }

        private static ChildLocalPose[] CaptureDirectChildLocalPositions(Transform root)
        {
            int count = root.childCount;
            var poses = new ChildLocalPose[count];
            for (int i = 0; i < count; i++)
            {
                Transform child = root.GetChild(i);
                poses[i] = new ChildLocalPose(child, child.localPosition);
            }

            return poses;
        }

        private static void RestoreDirectChildLocalPositions(ChildLocalPose[] poses)
        {
            if (poses == null)
                return;

            foreach (ChildLocalPose pose in poses)
            {
                if (pose.Transform != null)
                    pose.Transform.localPosition = pose.LocalPosition;
            }
        }

        private void SeatPieceOnBoard(Piece piece, int coordinate, bool immediateVisual = true)
        {
            if (piece == null)
                return;

            Transform pieceTransform = piece.transform;
            if (piecesRoot != null && pieceTransform.parent != piecesRoot)
                pieceTransform.SetParent(piecesRoot, !immediateVisual);

            float cellSpacing = boardLayout != null ? boardLayout.CellSpacing : 0.42f;

            if (immediateVisual)
            {
                ApplyBoardSeatedVisual(piece.gameObject, coordinate, piece.BoardYawDegrees);
                if (piece.Type == PieceType.Boat)
                    BoatManager.Instance?.OnBoatSeated(piece);
            }
            else
                WoodTheme.EnsurePiecePickCollider(piece.gameObject, cellSpacing);
        }

        private bool TryGetBoardPointWorldPosition(int coordinate, out Vector3 position)
        {
            position = default;
            if (pointsRoot == null)
                return false;

            foreach (BoardPoint point in pointsRoot.GetComponentsInChildren<BoardPoint>(true))
            {
                if (point == null || point.Coordinate != coordinate)
                    continue;

                position = point.transform.position;
                return true;
            }

            return false;
        }

        public void RemovePiece(Piece piece)
        {
            if (piece == null)
                return;

            ReleasePieceFromBoard(piece);
            Destroy(piece.gameObject);
        }

        public void ReleasePieceFromBoard(Piece piece)
        {
            if (piece == null)
                return;

            int coordinate = piece.BoardCoordinate;
            if (piece.Type == PieceType.Boat)
                BoatManager.Instance?.ClearCargoForBoat(piece);

            if (coordinate >= 0)
                piecesOnBoard.Remove(coordinate);

            piece.SetCoordinate(-1);
        }

        public void ClearAllPieces()
        {
            // Abort travel/place coroutines before destroying tiles mid-animation.
            PieceFeedbackManager.Instance?.CancelAll();
            var pieces = new List<Piece>(piecesOnBoard.Values);
            foreach (Piece piece in pieces)
                RemovePiece(piece);
        }

        public bool TryResolveCoordinate(Ray ray, out int coordinate) =>
            TryResolveCoordinate(ray, Vector2.zero, out coordinate);

        public bool TryResolveCoordinate(Ray ray, Vector2 screenPosition, out int coordinate)
        {
            coordinate = -1;
            Camera camera = Camera.main;
            if (camera == null || boardLayout == null)
                return false;

            if (BoardPickUtility.TryResolveCoordinate(camera, ray, screenPosition, boardLayout, this, out coordinate))
                return true;

            // Mesh hit fallback when screen pick also misses.
            var hits = Physics.RaycastAll(ray, 200f);
            if (hits == null || hits.Length == 0)
                return false;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.GetComponentInParent<HandTileHandle>() != null)
                    continue;

                if (boardLayout.TryWorldToCoordinate(hit.point, out coordinate, BoardPickUtility.WorldSnapToleranceScale))
                    return true;
            }

            return false;
        }

        public GameObject CreateHandVisual(Player owner, PieceType type)
        {
            GameObject pieceObject = CreatePieceObject(
                type,
                out bool fromPrefab,
                alignBottomToSurface: false,
                liftFlowerFaces: false,
                skipCellSpacingFit: true);
            var piece = pieceObject.GetComponent<Piece>() ?? pieceObject.AddComponent<Piece>();
            piece.UsesPrefabVisual = fromPrefab;
            piece.Configure(owner, type, -1);

            // Life animation is board-only; a leftover animator would yank tray tiles to parent origin.
            var animator = pieceObject.GetComponent<PieceStateAnimator>();
            if (animator != null)
            {
                animator.enabled = false;
                Object.Destroy(animator);
            }

            return pieceObject;
        }

        public void RefreshAllHarmony()
        {
            var previousHarmony = new Dictionary<Piece, bool>();
            foreach (var piece in piecesOnBoard.Values)
            {
                if (piece == null)
                    continue;
                previousHarmony[piece] = piece.InHarmony;
                piece.InHarmony = false;
            }

            var pieces = GetAllPieces();
            for (int i = 0; i < pieces.Count; i++)
            {
                for (int j = i + 1; j < pieces.Count; j++)
                {
                    if (HarmonyManager.Instance == null)
                        continue;

                    if (HarmonyManager.Instance.IsHarmony(pieces[i], pieces[j]))
                    {
                        pieces[i].InHarmony = true;
                        pieces[j].InHarmony = true;
                    }
                }
            }

            bool anyHarmonyEntered = false;
            foreach (Piece piece in pieces)
            {
                bool wasInHarmony = previousHarmony.TryGetValue(piece, out bool prior) && prior;
                var animator = PieceStateAnimator.Ensure(piece);
                if (animator == null)
                    continue;

                if (!wasInHarmony && piece.InHarmony)
                {
                    animator.NotifyHarmonyEntered();
                    anyHarmonyEntered = true;
                }
                else if (wasInHarmony && !piece.InHarmony)
                {
                    animator.NotifyHarmonyExited();
                }

                // Always re-resolve so dormant pieces pick up InHarmony even if a prior
                // one-shot left lifeState matching by accident.
                animator.SyncFromPiece(immediate: false);
                if (piece.InHarmony)
                    animator.EnsureHarmonyPresentation();
            }

            if (anyHarmonyEntered)
                PieceFeedbackManager.Instance?.PlayHarmony();
        }

        private void RefreshHarmonyForPiece(Piece piece)
        {
            RefreshAllHarmony();
            GameplayVisualizer.Instance?.Refresh();
        }

        private GameObject CreatePieceObject(
            PieceType type,
            out bool fromPrefab,
            bool alignBottomToSurface = true,
            bool liftFlowerFaces = true,
            bool skipCellSpacingFit = false)
        {
            fromPrefab = false;
            if (!prefabLookup.TryGetValue(type, out GameObject visual) || visual == null)
            {
                DebugLogger.LogWarning($"No art loaded for {type}; using placeholder tile.");
                return WoodTheme.CreateWoodTileToken(type, Player.Host);
            }

            var instance = Instantiate(visual);
            instance.name = $"Tile_{type}";

            if (!HasRenderableGeometry(instance))
            {
                DebugLogger.LogWarning($"{type} art has no renderers; using placeholder tile.");
                Destroy(instance);
                return WoodTheme.CreateWoodTileToken(type, Player.Host);
            }

            fromPrefab = true;

            prefabSourcePaths.TryGetValue(type, out string sourcePath);
            string materialPath = sourcePath;
            if (string.IsNullOrEmpty(materialPath))
                materialPath = ResolveBlendFallbackPath(type);

            PieceMaterialUtility.EnsureMaterials(instance, materialPath, liftFlowerFaces: liftFlowerFaces);
            if (type == PieceType.Jasmine)
                WoodTheme.ApplyJasmineThicknessCorrection(instance);
            WoodTheme.NormalizeFlowerInlayNames(instance);
            float cellSpacing = boardLayout != null
                ? boardLayout.CellSpacing
                : 0.42f;
            if (skipCellSpacingFit)
            {
                // Hand-tray tiles are fitted after parenting in HandTrayController.
            }
            else
                WoodTheme.FitPrefabToCellSpacing(instance, cellSpacing, alignBottomToSurface);
            return instance;
        }

        private static string ResolveBlendFallbackPath(PieceType type)
        {
            return type switch
            {
                PieceType.Jasmine => "Assets/Models/Pieces/Tile_Jasmine.glb",
                PieceType.Rose => "Assets/BlenderFiles/Pieces/Tile_Rose.blend",
                PieceType.Lily => "Assets/BlenderFiles/Pieces/Tile_Lily.blend",
                PieceType.Jade => "Assets/BlenderFiles/Pieces/Tile_Jade.blend",
                PieceType.Chrysanthemum => "Assets/BlenderFiles/Pieces/Tile_Chrys.blend",
                PieceType.Rhododendron => "Assets/BlenderFiles/Pieces/Tile_Rhod.blend",
                PieceType.Boat => "Assets/BlenderFiles/Pieces/Tile_Boat.blend",
                PieceType.Rock => "Assets/BlenderFiles/Pieces/Tile_Rock.blend",
                PieceType.Knotweed => "Assets/BlenderFiles/Pieces/Tile_Knot.blend",
                PieceType.Wheel => "Assets/BlenderFiles/Pieces/Tile_Wheel.blend",
                PieceType.Lotus => "Assets/BlenderFiles/Pieces/Tile_Lotus.blend",
                PieceType.Orchid => "Assets/BlenderFiles/Pieces/Tile_Orchid.blend",
                _ => null
            };
        }

        private static bool HasRenderableGeometry(GameObject obj)
        {
            return PieceVisualLoader.HasRenderableGeometry(obj);
        }
    }
}
