using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using PaiSho;
using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    /// <summary>3D hand racks — tiles sit on mirrored player stands at each gate.</summary>
    public class HandTrayController : MonoBehaviour
    {
        public static HandTrayController Instance;

        private const float DragLerpSpeed = 20f;
        private const float SnapBackDuration = 0.26f;

        private sealed class TrayBinding
        {
            public Player Owner;
            public Transform Root;
            public Transform TilesRoot;
            public readonly List<HandTileHandle> Handles = new();
            public WoodTheme.PlayerStandAnchor StandAnchor;
            public bool HasStandAnchor;
            public float DragSurfaceY;
            public Plane DragPlane;
        }

        private BoardLayout layout;
        private GameBoardSetup boardSetup;
        private readonly TrayBinding hostTray = new() { Owner = Player.Host };
        private readonly TrayBinding opponentTray = new() { Owner = Player.Opponent };
        private HandTileHandle activeDrag;
        private HandTrayTunerSettings activeTuner;
        private bool tunerPreviewActive;
        private bool isSnappingBack;
        private Coroutine snapBackRoutine;
        private Vector3 dragTargetPosition;
        private Quaternion dragTargetRotation;
        private int? hoverCoordinate;
        private DragPiecePolish trayDragPolish;

        public bool IsDragging => activeDrag != null || isSnappingBack;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                enabled = false;
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureTrayRoots();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void EnsureTrayRoots()
        {
            var setup = FindAnyObjectByType<GameBoardSetup>();
            if (setup != null)
            {
                boardSetup = setup;
                setup.EnsureHandTrayRoots();
                setup.DiscoverTrayReferences();
            }

            Transform previousHost = hostTray.Root;
            Transform previousOpponent = opponentTray.Root;

            hostTray.Root = ResolveLiveTrayRoot(setup, Player.Host, hostTray.Root);
            opponentTray.Root = ResolveLiveTrayRoot(setup, Player.Opponent, opponentTray.Root);

            EnsureTilesRoot(hostTray);
            EnsureTilesRoot(opponentTray);

            bool rootChanged =
                (previousHost != null && previousHost != hostTray.Root) ||
                (previousOpponent != null && previousOpponent != opponentTray.Root);

            if (!rootChanged)
                return;

            if (previousHost != null && previousHost != hostTray.Root)
                DestroyOrphanTray(previousHost, hostTray);
            if (previousOpponent != null && previousOpponent != opponentTray.Root)
                DestroyOrphanTray(previousOpponent, opponentTray);

            ClearTray(hostTray);
            ClearTray(opponentTray);

            // Re-seat immediately — waiting for an external Refresh leaves an empty rack.
            if (layout != null && BoardManager.Instance != null)
            {
                ReconcileTray(hostTray);
                ReconcileTray(opponentTray);
            }
        }

        private static void DestroyOrphanTray(Transform orphan, TrayBinding liveTray)
        {
            if (orphan == null || orphan == liveTray.Root)
                return;

            // Only kill runtime placeholders, never scene-authored stands trays.
            if (orphan.parent != null && orphan.GetComponentInParent<GameBoardSetup>() != null &&
                GameBoardSetup.HasBakedSlotMarkers(orphan))
                return;

            Object.Destroy(orphan.gameObject);
        }

        private Transform ResolveLiveTrayRoot(GameBoardSetup setup, Player player, Transform current)
        {
            string trayName = player == Player.Host
                ? GameBoardSetup.HostTrayName
                : GameBoardSetup.OpponentTrayName;

            Transform authored = player == Player.Host ? setup?.HostTrayRoot : setup?.OpponentTrayRoot;
            if (authored != null && authored.gameObject.scene.IsValid())
            {
                if (current != null && current != authored)
                {
                    bool placeholder = current.GetComponentInParent<GameBoardSetup>() == null
                        || !GameBoardSetup.HasBakedSlotMarkers(current);
                    if (placeholder)
                        Object.Destroy(current.gameObject);
                }

                return authored;
            }

            // Search the loaded scene before creating a blank tray at the origin.
            var named = GameObject.Find(trayName);
            if (named != null && named.scene.IsValid())
                return named.transform;

            if (current != null && current.name == trayName)
                return current;

            // Prefer baking onto the player stand when board setup exists.
            BoardLayout bakeLayout = layout != null ? layout : FindAnyObjectByType<BoardLayout>();
            if (setup != null && bakeLayout != null)
            {
                Transform stand = BoardManager.Instance != null
                    ? BoardManager.Instance.GetPlayerStandTransform(player)
                    : null;
                if (stand == null)
                {
                    string standName = player == Player.Host ? "PlayerStand" : "PlayerStandOpponent";
                    var standObject = GameObject.Find(standName);
                    stand = standObject != null ? standObject.transform : null;
                }

                Transform baked = HandTrayLayoutUtility.CreateBakedTray(
                    stand, bakeLayout, player, trayName, bakeSlotMarkers: true);
                if (baked != null)
                    return baked;
            }

            if (setup != null)
                return current;

            var created = new GameObject(trayName).transform;
            return created;
        }

        private void EnsureTilesRoot(TrayBinding tray)
        {
            if (tray.Root == null)
                return;

            string tilesRootName = tray.Owner == Player.Host ? "HostHandTiles" : "OpponentHandTiles";

            // Prefer a tiles root under the tray so local-zero is on the rack, not board center.
            Transform underTray = tray.Root.Find(tilesRootName);
            if (underTray != null)
            {
                tray.TilesRoot = underTray;
                return;
            }

            if (boardSetup == null)
                boardSetup = FindAnyObjectByType<GameBoardSetup>();

            // Migrate legacy board-root tile folders onto the tray.
            if (boardSetup != null)
            {
                Transform legacy = boardSetup.transform.Find(tilesRootName);
                if (legacy != null)
                {
                    legacy.SetParent(tray.Root, true);
                    legacy.localPosition = Vector3.zero;
                    legacy.localRotation = Quaternion.identity;
                    tray.TilesRoot = legacy;
                    return;
                }
            }

            if (tray.TilesRoot != null && tray.TilesRoot.parent == tray.Root)
                return;

            var tilesRoot = new GameObject(tilesRootName);
            tilesRoot.transform.SetParent(tray.Root, false);
            tilesRoot.transform.localPosition = Vector3.zero;
            tilesRoot.transform.localRotation = Quaternion.identity;
            tilesRoot.transform.localScale = Vector3.one;
            tray.TilesRoot = tilesRoot.transform;
        }

        private void Update()
        {
            if (Instance != this)
                return;

            if (TitleMenu.Instance != null && TitleMenu.Instance.IsOpen)
                return;

            if (GameUI.IsPassScrimShowing)
                return;

            EnsureTrayRoots();
            RefreshTrayVisibility(hostTray);
            RefreshTrayVisibility(opponentTray);

            if (layout == null)
                layout = FindAnyObjectByType<BoardLayout>();

            if (boardSetup == null)
                boardSetup = FindAnyObjectByType<GameBoardSetup>();

            if (!tunerPreviewActive)
                HandlePointer();

            if (activeDrag != null)
                ApplyDragVisual();
        }

        public void ApplyTunerSettings(HandTrayTunerSettings settings, bool previewActive)
        {
            activeTuner = previewActive ? settings : null;
            tunerPreviewActive = previewActive;
            Refresh();
        }

        public void Refresh()
        {
            if (Instance != this)
                return;

            EnsureTrayRoots();
            CancelDrag();

            if (layout == null)
                layout = FindAnyObjectByType<BoardLayout>();

            if (boardSetup == null)
                boardSetup = FindAnyObjectByType<GameBoardSetup>();

            if (layout == null || BoardManager.Instance == null)
                return;

            if (tunerPreviewActive)
                BoardManager.Instance.RefreshAllPlayerStands();

            ReconcileTray(hostTray);
            ReconcileTray(opponentTray);
        }

        /// <summary>World-space bounds of a player's hand rack (tiles, slot markers, or stand).</summary>
        public bool TryGetPlayerTrayWorldBounds(Player player, out Bounds bounds)
        {
            TrayBinding tray = player == Player.Host ? hostTray : opponentTray;
            bounds = default;

            if (tray.Root == null)
                return false;

            bool any = false;
            float pad = layout != null ? layout.CellSpacing * 1.15f : 0.45f;
            Bounds working = default;

            foreach (HandTileHandle handle in tray.Handles)
            {
                if (handle == null)
                    continue;

                if (!any)
                {
                    working = new Bounds(handle.RestPosition, Vector3.one * 0.05f);
                    any = true;
                }
                else
                {
                    working.Encapsulate(handle.RestPosition);
                }
            }

            if (!any && tray.Root != null)
            {
                for (int i = 0; i < HandTrayAlignmentDefaults.MaxSlots; i++)
                {
                    if (!GameBoardSetup.TryGetSlotMarker(tray.Root, i, out Transform slot))
                        continue;

                    if (!any)
                    {
                        working = new Bounds(slot.position, Vector3.one * 0.05f);
                        any = true;
                    }
                    else
                    {
                        working.Encapsulate(slot.position);
                    }
                }
            }

            if (!any)
                working = new Bounds(tray.Root.position, Vector3.one * 0.05f);

            working.Expand(new Vector3(pad, pad * 0.45f, pad));
            bounds = working;
            return true;
        }

        private void ReconcileTray(TrayBinding tray)
        {
            if (tray.Root == null || ShouldHideTray(tray.Owner))
                return;

            if (!PositionTrayOnPlayerStand(tray))
                PositionTrayFallback(tray);

            ApplyTrayTunerOffset(tray);

            if (tunerPreviewActive && activeTuner != null && activeTuner.previewAllSlots &&
                tray.Owner == activeTuner.editingPlayer)
            {
                ClearTray(tray);
                PopulatePreviewSlots(tray);
                return;
            }

            List<PieceType> expected = GetExpectedHandTiles(tray.Owner);
            if (HandMatchesTray(tray, expected))
                return;

            ClearTray(tray);
            if (expected.Count > 0)
                PopulateTilesAtSlots(tray, expected);
        }

        private List<PieceType> GetExpectedHandTiles(Player player)
        {
            var tiles = new List<PieceType>();
            if (ReserveManager.Instance == null)
                return tiles;

            if (GameStateManager.Instance != null && GameStateManager.Instance.IsSpringPhase())
            {
                PieceType? drawn = ReserveManager.Instance.GetSpringDrawnFlower(player);
                if (drawn.HasValue)
                    tiles.Add(drawn.Value);
            }
            else
            {
                tiles.AddRange(ReserveManager.Instance.GetHand(player));
            }

            return tiles;
        }

        private static bool HandMatchesTray(TrayBinding tray, List<PieceType> expected)
        {
            if (tray.Handles.Count != expected.Count)
                return false;

            for (int i = 0; i < expected.Count; i++)
            {
                if (tray.Handles[i] == null || tray.Handles[i].PieceType != expected[i])
                    return false;
            }

            return true;
        }

        private void RefreshTrayVisibility(TrayBinding tray)
        {
            if (tray.Root == null)
                return;

            bool hide = ShouldHideTray(tray.Owner);
            if (tray.Root.gameObject.activeSelf == hide)
                tray.Root.gameObject.SetActive(!hide);

            if (hide && activeDrag != null && tray.Handles.Contains(activeDrag))
                CancelDrag();
        }

        private bool ShouldHideTray(Player player)
        {
            if (tunerPreviewActive && activeTuner != null)
                return player != activeTuner.editingPlayer;

            if (GameStateManager.Instance != null && GameStateManager.Instance.IsEndPhase())
                return true;

            if (GameManager.Instance == null)
                return true;

            // Hotseat: keep hands secret between passes — hide whoever isn't currently up.
            if (AiController.Instance != null && !AiController.Instance.IsAiEnabled &&
                GameManager.Instance.GetCurrentPlayer() != player)
                return true;

            return false;
        }

        private bool CanInteractWithTray(Player player)
        {
            if (!CanInteractWithTrayVisibility(player))
                return false;

            if (GameManager.Instance == null || player != GameManager.Instance.GetCurrentPlayer())
                return false;

            // AI matches: the AI's own rack is never player-draggable.
            return AiController.Instance == null || !AiController.Instance.IsAiPlayer(player);
        }

        private bool CanInteractWithTrayVisibility(Player player)
        {
            return !ShouldHideTray(player) && !tunerPreviewActive;
        }

        /// <summary>Whichever player's rack is currently draggable (current human turn), or null.</summary>
        private TrayBinding GetActiveTray()
        {
            if (GameManager.Instance == null)
                return null;

            Player current = GameManager.Instance.GetCurrentPlayer();
            if (!CanInteractWithTray(current))
                return null;

            return current == Player.Host ? hostTray : opponentTray;
        }

        private bool PositionTrayOnPlayerStand(TrayBinding tray)
        {
            tray.HasStandAnchor = false;
            if (tray.Root == null)
                return false;

            Transform standTransform = BoardManager.Instance?.GetPlayerStandTransform(tray.Owner);
            if (standTransform == null)
                return false;

            // Scene-authored trays under the stand keep their baked pose.
            if (UsesSceneSlotMarkers(tray) || IsSceneAuthoredTray(tray))
                return BindStandAnchorFromSceneTray(tray, standTransform);

            if (!WoodTheme.TryGetPlayerStandAnchor(standTransform.gameObject, tray.Owner, out tray.StandAnchor))
                return false;

            tray.HasStandAnchor = true;
            Transform stand = tray.StandAnchor.StandTransform;
            Bounds localBounds = tray.StandAnchor.LocalBounds;

            tray.Root.SetParent(stand, false);
            Vector3 topCenter = new Vector3(
                localBounds.center.x,
                localBounds.max.y + layout.PieceSurfaceLift + 0.01f,
                localBounds.center.z);
            tray.Root.localPosition = topCenter;
            tray.Root.localRotation = Quaternion.identity;

            tray.DragSurfaceY = tray.Root.position.y + 0.05f;
            tray.DragPlane = new Plane(Vector3.up, new Vector3(0f, tray.DragSurfaceY, 0f));
            return true;
        }

        private void ApplyTrayTunerOffset(TrayBinding tray)
        {
            if (tray.Root == null || UsesSceneSlotMarkers(tray) || IsSceneAuthoredTray(tray))
                return;

            Vector3 offset;
            Vector3 euler;
            if (tunerPreviewActive && activeTuner != null)
            {
                offset = activeTuner.TrayLocalOffsetFor(tray.Owner);
                euler = activeTuner.TrayLocalEulerFor(tray.Owner);
            }
            else
            {
                offset = HandTrayAlignmentDefaults.GetTrayLocalOffset(tray.Owner);
                euler = HandTrayAlignmentDefaults.GetTrayLocalEuler(tray.Owner);
            }

            tray.Root.localPosition += offset;
            tray.Root.localRotation *= Quaternion.Euler(euler);
            tray.DragSurfaceY = tray.Root.position.y + 0.05f;
            tray.DragPlane = new Plane(Vector3.up, new Vector3(0f, tray.DragSurfaceY, 0f));
        }

        private void PositionTrayFallback(TrayBinding tray)
        {
            if (tray.Root == null || layout == null)
                return;

            Transform origin = layout.Origin;
            float span = layout.GridSpan;
            float trayZ = tray.Owner == Player.Host ? -span * 0.62f : span * 0.62f;
            tray.Root.SetParent(origin, false);
            tray.Root.localPosition = new Vector3(0f, layout.TileHeight, trayZ);
            tray.Root.localRotation = Quaternion.identity;

            tray.DragSurfaceY = origin.position.y + layout.TileHeight + layout.PieceSurfaceLift + 0.14f;
            tray.DragPlane = new Plane(Vector3.up, origin.position + Vector3.up * tray.DragSurfaceY);
        }

        private void PopulateTiles(TrayBinding tray)
        {
            if (tunerPreviewActive && activeTuner != null && activeTuner.previewAllSlots &&
                tray.Owner == activeTuner.editingPlayer)
            {
                PopulatePreviewSlots(tray);
                return;
            }

            if (ReserveManager.Instance == null)
                return;

            var tiles = new List<PieceType>();

            if (GameStateManager.Instance != null && GameStateManager.Instance.IsSpringPhase())
            {
                PieceType? drawn = ReserveManager.Instance.GetSpringDrawnFlower(tray.Owner);
                if (drawn.HasValue)
                    tiles.Add(drawn.Value);
            }
            else
            {
                foreach (PieceType type in ReserveManager.Instance.GetHand(tray.Owner))
                    tiles.Add(type);
            }

            if (tiles.Count == 0)
                return;

            PopulateTilesAtSlots(tray, tiles);
        }

        private void PopulatePreviewSlots(TrayBinding tray)
        {
            var tiles = new List<PieceType>();
            for (int i = 0; i < HandTrayTunerSettings.MaxSlots; i++)
                tiles.Add((PieceType)i);

            PopulateTilesAtSlots(tray, tiles);
        }

        private void PopulateTilesAtSlots(TrayBinding tray, List<PieceType> tiles)
        {
            if (tray.Root == null || tray.TilesRoot == null || layout == null)
                return;

            activeTuner?.EnsureSlotArrays();

            float worldSpacing = layout.CellSpacing * (activeTuner?.autoSlotSpacing ?? 0.92f);
            float localSpacing = worldSpacing;
            float localShelfWidth = layout.GridSpan * 0.72f;

            if (tray.HasStandAnchor)
            {
                float standScale = Mathf.Max(tray.StandAnchor.StandTransform.lossyScale.x, 0.001f);
                localSpacing = worldSpacing / standScale;
                localShelfWidth = tray.StandAnchor.LocalShelfWidth;
            }

            float totalWidth = (tiles.Count - 1) * localSpacing;
            if (!UseManualSlots() && tiles.Count > 1 && totalWidth > localShelfWidth * 0.92f)
            {
                localSpacing = (localShelfWidth * 0.92f) / (tiles.Count - 1);
                totalWidth = (tiles.Count - 1) * localSpacing;
            }

            float startOffset = -totalWidth * 0.5f;
            Vector3 tileAxis = tray.HasStandAnchor ? tray.StandAnchor.LocalTileAxis : Vector3.right;
            bool sceneSlots = UsesSceneSlotMarkers(tray);
            EnsureTilesRoot(tray);

            for (int i = 0; i < tiles.Count; i++)
            {
                PieceType type = tiles[i];
                bool locked = !tunerPreviewActive &&
                              GameStateManager.Instance != null &&
                              !GameStateManager.Instance.IsSpringPhase() &&
                              GameManager.Instance != null &&
                              PieceRules.IsSpecialFlower(type) &&
                              !GameManager.Instance.SpecialTilesUnlocked(tray.Owner);

                GameObject visual = BoardManager.Instance.CreateHandVisual(tray.Owner, type);
                int slotIndex = ResolveSlotIndex(i, tiles.Count);

                if (sceneSlots && GameBoardSetup.TryGetSlotMarker(tray.Root, slotIndex, out Transform slotMarker))
                {
                    HandTrayLayoutUtility.ApplyRuntimeTileToSlot(
                        visual,
                        slotMarker,
                        tray.TilesRoot,
                        layout.CellSpacing,
                        type);
                }
                else
                {
                    visual.transform.SetParent(tray.TilesRoot, false);
                    visual.transform.localScale = Vector3.one;

                    if (UseManualSlots())
                    {
                        Vector3 localPosition = GetSlotPosition(slotIndex, tray.Owner);
                        Quaternion localRotation = Quaternion.Euler(GetSlotEuler(slotIndex, tray.Owner));
                        visual.transform.SetPositionAndRotation(
                            tray.Root.TransformPoint(localPosition),
                            tray.Root.rotation * localRotation);
                    }
                    else
                    {
                        Vector3 localPosition = tileAxis * (startOffset + i * localSpacing);
                        visual.transform.SetPositionAndRotation(
                            tray.Root.TransformPoint(localPosition),
                            tray.Root.rotation);
                    }

                    WoodTheme.FitPrefabScaleOnly(visual, layout.CellSpacing);
                }

                WoodTheme.EnsurePiecePickCollider(visual, layout.CellSpacing);

                var handle = visual.AddComponent<HandTileHandle>();
                handle.PieceType = type;
                handle.SlotIndex = slotIndex;
                handle.IsSpringDraw = GameStateManager.Instance != null && GameStateManager.Instance.IsSpringPhase();
                handle.Locked = locked;
                handle.RestPosition = visual.transform.position;
                handle.RestRotation = visual.transform.rotation;
                handle.RestScale = visual.transform.localScale;

                if (locked)
                    ApplyLockedLook(visual);
                else if (handle.IsSpringDraw)
                    PieceStateAnimator.ApplySpringBudLook(visual, type, tray.Owner);

                tray.Handles.Add(handle);
            }
        }

        private int ResolveSlotIndex(int tileListIndex, int tileCount)
        {
            if (!tunerPreviewActive &&
                tileCount == 1 &&
                GameStateManager.Instance != null &&
                GameStateManager.Instance.IsSpringPhase())
            {
                return HandTrayAlignmentDefaults.SpringDrawSlotIndex;
            }

            return Mathf.Clamp(tileListIndex, 0, HandTrayTunerSettings.MaxSlots - 1);
        }

        private bool UseManualSlots()
        {
            if (tunerPreviewActive && activeTuner != null)
                return activeTuner.useManualSlotPositions;

            return HandTrayAlignmentDefaults.UseManualSlotPositions;
        }

        private Vector3 GetSlotPosition(int slotIndex, Player player)
        {
            if (tunerPreviewActive && activeTuner != null)
            {
                return player == Player.Host
                    ? activeTuner.slotLocalPositions[slotIndex]
                    : activeTuner.opponentSlotLocalPositions[slotIndex];
            }

            return HandTrayAlignmentDefaults.GetSlotPosition(slotIndex, player);
        }

        private Vector3 GetSlotEuler(int slotIndex, Player player)
        {
            if (tunerPreviewActive && activeTuner != null)
            {
                return player == Player.Host
                    ? activeTuner.slotLocalEuler[slotIndex]
                    : activeTuner.opponentSlotLocalEuler[slotIndex];
            }

            return HandTrayAlignmentDefaults.GetSlotEuler(slotIndex, player);
        }

        private static void ApplyLockedLook(GameObject visual)
        {
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
            {
                if (renderer.material == null)
                    continue;

                Color c = renderer.material.HasProperty("_BaseColor")
                    ? renderer.material.GetColor("_BaseColor")
                    : renderer.material.color;
                c = Color.Lerp(c, Color.gray, 0.55f);
                c.a = 0.55f;
                if (renderer.material.HasProperty("_BaseColor"))
                    renderer.material.SetColor("_BaseColor", c);
                else
                    renderer.material.color = c;
            }
        }

        private void HandlePointer()
        {
            if (isSnappingBack || (Mouse.current == null && Touchscreen.current == null))
                return;

            Vector2 pointer = GetPointerPosition();

            if (activeDrag == null)
            {
                TrayBinding active = GetActiveTray();
                if (active == null || !IsPrimaryPressed())
                    return;

                if (!TryPickHandle(active, pointer, out HandTileHandle handle))
                    return;

                if (handle.Locked)
                {
                    GameplayFeedback.Show("Unlocks after 3 of your Play turns.");
                    return;
                }

                BeginDrag(handle);
                return;
            }

            if (IsPrimaryHeld())
            {
                UpdateDragPosition(pointer);
                return;
            }

            EndDrag(pointer);
        }

        private static Vector2 GetPointerPosition()
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                return Touchscreen.current.primaryTouch.position.ReadValue();

            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();

            return Vector2.zero;
        }

        private static bool IsPrimaryPressed()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;

            return Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        }

        private static bool IsPrimaryHeld()
        {
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
                return true;

            return Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed;
        }

        private void BeginDrag(HandTileHandle handle)
        {
            activeDrag = handle;
            hoverCoordinate = null;
            handle.transform.localScale = handle.RestScale.sqrMagnitude > 0.0001f
                ? handle.RestScale
                : handle.transform.localScale;
            handle.transform.SetParent(null, true);
            dragTargetPosition = handle.transform.position + Vector3.up * PieceMotion.TrayDragLift * 0.35f;
            dragTargetRotation = handle.RestRotation;

            if (trayDragPolish != null)
            {
                trayDragPolish.Detach();
                Destroy(trayDragPolish);
            }

            trayDragPolish = handle.gameObject.AddComponent<DragPiecePolish>();
            trayDragPolish.Attach(handle.transform, layout);

            GameInputController.Instance?.ClearSelection();
            GameInputController.Instance?.PreviewPlacement(handle.PieceType);
            PieceFeedbackManager.Instance?.PlayClick();
        }

        private void UpdateDragPosition(Vector2 screenPosition)
        {
            if (activeDrag == null || Camera.main == null || layout == null)
                return;

            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            if (TryGetBoardDragTarget(ray, screenPosition, out int coordinate, out Vector3 boardPosition))
            {
                hoverCoordinate = coordinate;
                dragTargetPosition = boardPosition;
                // Keep live drag yaw — do not flatten to identity.
                LegalMoveHighlighter.Instance?.SetHoveredCoordinate(hoverCoordinate);
                return;
            }

            hoverCoordinate = null;
            LegalMoveHighlighter.Instance?.SetHoveredCoordinate(null);

            TrayBinding tray = GetTrayForHandle(activeDrag);
            if (tray != null && tray.DragPlane.Raycast(ray, out float enter))
            {
                Vector3 point = ray.GetPoint(enter);
                dragTargetPosition = point + Vector3.up * PieceMotion.TrayDragLift;
            }
        }

        private bool TryGetBoardDragTarget(Ray ray, Vector2 screenPosition, out int coordinate, out Vector3 surfacePosition)
        {
            coordinate = -1;
            surfacePosition = default;

            if (BoardManager.Instance != null &&
                BoardManager.Instance.TryResolveCoordinate(ray, screenPosition, out coordinate))
            {
                surfacePosition = PieceMotion.GetBoardHoverPosition(coordinate);
                return true;
            }

            Vector3 planePoint = layout.GetSurfaceWorldPosition(BoardUtils.MiddleGate);
            var boardPlane = new Plane(Vector3.up, planePoint);
            if (!boardPlane.Raycast(ray, out float enter))
                return false;

            Vector3 hit = ray.GetPoint(enter);
            if (!layout.TryWorldToCoordinate(hit, out coordinate, BoardPickUtility.WorldSnapToleranceScale))
                return false;

            surfacePosition = PieceMotion.GetBoardHoverPosition(coordinate);
            return true;
        }

        private void ApplyDragVisual()
        {
            if (activeDrag == null)
                return;

            float t = 1f - Mathf.Exp(-DragLerpSpeed * Time.deltaTime);
            Transform tile = activeDrag.transform;
            tile.position = Vector3.Lerp(tile.position, dragTargetPosition, t);
            trayDragPolish?.UpdateDrag(dragTargetPosition, hoverCoordinate);
        }

        private void CleanupTrayDragPolish()
        {
            if (trayDragPolish == null)
                return;

            trayDragPolish.Detach();
            Destroy(trayDragPolish);
            trayDragPolish = null;
        }

        private void EndDrag(Vector2 screenPosition)
        {
            if (activeDrag == null || Camera.main == null)
                return;

            HandTileHandle handle = activeDrag;
            float releaseYaw = trayDragPolish != null
                ? trayDragPolish.GetLiveYawDegrees()
                : handle.transform.eulerAngles.y;
            activeDrag = null;
            hoverCoordinate = null;
            LegalMoveHighlighter.Instance?.SetHoveredCoordinate(null);
            CleanupTrayDragPolish();
            handle.transform.rotation = Quaternion.Euler(0f, releaseYaw, 0f);
            GameInputController.Instance?.ClearPlacementPreview();

            Player player = GameManager.Instance.GetCurrentPlayer();
            Ray ray = Camera.main.ScreenPointToRay(screenPosition);

            bool resolved = BoardManager.Instance.TryResolveCoordinate(ray, screenPosition, out int coordinate);
            if (resolved &&
                TileSelector.Instance != null &&
                TileSelector.Instance.TryPlaceTile(
                    player, handle.PieceType, coordinate, handle.gameObject, animateDrop: true))
            {
                TrayBinding tray = GetTrayForHandle(handle) ?? hostTray;
                tray.Handles.Remove(handle);
                return;
            }

            if (!resolved)
                GameplayFeedback.Show("Release on a blue marker.");

            PieceFeedbackManager.Instance?.PlayClick();
            StartSnapBack(handle);
        }

        private void StartSnapBack(HandTileHandle handle)
        {
            if (snapBackRoutine != null)
                StopCoroutine(snapBackRoutine);

            snapBackRoutine = StartCoroutine(SnapBackRoutine(handle));
        }

        public bool TryTakeHandVisual(Player player, PieceType type, out GameObject visual)
        {
            visual = null;
            TrayBinding tray = player == Player.Host ? hostTray : opponentTray;
            foreach (HandTileHandle handle in tray.Handles)
            {
                if (handle == null || handle.Locked || handle.PieceType != type)
                    continue;

                visual = handle.gameObject;
                tray.Handles.Remove(handle);
                return true;
            }

            return false;
        }

        private IEnumerator SnapBackRoutine(HandTileHandle handle)
        {
            isSnappingBack = true;

            Transform tile = handle.transform;
            Vector3 endPosition = handle.RestPosition;
            Quaternion endRotation = handle.RestRotation;
            Vector3 restScale = handle.RestScale.sqrMagnitude > 0.0001f
                ? handle.RestScale
                : tile.localScale;

            if (PieceFeedbackManager.Instance != null)
            {
                bool finished = false;
                PieceFeedbackManager.Instance.ExecuteSnapBack(
                    tile,
                    endPosition,
                    endRotation,
                    () => finished = true);

                while (!finished)
                    yield return null;
            }
            else
            {
                yield return PieceMotion.AnimateSnap(
                    tile,
                    tile.position,
                    endPosition,
                    tile.rotation,
                    endRotation,
                    SnapBackDuration);
            }

            TrayBinding tray = GetTrayForHandle(handle) ?? hostTray;
            tile.SetParent(ResolveRestParent(tray, handle), true);
            tile.position = endPosition;
            tile.rotation = endRotation;
            tile.localScale = restScale;
            isSnappingBack = false;
            snapBackRoutine = null;
        }

        private bool TryPickHandle(TrayBinding active, Vector2 screenPosition, out HandTileHandle handle)
        {
            handle = null;
            if (Camera.main == null || active == null)
                return false;

            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            var hits = Physics.RaycastAll(ray, 200f);
            if (hits != null && hits.Length > 0)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (var hit in hits)
                {
                    var candidate = hit.collider.GetComponentInParent<HandTileHandle>();
                    if (candidate == null || !active.Handles.Contains(candidate))
                        continue;

                    handle = candidate;
                    return true;
                }
            }

            return TryScreenNearestHandle(active, screenPosition, out handle);
        }

        private bool TryScreenNearestHandle(TrayBinding active, Vector2 screenPosition, out HandTileHandle handle)
        {
            handle = null;
            if (Camera.main == null || active?.Handles == null || active.Handles.Count == 0)
                return false;

            float best = BoardPickUtility.ScreenHandPickRadiusPixels;
            HandTileHandle bestHandle = null;

            foreach (HandTileHandle candidate in active.Handles)
            {
                if (candidate == null || candidate.Locked)
                    continue;

                Vector3 screen = Camera.main.WorldToScreenPoint(candidate.transform.position);
                if (screen.z <= 0f)
                    continue;

                float distance = Vector2.Distance(screenPosition, new Vector2(screen.x, screen.y));
                if (distance >= best)
                    continue;

                best = distance;
                bestHandle = candidate;
            }

            if (bestHandle == null)
                return false;

            handle = bestHandle;
            return true;
        }

        private void CancelDrag()
        {
            if (snapBackRoutine != null)
            {
                StopCoroutine(snapBackRoutine);
                snapBackRoutine = null;
                isSnappingBack = false;
            }

            if (activeDrag == null)
                return;

            HandTileHandle handle = activeDrag;
            activeDrag = null;
            hoverCoordinate = null;
            LegalMoveHighlighter.Instance?.SetHoveredCoordinate(null);
            CleanupTrayDragPolish();
            GameInputController.Instance?.ClearPlacementPreview();
            TrayBinding tray = GetTrayForHandle(handle) ?? hostTray;
            handle.transform.SetParent(ResolveRestParent(tray, handle), true);
            handle.transform.position = handle.RestPosition;
            handle.transform.rotation = handle.RestRotation;
            handle.transform.localScale = handle.RestScale.sqrMagnitude > 0.0001f
                ? handle.RestScale
                : handle.transform.localScale;
        }

        public bool WouldConsumePointer(Vector2 screenPosition)
        {
            if (tunerPreviewActive || isSnappingBack)
                return false;

            TrayBinding active = GetActiveTray();
            return active != null && TryPickHandle(active, screenPosition, out _);
        }

        private TrayBinding GetTrayForHandle(HandTileHandle handle)
        {
            if (hostTray.Handles.Contains(handle))
                return hostTray;

            if (opponentTray.Handles.Contains(handle))
                return opponentTray;

            return null;
        }

        private static void ClearTray(TrayBinding tray)
        {
            foreach (var handle in tray.Handles)
            {
                if (handle != null)
                    Destroy(handle.gameObject);
            }

            tray.Handles.Clear();
            tray.HasStandAnchor = false;
        }

        public bool TryGetSlotHandle(Player player, int slotIndex, out HandTileHandle handle)
        {
            handle = null;
            TrayBinding tray = player == Player.Host ? hostTray : opponentTray;
            foreach (HandTileHandle candidate in tray.Handles)
            {
                if (candidate != null && candidate.SlotIndex == slotIndex)
                {
                    handle = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool OwnsHandle(Player player, HandTileHandle handle) =>
            handle != null && (player == Player.Host ? hostTray : opponentTray).Handles.Contains(handle);

        public void SyncSlotFromTransform(Player player, int slotIndex, Transform tileTransform)
        {
            if (tileTransform == null || tileTransform.parent == null)
                return;

            TrayBinding tray = player == Player.Host ? hostTray : opponentTray;
            EnsureTilesRoot(tray);

            Transform parent = tileTransform.parent;
            if (parent == tray.TilesRoot &&
                GameBoardSetup.TryGetSlotMarker(tray.Root, slotIndex, out Transform marker))
            {
                marker.SetPositionAndRotation(tileTransform.position, tileTransform.rotation);
#if UNITY_EDITOR
                marker.GetComponent<HandTraySlotMarker>()?.RefreshVisibility();
#endif
            }
            else if (parent.GetComponent<HandTraySlotMarker>() != null)
            {
                parent.SetPositionAndRotation(tileTransform.position, tileTransform.rotation);
                tileTransform.localPosition = Vector3.zero;
                tileTransform.localRotation = Quaternion.identity;
#if UNITY_EDITOR
                parent.GetComponent<HandTraySlotMarker>()?.RefreshVisibility();
#endif
            }
            else if (GameBoardSetup.TryGetSlotMarker(parent, slotIndex, out Transform legacyMarker))
            {
                legacyMarker.localPosition = tileTransform.localPosition;
                legacyMarker.localRotation = tileTransform.localRotation;
#if UNITY_EDITOR
                legacyMarker.GetComponent<HandTraySlotMarker>()?.RefreshVisibility();
#endif
            }

            if (activeTuner == null)
            {
                if (TryGetSlotHandle(player, slotIndex, out HandTileHandle handle))
                {
                    handle.RestPosition = tileTransform.position;
                    handle.RestRotation = tileTransform.rotation;
                }

                return;
            }

            Vector3[] positions = activeTuner.GetSlotPositions(player);
            Vector3[] euler = activeTuner.GetSlotEuler(player);
            if (slotIndex < 0 || slotIndex >= positions.Length)
                return;

            positions[slotIndex] = tileTransform.localPosition;
            euler[slotIndex] = tileTransform.localEulerAngles;

            if (TryGetSlotHandle(player, slotIndex, out HandTileHandle tunedHandle))
            {
                tunedHandle.RestPosition = tileTransform.position;
                tunedHandle.RestRotation = tileTransform.rotation;
            }
        }

        private bool UsesSceneSlotMarkers(TrayBinding tray)
        {
            if (boardSetup == null)
                boardSetup = FindAnyObjectByType<GameBoardSetup>();

            return boardSetup != null
                && boardSetup.PreserveSceneAuthored
                && GameBoardSetup.HasBakedSlotMarkers(tray.Root);
        }

        private bool IsSceneAuthoredTray(TrayBinding tray)
        {
            if (tray.Root == null)
                return false;

            if (boardSetup == null)
                boardSetup = FindAnyObjectByType<GameBoardSetup>();

            if (boardSetup == null || !boardSetup.PreserveSceneAuthored)
                return false;

            return tray.Root.GetComponentInParent<GameBoardSetup>() != null;
        }

        private static bool BindStandAnchorFromSceneTray(TrayBinding tray, Transform standTransform)
        {
            if (tray.Root == null)
                return false;

            if (WoodTheme.TryGetPlayerStandAnchor(standTransform.gameObject, tray.Owner, out tray.StandAnchor))
                tray.HasStandAnchor = true;

            tray.DragSurfaceY = tray.Root.position.y + 0.05f;
            tray.DragPlane = new Plane(Vector3.up, new Vector3(0f, tray.DragSurfaceY, 0f));
            return true;
        }

        private Transform ResolveRestParent(TrayBinding tray, HandTileHandle handle)
        {
            EnsureTilesRoot(tray);
            return tray.TilesRoot != null ? tray.TilesRoot : tray.Root;
        }
    }
}
