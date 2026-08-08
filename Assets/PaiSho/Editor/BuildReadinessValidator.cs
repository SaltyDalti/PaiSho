#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using PaiSho;
using PaiSho.Board;
using PaiSho.Game;

namespace PaiSho.EditorTools
{
    public static class BuildReadinessValidator
    {
        private const string GameBoardSetupPath = "Assets/Prefabs/Game/GameBoardSetup.prefab";
        private const string GamePlayScenePath = "Assets/Scenes/GamePlay.unity";

        [MenuItem("Pai Sho/Validate Player Build Readiness")]
        public static void ValidateFromMenu()
        {
            var report = Validate();
            if (report.Issues.Count == 0)
            {
                Debug.Log("Pai Sho build readiness: all checks passed.");
                return;
            }

            var message = new StringBuilder("Pai Sho build readiness issues:\n");
            foreach (string issue in report.Issues)
                message.AppendLine($" - {issue}");

            Debug.LogWarning(message.ToString());
        }

        [MenuItem("Pai Sho/Smoke Test Resources Loading")]
        public static void SmokeTestResourcesFromMenu()
        {
            var report = SmokeTestResources();
            if (report.Issues.Count == 0)
            {
                Debug.Log("Pai Sho Resources smoke test: all loads succeeded.");
                return;
            }

            var message = new StringBuilder("Pai Sho Resources smoke test failures:\n");
            foreach (string issue in report.Issues)
                message.AppendLine($" - {issue}");

            Debug.LogWarning(message.ToString());
        }

        public static ValidationReport Validate()
        {
            var report = new ValidationReport();

            if (!File.Exists("Assets/Resources/Board/Board.glb"))
                report.Issues.Add("Missing Assets/Resources/Board/Board.glb");

            if (!File.Exists("Assets/Resources/Board/PlayerStand.glb"))
                report.Issues.Add("Missing Assets/Resources/Board/PlayerStand.glb");

            string[] pieceNames =
            {
                "Tile_Jasmine", "Tile_Rose", "Tile_Lily", "Tile_Jade", "Tile_Chrys", "Tile_Rhod",
                "Tile_Boat", "Tile_Rock", "Tile_Knot", "Tile_Wheel", "Tile_Lotus", "Tile_Orchid"
            };

            foreach (string piece in pieceNames)
            {
                if (!File.Exists($"Assets/Resources/PieceVisuals/{piece}.glb"))
                    report.Issues.Add($"Missing Assets/Resources/PieceVisuals/{piece}.glb");
            }

            if (!File.Exists(GameBoardSetupPath))
                report.Issues.Add($"Missing baked board prefab at {GameBoardSetupPath} — run Pai Sho → Bake Game Board Setup Prefab");

            ValidateBuildScenes(report);
            ValidateSceneWiring(report);
            ValidatePrefabTraysAndPots(report);

            if (!PlayerStandLoader.IsAvailable())
                report.Issues.Add("PlayerStandLoader could not resolve a stand model");

            if (!BoardVisualLoader.IsModelAvailable())
                report.Issues.Add("BoardVisualLoader could not resolve a board model");

            foreach (string issue in SmokeTestResources().Issues)
                report.Issues.Add(issue);

            return report;
        }

        public static ValidationReport SmokeTestResources()
        {
            var report = new ValidationReport();

            // Prefer loader APIs used at runtime over AssetDatabase paths.
            if (!BoardVisualLoader.IsModelAvailable())
                report.Issues.Add("Resources smoke: BoardVisualLoader failed (Board.glb / board model)");

            if (!PlayerStandLoader.IsAvailable())
                report.Issues.Add("Resources smoke: PlayerStandLoader failed (PlayerStand.glb)");

            Object boardAsset = Resources.Load("Board/Board");
            if (boardAsset == null)
                report.Issues.Add("Resources.Load(\"Board/Board\") returned null");

            Object standAsset = Resources.Load("Board/PlayerStand");
            if (standAsset == null)
                report.Issues.Add("Resources.Load(\"Board/PlayerStand\") returned null");

            string[] probePieces = { "Tile_Jasmine", "Tile_Rose", "Tile_Boat", "Tile_Wheel" };
            foreach (string piece in probePieces)
            {
                Object visual = Resources.Load($"PieceVisuals/{piece}");
                if (visual == null)
                    report.Issues.Add($"Resources.Load(\"PieceVisuals/{piece}\") returned null");
            }

            return report;
        }

        private static void ValidateBuildScenes(ValidationReport report)
        {
            bool hasGamePlay = false;
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled)
                    continue;

                if (scene.path == GamePlayScenePath)
                    hasGamePlay = true;
                else if (scene.path.Contains("SampleScene"))
                    report.Issues.Add($"SampleScene is still enabled in Build Settings: {scene.path}");
                else
                    report.Issues.Add($"Unexpected enabled build scene: {scene.path}");
            }

            if (!hasGamePlay)
                report.Issues.Add($"Build Settings must include enabled {GamePlayScenePath}");
        }

        private static void ValidateSceneWiring(ValidationReport report)
        {
            var bootstrap = Object.FindAnyObjectByType<GameBootstrap>();
            var setup = Object.FindAnyObjectByType<GameBoardSetup>();

            if (bootstrap == null)
            {
                report.Issues.Add("No GameBootstrap in the open scene");
            }
            else
            {
                SerializedObject bootstrapObject = new SerializedObject(bootstrap);
                Object prefab = bootstrapObject.FindProperty("gameBoardSetupPrefab").objectReferenceValue;
                if (prefab == null)
                    report.Issues.Add("GameBootstrap.gameBoardSetupPrefab is not assigned (needed for tray/pot restore in player builds)");
            }

            if (setup == null)
            {
                report.Issues.Add("No GameBoardSetup in the open scene");
                return;
            }

            setup.DiscoverTrayReferences();
            setup.DiscoverCapturePotReferences();

            if (!setup.HasHandTrayRoots())
                report.Issues.Add("Scene GameBoardSetup is missing hand trays — run Pai Sho > Hand Tray > Sync Scene Instance From Prefab");
            else
            {
                if (!GameBoardSetup.HasBakedSlotMarkers(setup.HostTrayRoot))
                    report.Issues.Add("Host hand tray is missing baked Slot_ markers");
                if (!GameBoardSetup.HasBakedSlotMarkers(setup.OpponentTrayRoot))
                    report.Issues.Add("Opponent hand tray is missing baked Slot_ markers");
            }

            if (!setup.HasBakedCapturePotMarkers())
                report.Issues.Add("Scene GameBoardSetup is missing capture pot stack markers — run Pai Sho > Capture Pot > Sync Scene Instance From Prefab");
        }

        private static void ValidatePrefabTraysAndPots(ValidationReport report)
        {
            var prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(GameBoardSetupPath);
            if (prefabRoot == null)
                return;

            var setup = prefabRoot.GetComponent<GameBoardSetup>();
            if (setup == null)
            {
                report.Issues.Add($"{GameBoardSetupPath} has no GameBoardSetup component");
                return;
            }

            setup.DiscoverTrayReferences();
            setup.DiscoverCapturePotReferences();

            if (!setup.HasHandTrayRoots())
                report.Issues.Add("GameBoardSetup prefab is missing hand trays");
            if (!setup.HasBakedCapturePotMarkers())
                report.Issues.Add("GameBoardSetup prefab is missing capture pot markers");
        }

        public sealed class ValidationReport
        {
            public System.Collections.Generic.List<string> Issues { get; } = new();
            public bool IsReady => Issues.Count == 0;
        }
    }
}
#endif
