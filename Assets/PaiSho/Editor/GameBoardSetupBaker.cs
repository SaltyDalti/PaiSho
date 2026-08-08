#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using PaiSho;
using PaiSho.Board;
using PaiSho.Game;
using PaiSho.Pieces;

namespace PaiSho.EditorTools
{
    /// <summary>
    /// Creates a scene-first board hierarchy using prefab instances so materials survive save/load.
    /// Position and tune everything in the Inspector — runtime will not overwrite transforms or materials.
    /// </summary>
    public static class GameBoardSetupBaker
    {
        private const string PrefabPath = "Assets/Prefabs/Game/GameBoardSetup.prefab";
        private const string BoardBlendPath = "Assets/BlenderFiles/Board/Board.blend";
        private const string BoardGlbPath = "Assets/Models/Board/Board.glb";
        private const string BoardPrefabPath = "Assets/Prefabs/Board/Board.prefab";

        /// <summary>Legacy alias — use Pai Sho &gt; Bake Hand Tray Tile Markers from HandTraySlotMarkerSetup.</summary>
        public static void BakeHandTrayTileMarkersFromMenu()
        {
            HandTraySlotMarkerSetup.BakeHandTrayTileMarkers();
        }

        [MenuItem("Pai Sho/Create Scene Board Template")]
        public static void CreateSceneTemplateFromMenu()
        {
            CreateSceneTemplate();
        }

        [MenuItem("Pai Sho/Bake Game Board Setup Prefab")]
        public static void BakeFromMenu()
        {
            Bake();
        }

        public static void BakeBatch()
        {
            Bake();
        }

        public static GameObject CreateSceneTemplate()
        {
            var root = BuildTemplateRoot();
            Selection.activeGameObject = root;
            Debug.Log(
                "Scene board template created. Position Board + stands + trays in the scene, " +
                "then save. Play mode will not rewrite materials or transforms (Preserve Scene Authored is on).");
            return root;
        }

        public static GameObject Bake()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath) ?? "Assets/Prefabs/Game");

            var root = BuildTemplateRoot();
            try
            {
                GameObject prefab = SaveOrUpdatePrefab(root);
                Debug.Log(
                    $"Saved scene board template to {PrefabPath}. " +
                    "Open the prefab, position visuals, save, then assign on PaiShoBootstrap.");
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject BuildTemplateRoot()
        {
            var root = new GameObject("GameBoardSetup");
            var layout = root.AddComponent<BoardLayout>();
            var manager = root.AddComponent<BoardManager>();
            var setup = root.AddComponent<GameBoardSetup>();

            var piecesRoot = new GameObject("Pieces");
            piecesRoot.transform.SetParent(root.transform, false);

            var boardSurface = new GameObject("BoardModelSurface");
            boardSurface.transform.SetParent(root.transform, false);
            InstantiateLinkedPrefab(ResolveBoardSourcePath(), boardSurface.transform, "Board");

            InstantiateLinkedPrefab(PlayerStandLoader.ModelAssetPath, root.transform, "PlayerStand");
            InstantiateLinkedPrefab(PlayerStandLoader.ModelAssetPath, root.transform, "PlayerStandOpponent");

            Transform hostStand = root.transform.Find("PlayerStand");
            Transform opponentStand = root.transform.Find("PlayerStandOpponent");
            Transform hostTray = hostStand != null
                ? HandTrayLayoutUtility.CreateBakedTray(
                    hostStand, layout, Player.Host, GameBoardSetup.HostTrayName, bakeSlotMarkers: true)
                : null;
            Transform opponentTray = opponentStand != null
                ? HandTrayLayoutUtility.CreateBakedTray(
                    opponentStand, layout, Player.Opponent, GameBoardSetup.OpponentTrayName, bakeSlotMarkers: true)
                : null;

            WireComponents(manager, layout, piecesRoot.transform, setup, hostTray, opponentTray);
            return root;
        }

        private static string ResolveBoardSourcePath()
        {
            // Prefer the rebuilt standalone prefab (materials baked into GLB-derived assets).
            // Then the GLB itself. Avoid Board.blend — Unity's blend importer drops textures.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BoardPrefabPath) != null)
                return BoardPrefabPath;

            if (AssetDatabase.LoadAssetAtPath<GameObject>(BoardGlbPath) != null)
                return BoardGlbPath;

            return BoardBlendPath;
        }

        private static GameObject InstantiateLinkedPrefab(string assetPath, Transform parent, string objectName)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (source == null)
            {
                Debug.LogWarning($"Could not load prefab/model at {assetPath}");
                return null;
            }

            var instance = PrefabUtility.InstantiatePrefab(source, parent) as GameObject;
            if (instance == null)
            {
                // glTFast assets sometimes need a plain instantiate.
                instance = Object.Instantiate(source, parent);
            }

            if (instance == null)
                return null;

            instance.name = objectName;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            return instance;
        }

        private static void WireComponents(
            BoardManager manager,
            BoardLayout layout,
            Transform piecesRoot,
            GameBoardSetup setup,
            Transform hostTray = null,
            Transform opponentTray = null)
        {
            var entries = new BoardManager.PiecePrefabEntry[]
            {
                Entry(PieceType.Jasmine, "Tile_Jasmine"),
                Entry(PieceType.Rose, "Tile_Rose"),
                Entry(PieceType.Lily, "Tile_Lily"),
                Entry(PieceType.Jade, "Tile_Jade"),
                Entry(PieceType.Chrysanthemum, "Tile_Chrys"),
                Entry(PieceType.Rhododendron, "Tile_Rhod"),
                Entry(PieceType.Boat, "Tile_Boat"),
                Entry(PieceType.Rock, "Tile_Rock"),
                Entry(PieceType.Knotweed, "Tile_Knot"),
                Entry(PieceType.Wheel, "Tile_Wheel"),
                Entry(PieceType.Lotus, "Tile_Lotus"),
                Entry(PieceType.Orchid, "Tile_Orchid"),
            };

            SerializedObject managerObject = new SerializedObject(manager);
            managerObject.FindProperty("boardLayout").objectReferenceValue = layout;
            managerObject.FindProperty("piecesRoot").objectReferenceValue = piecesRoot;
            managerObject.FindProperty("usePrebuiltLayout").boolValue = true;
            managerObject.FindProperty("preserveSceneAuthored").boolValue = true;
            SerializedProperty piecePrefabs = managerObject.FindProperty("piecePrefabs");
            piecePrefabs.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                SerializedProperty element = piecePrefabs.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Type").enumValueIndex = (int)entries[i].Type;
                element.FindPropertyRelative("Prefab").objectReferenceValue = entries[i].Prefab;
            }

            managerObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject setupObject = new SerializedObject(setup);
            setupObject.FindProperty("usePrebuiltLayout").boolValue = true;
            setupObject.FindProperty("preserveSceneAuthored").boolValue = true;
            setupObject.FindProperty("hostTrayRoot").objectReferenceValue = hostTray;
            setupObject.FindProperty("opponentTrayRoot").objectReferenceValue = opponentTray;
            setupObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static BoardManager.PiecePrefabEntry Entry(PieceType type, string prefabName)
        {
            string path = $"Assets/Prefabs/Pieces/{prefabName}.prefab";
            return new BoardManager.PiecePrefabEntry
            {
                Type = type,
                Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path)
            };
        }

        private static GameObject SaveOrUpdatePrefab(GameObject root)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
                return PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);

            return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
    }
}
#endif
