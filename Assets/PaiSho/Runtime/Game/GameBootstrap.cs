using System.Collections;
using UnityEngine;
using PaiSho;
using PaiSho.Board;

namespace PaiSho.Game
{
    public class GameBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateIfNeeded()
        {
            if (FindAnyObjectByType<GameBootstrap>() != null)
                return;

            var bootstrapObject = new GameObject("PaiShoGame");
            bootstrapObject.AddComponent<GameBootstrap>();
        }

        [SerializeField] private Transform boardParent;
        [SerializeField] private GameBoardSetup gameBoardSetupPrefab;

        private void Awake()
        {
            EnsureManager<GameStateManager>();
            EnsureManager<GameManager>();
            EnsureManager<SeasonManager>();
            EnsureManager<ReserveManager>();
            EnsureManager<PlacementValidator>();
            EnsureManager<TileSelector>();
            EnsureManager<HarmonyManager>();
            EnsureManager<CaptureManager>();
            EnsureManager<PotManager>();
            EnsureManager<PotVisualManager>();
            EnsureManager<BloomingManager>();
            EnsureManager<MovementManager>();
            EnsureManager<MomentumManager>();
            EnsureManager<ScoringManager>();
            EnsureManager<VictoryManager>();
            EnsureManager<GameEndManager>();
            EnsureManager<TileLifecycleManager>();
            EnsureManager<EchoTileManager>();
            EnsureManager<WheelRotationManager>();
            EnsureManager<BoatManager>();
            EnsureManager<KnotweedManager>();
            EnsureManager<PieceFeedbackManager>();
            EnsureManager<UiAudio>();
            EnsureManager<GameplayVisualizer>();
            EnsureManager<PieceCaptureManager>();
            EnsureManager<GameLogManager>();
            EnsureManager<GameSummaryManager>();
            EnsureManager<MaterialManager>();
            EnsureManager<AiController>();
            EnsureManager<GameInputController>();
            EnsureManager<LegalMoveHighlighter>();
            EnsureManager<GameUI>();
            EnsureManager<GameCoach>();
            EnsureManager<HandTrayController>();
            EnsureManager<BoardPieceDragController>();
            EnsureManager<TitleMenu>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            EnsureManager<BoardPointTuner>();
            EnsureManager<HandTrayTuner>();
            EnsureManager<CapturePotDebugger>();
#endif
        }

        private IEnumerator Start()
        {
            GameSession.ApplyAudio();
            // Reserves must exist before board init refreshes trays / HUD.
            ReserveManager.Instance.InitializeDefaultReserves();
            SetupBoard();
            PotVisualManager.Instance?.InitializeAnchors();
            FrameCameraOnBoard();
            HandTrayController.Instance?.Refresh();
            GameManager.Instance.RefreshLiveScores();

            yield return TitleMenu.WaitUntilMatchRequested();

            if (AiController.Instance != null)
                AiController.Instance.SetAiEnabled(GameSession.AiEnabled);

            AiStudyLibrary.EnsureLoaded();

            ReserveManager.Instance.PrepareSpringTurn(GameManager.Instance.GetCurrentPlayer());
            MovementManager.Instance.StartTurn();
            GameLogManager.Instance?.ClearEntries();
            AiPlanMemory.Clear();
            HandTrayController.Instance?.Refresh();
            GameManager.Instance.RefreshLiveScores();
            DebugLogger.Log("--- New match ---");
            DebugLogger.Log(
                GameSession.AiEnabled
                    ? "Pai Sho ready. Spring: random flower draws - place on your side. Opponent is AI."
                    : "Pai Sho ready. Hotseat: both players share this device.");
            AiController.Instance?.TryPlayTurn();
        }

