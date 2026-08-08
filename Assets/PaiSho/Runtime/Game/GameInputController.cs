using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using PaiSho;
using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public enum MomentumSpendMode
    {
        None,
        Revive,
        Freeze
    }

    public class GameInputController : MonoBehaviour
    {
        public static GameInputController Instance;

        public PieceType? SelectedHandType { get; private set; }
        public Piece SelectedBoardPiece { get; private set; }
        public MomentumSpendMode MomentumMode { get; private set; } = MomentumSpendMode.None;

        private Camera mainCamera;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        private void Start()
        {
            mainCamera = Camera.main;
            ClearSelection();
        }

        private void Update()
        {
            if (TitleMenu.Instance != null && TitleMenu.Instance.IsOpen)
                return;

            if (GameUI.IsPassScrimShowing)
                return;

            if (GameStateManager.Instance != null && GameStateManager.Instance.IsEndPhase())
                return;

            if (AiController.Instance != null && AiController.Instance.IsAiTurn())
                return;

            if (HandTrayController.Instance != null && HandTrayController.Instance.IsDragging)
                return;

            if (BoardPieceDragController.Instance != null &&
                (BoardPieceDragController.Instance.IsDragging ||
                 BoardPieceDragController.Instance.HasPointerCapture))
                return;

            // F8 board/garden tuner owns clicks while open.
            if (BoardPointTuner.Instance != null && BoardPointTuner.Instance.IsPanelOpen)
                return;

            if (HandTrayTuner.Instance != null && HandTrayTuner.Instance.IsPanelOpen)
                return;

            HandleKeyboardShortcuts();

            if (!WasPrimaryClickPressed())
                return;

            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null || BoardManager.Instance == null)
                return;

            Vector2 pointer = GetPointerPosition();
            if (HandTrayController.Instance != null && HandTrayController.Instance.WouldConsumePointer(pointer))
                return;

            if (GameUI.IsPointerOverHud)
                return;

            Ray ray = mainCamera.ScreenPointToRay(pointer);
            if (!BoardManager.Instance.TryResolveCoordinate(ray, pointer, out int coordinate))
                return;

            HandleBoardClick(coordinate);
        }

        private void HandleKeyboardShortcuts()
        {
            if (Keyboard.current == null)
                return;

            if (GameStateManager.Instance != null && GameStateManager.Instance.IsSpringPhase())
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame)
                    ClearSelection();
                return;
            }

            if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectHandTile(PieceType.Jasmine);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectHandTile(PieceType.Rose);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectHandTile(PieceType.Lily);
            if (Keyboard.current.digit4Key.wasPressedThisFrame) SelectHandTile(PieceType.Jade);
            if (Keyboard.current.digit5Key.wasPressedThisFrame) SelectHandTile(PieceType.Chrysanthemum);
            if (Keyboard.current.digit6Key.wasPressedThisFrame) SelectHandTile(PieceType.Rhododendron);
            if (Keyboard.current.digit7Key.wasPressedThisFrame) SelectHandTile(PieceType.Boat);
            if (Keyboard.current.digit8Key.wasPressedThisFrame) SelectHandTile(PieceType.Rock);
            if (Keyboard.current.digit9Key.wasPressedThisFrame) SelectHandTile(PieceType.Knotweed);
            if (Keyboard.current.digit0Key.wasPressedThisFrame) SelectHandTile(PieceType.Wheel);
            if (Keyboard.current.minusKey.wasPressedThisFrame) SelectHandTile(PieceType.Lotus);
            if (Keyboard.current.equalsKey.wasPressedThisFrame) SelectHandTile(PieceType.Orchid);

            if (Keyboard.current.rKey.wasPressedThisFrame &&
                (GameManager.Instance == null || !GameManager.Instance.PendingBonusMove))
                BeginMomentumMode(MomentumSpendMode.Revive);

            if (Keyboard.current.fKey.wasPressedThisFrame &&
                (GameManager.Instance == null || !GameManager.Instance.PendingBonusMove))
                BeginMomentumMode(MomentumSpendMode.Freeze);

            if (Keyboard.current.uKey.wasPressedThisFrame &&
                (GameManager.Instance == null || !GameManager.Instance.PendingBonusMove) &&
                SelectedBoardPiece != null &&
                SelectedBoardPiece.Type == PieceType.Boat &&
                BoatManager.Instance != null &&
                BoatManager.Instance.HasCargo(SelectedBoardPiece))
            {
                foreach (int neighbor in BoardUtils.GetAdjacentCoordinates(SelectedBoardPiece.BoardCoordinate))
                {
                    if (!BoatManager.Instance.TryUnload(SelectedBoardPiece, neighbor))
                        continue;

                    GameManager.Instance.ClearBonusMoveOffer();
                    GameManager.Instance.MarkTurnComplete();
                    GameManager.Instance.EndTurn();
                    ClearSelection();
                    break;
                }
            }

            if (Keyboard.current.oKey.wasPressedThisFrame &&
                (GameManager.Instance == null || !GameManager.Instance.PendingBonusMove) &&
                SelectedBoardPiece != null &&
                SelectedBoardPiece.Type == PieceType.Wheel &&
                GameManager.Instance != null)
            {
                Player current = GameManager.Instance.GetCurrentPlayer();
                if (TileSelector.Instance != null &&
                    TileSelector.Instance.TryRotateWheel(current, SelectedBoardPiece))
                    ClearSelection();
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
                ClearSelection();

            if (Keyboard.current.eKey.wasPressedThisFrame && GameManager.Instance != null)
            {
                if (GameManager.Instance.PendingBonusMove)
                    GameManager.Instance.TryEndTurnEarly();
                else if (!GameManager.Instance.HasLegalActions(GameManager.Instance.GetCurrentPlayer()))
                    GameManager.Instance.PassTurn();
            }
        }

        public void HandleBoardClick(int coordinate)
        {
            Player currentPlayer = GameManager.Instance.GetCurrentPlayer();
            Piece existingPiece = BoardManager.Instance.GetPieceAt(coordinate);
            bool bonusPending = GameManager.Instance.PendingBonusMove;

            if (GameStateManager.Instance.IsSpringPhase())
            {
                PieceType? drawn = ReserveManager.Instance.GetSpringDrawnFlower(currentPlayer);
                if (!drawn.HasValue)
                {
                    GameplayFeedback.Show("No spring flower drawn — wait for your turn.");
                    return;
                }

                if (TileSelector.Instance.TryPlaceTile(currentPlayer, drawn.Value, coordinate))
                    ClearSelection();

                return;
            }

            if (bonusPending && MomentumMode != MomentumSpendMode.None)
                ClearMomentumMode();

            if (!bonusPending &&
                MomentumMode != MomentumSpendMode.None &&
                existingPiece != null &&
                existingPiece.Owner == currentPlayer)
            {
                bool applied = MomentumMode == MomentumSpendMode.Revive
                    ? TileSelector.Instance.TryMomentumRevive(currentPlayer, existingPiece)
                    : TileSelector.Instance.TryMomentumFreeze(currentPlayer, existingPiece);

                if (applied)
                    ClearSelection();

                return;
            }

            if (existingPiece != null && existingPiece.Owner == currentPlayer)
            {
                if (!bonusPending &&
                    SelectedBoardPiece != null &&
                    SelectedBoardPiece.Type == PieceType.Boat &&
                    SelectedBoardPiece != existingPiece &&
                    BoatManager.Instance != null &&
                    BoatManager.Instance.TryLoad(SelectedBoardPiece, existingPiece))
                {
                    ClearSelection();
                    GameplayVisualizer.Instance?.Refresh();
                    return;
                }

                if (SelectedBoardPiece == existingPiece)
                {
                    if (!bonusPending &&
                        existingPiece.Type == PieceType.Wheel &&
                        TileSelector.Instance != null &&
                        TileSelector.Instance.TryRotateWheel(currentPlayer, existingPiece))
                    {
                        ClearSelection();
                        return;
                    }

                    ClearSelection();
                }
                else
                {
                    SelectBoardPiece(existingPiece);
                }

                return;
            }

            if (existingPiece == null && SelectedBoardPiece == null && !SelectedHandType.HasValue && MomentumMode == MomentumSpendMode.None)
            {
                ClearSelection();
                return;
            }

            if (SelectedBoardPiece != null)
            {
                if (!bonusPending &&
                    SelectedBoardPiece.Type == PieceType.Boat &&
                    existingPiece == null &&
                    BoatManager.Instance != null &&
                    BoatManager.Instance.HasCargo(SelectedBoardPiece) &&
                    BoatManager.Instance.CanUnload(SelectedBoardPiece, coordinate) &&
                    (PlacementValidator.Instance == null ||
                     !PlacementValidator.Instance.TryGetLegalMove(SelectedBoardPiece, coordinate, out _)))
                {
                    if (BoatManager.Instance.TryUnload(SelectedBoardPiece, coordinate))
                    {
                        GameManager.Instance.ClearBonusMoveOffer();
                        GameManager.Instance.MarkTurnComplete();
                        GameManager.Instance.EndTurn();
                        ClearSelection();
                    }

                    return;
                }

                if (TileSelector.Instance.TryMoveTile(SelectedBoardPiece, coordinate))
                    ClearSelection();

                return;
            }

            if (bonusPending)
            {
                GameplayFeedback.Show("Extra Move ready — move a second tile, or press E to end your turn.");
                return;
            }

            if (SelectedHandType.HasValue)
            {
                PieceType type = SelectedHandType.Value;
                if (!ReserveManager.Instance.HasInHand(currentPlayer, type))
                {
                    GameplayFeedback.Show($"You do not have {type} in your hand.");
                    return;
                }

                if (TileSelector.Instance.TryPlaceTile(currentPlayer, type, coordinate))
                    ClearSelection();
            }
        }

        public void BeginMomentumMode(MomentumSpendMode mode)
        {
            if (GameManager.Instance != null && GameManager.Instance.PendingBonusMove)
            {
                GameplayFeedback.Show("Extra Move ready — move a second tile, or press E to end your turn.");
                return;
            }

            Player current = GameManager.Instance.GetCurrentPlayer();
            if (MomentumManager.Instance == null || MomentumManager.Instance.GetMomentum(current) <= 0)
            {
                GameplayFeedback.Show("No momentum tokens available.");
                return;
            }

            if (GetMomentumTargets(current, mode).Count == 0)
            {
                GameplayFeedback.Show(mode == MomentumSpendMode.Revive
                    ? "No wilted tiles to revive."
                    : "No tiles available to protect.");
                return;
            }

            MomentumMode = mode;
            SelectedHandType = null;
            SelectedBoardPiece = null;
            LegalMoveHighlighter.Instance?.ClearSelectionRing();
            RefreshHighlights();
        }

        public void ClearMomentumMode() => MomentumMode = MomentumSpendMode.None;

        public void SelectHandTile(PieceType type)
        {
            if (GameManager.Instance != null && GameManager.Instance.PendingBonusMove)
            {
                GameplayFeedback.Show("Extra Move ready — move a second tile, or press E to end your turn.");
                return;
            }

            Player current = GameManager.Instance.GetCurrentPlayer();
            if (!ReserveManager.Instance.HasInHand(current, type))
            {
                GameplayFeedback.Show($"{type} is not in your hand.");
                return;
            }

            ClearMomentumMode();
            SelectedHandType = type;
            SelectedBoardPiece = null;
            RefreshHighlights();
        }

        public void SelectBoardPiece(Piece piece)
        {
            ClearMomentumMode();
            SelectedBoardPiece = piece;
            SelectedHandType = null;
            LegalMoveHighlighter.Instance?.ShowSelectedPiece(piece);
            RefreshHighlights();

        }

        public void ClearSelection()
        {
            SelectedBoardPiece = null;
            SelectedHandType = null;
            ClearMomentumMode();
            LegalMoveHighlighter.Instance?.ClearSelectionRing();
            RefreshHighlights();
        }

        public void PreviewPlacement(PieceType type)
        {
            ClearMomentumMode();
            SelectedBoardPiece = null;
            SelectedHandType = type;
            RefreshHighlights();
        }

        public void ClearPlacementPreview()
        {
            SelectedHandType = null;
            RefreshHighlights();
        }

        public void SyncSelectionToPhase()
        {
            ClearSelection();
            RefreshHighlights();
        }

        public void RefreshHighlights()
        {
            if (LegalMoveHighlighter.Instance == null || PlacementValidator.Instance == null)
                return;

            Player currentPlayer = GameManager.Instance != null
                ? GameManager.Instance.GetCurrentPlayer()
                : Player.Host;

            if (MomentumMode != MomentumSpendMode.None)
            {
                LegalMoveHighlighter.Instance.ShowMomentumTargets(GetMomentumTargets(currentPlayer, MomentumMode));
                return;
            }

            if (SelectedBoardPiece != null)
            {
                Piece selected = SelectedBoardPiece;
                var moves = new List<LegalMove>(PlacementValidator.Instance.GetLegalMoves(selected));
                var moveCoords = new HashSet<int>();
                foreach (LegalMove move in moves)
                    moveCoords.Add(move.Coordinate);

                List<int> unloadTargets = null;
                if (selected.Type == PieceType.Boat &&
                    BoatManager.Instance != null &&
                    BoatManager.Instance.HasCargo(selected))
                {
                    foreach (int neighbor in BoardUtils.GetAdjacentCoordinates(selected.BoardCoordinate))
                    {
                        if (moveCoords.Contains(neighbor))
                            continue;
                        if (BoatManager.Instance.CanUnload(selected, neighbor))
                        {
                            unloadTargets ??= new List<int>();
                            unloadTargets.Add(neighbor);
                        }
                    }
                }

                LegalMoveHighlighter.Instance.ShowMovesWithContext(
                    selected,
                    moves,
                    LegalMoveCalculator.GetDisharmonyBlockedLandings(selected),
                    LegalMoveCalculator.GetGardenBlockedLandings(selected),
                    unloadTargets);
                return;
            }

            if (GameStateManager.Instance != null && GameStateManager.Instance.IsSpringPhase())
            {
                PieceType? springType = SelectedHandType;
                if (!springType.HasValue && ReserveManager.Instance != null)
                    springType = ReserveManager.Instance.GetSpringDrawnFlower(currentPlayer);

                if (springType.HasValue)
                {
                    LegalMoveHighlighter.Instance.ShowPlacementsWithContext(
                        PlacementValidator.Instance.GetLegalPlacements(currentPlayer, springType.Value),
                        currentPlayer,
                        springType);
                }
                else
                {
                    LegalMoveHighlighter.Instance.Clear();
                }

                return;
            }

            if (SelectedHandType.HasValue)
            {
                LegalMoveHighlighter.Instance.ShowPlacementsWithContext(
                    PlacementValidator.Instance.GetLegalPlacements(currentPlayer, SelectedHandType.Value),
                    currentPlayer,
                    SelectedHandType);
                return;
            }

            LegalMoveHighlighter.Instance.Clear();
        }

        public static List<int> GetMomentumTargets(Player player, MomentumSpendMode mode)
        {
            var targets = new List<int>();
            if (BoardManager.Instance == null)
                return targets;

            foreach (Piece piece in BoardManager.Instance.GetAllPieces())
            {
                if (piece.Owner != player)
                    continue;

                if (mode == MomentumSpendMode.Revive && piece.WiltLevel > 0)
                    targets.Add(piece.BoardCoordinate);
                else if (mode == MomentumSpendMode.Freeze)
                    targets.Add(piece.BoardCoordinate);
            }

            return targets;
        }

        private static bool WasPrimaryClickPressed()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return true;

            return false;
        }

        private static Vector2 GetPointerPosition()
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                return Touchscreen.current.primaryTouch.position.ReadValue();

            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();

            return Vector2.zero;
        }
    }
}