        private void SetupBoard()
        {
            Transform parent = boardParent != null ? boardParent : transform;

            var existingSetup = FindAnyObjectByType<GameBoardSetup>();
            if (existingSetup != null)
            {
                // Bind stands first — hand trays parent under PlayerStand / PlayerStandOpponent.
                existingSetup.InitializeBoard();
                EnsureHandTrayRoots(existingSetup);
                EnsureCapturePotRoots(existingSetup);
                EnsureTableVisual(existingSetup.gameObject);
                SceneEnvironmentBuilder.Build(existingSetup.transform, existingSetup.GetComponent<BoardLayout>());
                return;
            }

            if (gameBoardSetupPrefab != null)
            {
                var instance = Instantiate(gameBoardSetupPrefab, parent);
                instance.InitializeBoard();
                EnsureHandTrayRoots(instance);
                EnsureCapturePotRoots(instance);
                EnsureTableVisual(instance.gameObject);
                SceneEnvironmentBuilder.Build(instance.transform, instance.GetComponent<BoardLayout>());
                return;
            }

            var boardObject = new GameObject("Board");
            boardObject.transform.SetParent(parent, false);

            var layout = boardObject.AddComponent<BoardLayout>();
            var boardManager = boardObject.AddComponent<BoardManager>();

            var piecesRoot = new GameObject("Pieces").transform;
            piecesRoot.SetParent(boardObject.transform, false);
            boardManager.Initialize(layout, piecesRoot);

            EnsureTableVisual(boardObject);
            SceneEnvironmentBuilder.Build(boardObject.transform, layout);
        }

        private void EnsureHandTrayRoots(GameBoardSetup setup)
        {
            setup.EnsureHandTrayRoots(gameBoardSetupPrefab);
        }

        private void EnsureCapturePotRoots(GameBoardSetup setup)
        {
            setup.EnsureCapturePotRoots(gameBoardSetupPrefab);
        }

        private static void EnsureTableVisual(GameObject boardObject)
        {
            if (boardObject.transform.Find("TableWood") != null)
                return;

            // Scene-authored 3D boards already include their own base mesh — skip the runtime plane.
            if (boardObject.transform.Find("BoardModelSurface") != null)
                return;

            var layout = boardObject.GetComponent<BoardLayout>();
            if (layout == null)
                return;

            AddProceduralBoardVisual(boardObject, layout);
        }

        private static void AddProceduralBoardVisual(GameObject boardObject, BoardLayout layout)
        {
            float gridSpan = layout.GridSpan;
            float photoCoverage = layout.PhotoGridCoverage;
            float boardDiameter = gridSpan / photoCoverage;

            var table = GameObject.CreatePrimitive(PrimitiveType.Plane);
            table.name = "TableWood";
            table.transform.SetParent(boardObject.transform, false);
            float tableScale = (boardDiameter * 1.35f) / 10f;
            table.transform.localScale = new Vector3(tableScale, 1f, tableScale);
            table.transform.localPosition = new Vector3(0f, -0.04f, 0f);
            DestroyCollider(table);
            WoodTheme.ApplyTexturedWood(table.GetComponent<Renderer>(), "Scene/dark_wood", WoodTheme.TableWood, 0.42f, 3f);

            if (BoardVisualLoader.IsModelAvailable() || BoardTextureLoader.IsAvailable())
                return;

            var fallbackRim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fallbackRim.name = "BoardRim";
            fallbackRim.transform.SetParent(boardObject.transform, false);
            fallbackRim.transform.localScale = new Vector3(boardDiameter + 0.12f, 0.055f, boardDiameter + 0.12f);
            fallbackRim.transform.localPosition = new Vector3(0f, -0.022f, 0f);
            DestroyCollider(fallbackRim);
            WoodTheme.ApplyWood(fallbackRim.GetComponent<Renderer>(), WoodTheme.FrameWood, 0.48f);

            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "BoardDisc";
            disc.transform.SetParent(boardObject.transform, false);
            disc.transform.localScale = new Vector3(boardDiameter, 0.028f, boardDiameter);
            disc.transform.localPosition = new Vector3(0f, -0.014f, 0f);
            DestroyCollider(disc);
            WoodTheme.ApplyWood(disc.GetComponent<Renderer>(), WoodTheme.BoardBaseWood, 0.36f);
        }

        private static void DestroyCollider(GameObject obj)
        {
            var collider = obj.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);
        }

        private static void FrameCameraOnBoard()
        {
            var camera = Camera.main;
            var layout = FindAnyObjectByType<BoardLayout>();
            if (camera == null || layout == null)
                return;

            var controller = camera.GetComponent<BoardCameraController>();
            if (controller == null)
                controller = camera.gameObject.AddComponent<BoardCameraController>();

            GameplayCameraFraming.ApplyTo(controller, camera, layout);
        }

        private static void EnsureManager<T>() where T : Component
        {
            if (FindAnyObjectByType<T>() != null)
                return;

            var managers = GameObject.Find("PaiShoManagers");
            if (managers == null)
                managers = new GameObject("PaiShoManagers");

            managers.AddComponent<T>();
        }
    }
}
