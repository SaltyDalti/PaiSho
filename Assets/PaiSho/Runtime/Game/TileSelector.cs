using System;
using UnityEngine;
using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class TileSelector : MonoBehaviour
    {
        public static TileSelector Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public bool TryPlaceTile(
            Player player,
            PieceType type,
            int coordinate,
            GameObject existingVisual = null,
            bool animateDrop = false)
        {
            if (GameManager.Instance != null && GameManager.Instance.PendingBonusMove)
            {
                GameplayFeedback.Show("Extra Move is active — move another tile.");
                return false;
            }

            if (!ReserveManager.Instance.HasAvailableToPlace(player, type))
            {
                GameplayFeedback.Show("You don't have that tile in hand or reserve.");
                return false;
            }

            Piece occupant = BoardManager.Instance.GetPieceAt(coordinate);
            if (occupant != null)
            {
                GameplayFeedback.Show("That space is already occupied.");
                return false;
            }

            if (!PlacementValidator.Instance.CanPlace(player, type, coordinate))
            {
                GameplayFeedback.Show(
                    PlacementValidator.Instance.ExplainPlacementFailure(player, type, coordinate));
                return false;
            }

            GameObject sourceVisual = existingVisual;
            PiecePlacementMotion motion = PiecePlacementMotion.Immediate;

            if (HeadlessActionExecutor.IsActive)
            {
                motion = PiecePlacementMotion.Immediate;
            }
            else if (sourceVisual == null &&
                HandTrayController.Instance != null &&
                HandTrayController.Instance.TryTakeHandVisual(player, type, out GameObject trayVisual))
            {
                sourceVisual = trayVisual;
                motion = PiecePlacementMotion.TrayToBoard;
            }
            else if (sourceVisual != null)
            {
                motion = PiecePlacementMotion.TrayToBoard;
            }
            else
            {
                motion = PiecePlacementMotion.TrayToBoard;
            }

            bool preserveWorldPose = motion != PiecePlacementMotion.Immediate;
            Piece placed = BoardManager.Instance.PlacePiece(
                player, type, coordinate, sourceVisual, preserveWorldPose);
            if (placed == null)
                return false;

            if (motion == PiecePlacementMotion.TrayToBoard && sourceVisual == null)
            {
                placed.transform.position = PieceMotion.GetAutoPlaceOrigin(player);
                placed.transform.rotation = Quaternion.identity;
            }

            ReserveManager.Instance.RemovePlacedTile(player, type);
            MovementManager.Instance.RegisterPlacement(player);

            if (type == PieceType.Knotweed)
                KnotweedManager.Instance?.ProcessDrainEffects();

            if (GameStateManager.Instance.IsSpringPhase())
                GameManager.Instance.RecordSpringPlacement();

            DebugLogger.Log($"Placed {type} by {player} at {coordinate}");
            GameLogManager.Instance?.Log(ActionType.Placement, player, type, coordinate, coordinate);

            void FinishPlace()
            {
                GameManager.Instance.ClearBonusMoveOffer();
                GameManager.Instance.MarkTurnComplete();
                GameManager.Instance.EndTurn();
                if (!HeadlessActionExecutor.SkipPresentation)
                {
                    GameInputController.Instance?.RefreshHighlights();
                    GameplayVisualizer.Instance?.Refresh();
                }
            }

            if (HeadlessActionExecutor.IsActive || PieceFeedbackManager.Instance == null)
                FinishPlace();
            else
                PieceFeedbackManager.Instance.ExecutePlace(placed, FinishPlace, motion);

            return true;
        }

        public bool TryMoveTile(Piece piece, int coordinate)
        {
            if (piece == null)
                return false;

            if (!MovementManager.Instance.CanMoveTile(piece))
            {
                GameplayFeedback.Show($"You've already moved this turn — spend momentum to move again.");
                return false;
            }

            if (!PlacementValidator.Instance.TryGetLegalMove(piece, coordinate, out LegalMove legalMove))
            {
                GameplayFeedback.Show(PlacementValidator.Instance.ExplainMoveFailure(piece, coordinate));
                return false;
            }

            int fromCoordinate = piece.BoardCoordinate;
            int captureFromCoordinate = legalMove.IsCapture && legalMove.CaptureTarget != null
                ? legalMove.CaptureTarget.BoardCoordinate
                : -1;

            void ApplyMove()
            {
                if (legalMove.IsCapture && legalMove.CaptureTarget != null)
                {
                    Piece target = legalMove.CaptureTarget;
                    int capturedFrom = captureFromCoordinate >= 0 ? captureFromCoordinate : coordinate;

                    // Animated path already released the victim; instant/headless may not have.
                    // Always clear the square before MovePiece or landing fails silently.
                    if (target.BoardCoordinate >= 0)
                    {
                        PotManager.Instance.RecordCapture(target, piece.Owner, capturedFrom);
                        if (PotVisualManager.Instance != null)
                            PotVisualManager.Instance.SendToPot(target, piece.Owner);
                        else
                            BoardManager.Instance.ReleasePieceFromBoard(target);
                    }

                    GameLogManager.Instance?.Log(
                        ActionType.Capture,
                        piece.Owner,
                        target.Type,
                        capturedFrom,
                        coordinate);
                }

                if (legalMove.HasPush)
                {
                    Piece pushed = legalMove.Push.PushedPiece;
                    int pushFrom = pushed.BoardCoordinate;
                    if (!BoardManager.Instance.MovePiece(pushed, legalMove.Push.ToCoordinate))
                        return;

                    DebugLogger.Log($"Boat pushed {pushed.Type} from {pushFrom} to {legalMove.Push.ToCoordinate}");
                }

                if (!BoardManager.Instance.MovePiece(piece, coordinate))
                    return;

                MovementManager.Instance.RegisterMovement(piece);
                DebugLogger.Log($"Moved {piece.Type} to {coordinate}");
                GameLogManager.Instance?.Log(ActionType.Move, piece.Owner, piece.Type, fromCoordinate, coordinate);

                Player mover = piece.Owner;
                int movedCount = MovementManager.Instance.GetMovedTileCount(mover);
                bool extraGranted = GameManager.Instance != null && GameManager.Instance.PendingBonusMove;

                // Extra Move already bought: first move leaves the turn open for the second.
                if (extraGranted && movedCount == 1)
                {
                    if (!HeadlessActionExecutor.SkipPresentation)
                    {
                        GameplayFeedback.Show("Extra Move — take your second move.", 3.5f);
                        GameInputController.Instance?.ClearSelection();
                        GameInputController.Instance?.RefreshHighlights();
                        GameplayVisualizer.Instance?.Refresh();
                    }

                    return;
                }

                // AI / headless: keep the turn open briefly so they can decide to buy Extra Move.
                bool aiOrHeadless =
                    HeadlessActionExecutor.IsActive ||
                    (AiController.Instance != null && AiController.Instance.IsAiPlayer(mover));
                bool canBuy =
                    GameManager.Instance != null &&
                    GameManager.Instance.CanGrantExtraMove(mover) &&
                    movedCount == 1;

                if (aiOrHeadless && canBuy)
                {
                    if (!HeadlessActionExecutor.SkipPresentation)
                    {
                        GameInputController.Instance?.RefreshHighlights();
                        GameplayVisualizer.Instance?.Refresh();
                    }

                    return;
                }

                GameManager.Instance.ClearBonusMoveOffer();
                GameManager.Instance.MarkTurnComplete();
                GameManager.Instance.EndTurn();
                if (!HeadlessActionExecutor.SkipPresentation)
                {
                    GameInputController.Instance?.RefreshHighlights();
                    GameplayVisualizer.Instance?.Refresh();
                }
            }

            if (HeadlessActionExecutor.IsActive)
            {
                ApplyMove();
                return true;
            }

            if (PieceFeedbackManager.Instance != null)
                PieceFeedbackManager.Instance.ExecuteMove(piece, fromCoordinate, coordinate, legalMove, ApplyMove);
            else
                ApplyMove();

            return true;
        }

        public bool TryRotateWheel(Player player, Piece wheel)
        {
            if (wheel == null || wheel.Type != PieceType.Wheel || wheel.Owner != player)
                return false;

            if (GameManager.Instance != null && GameManager.Instance.PendingBonusMove)
            {
                GameplayFeedback.Show("Extra Move is active — move another tile.");
                return false;
            }

            if (!MovementManager.Instance.CanMoveTile(wheel))
            {
                GameplayFeedback.Show("You've already acted this turn — spend momentum to rotate again.");
                return false;
            }

            void FinishRotate()
            {
                MovementManager.Instance.RegisterWheelRotation(wheel);
                PieceFeedbackManager.Instance?.PlayClick();
                GameManager.Instance.ClearBonusMoveOffer();
                GameManager.Instance.MarkTurnComplete();
                GameManager.Instance.EndTurn();
                if (!HeadlessActionExecutor.SkipPresentation)
                {
                    GameInputController.Instance?.RefreshHighlights();
                    GameplayVisualizer.Instance?.Refresh();
                }
            }

            if (WheelRotationManager.Instance == null)
                return false;

            WheelRotationManager.Instance.RotateAdjacentTiles(wheel, FinishRotate);
            return true;
        }

        public bool TryBoatLoad(Player player, Piece boat, int passengerCoordinate)
        {
            if (boat == null || BoatManager.Instance == null || BoardManager.Instance == null)
                return false;

            if (GameManager.Instance != null && GameManager.Instance.PendingBonusMove)
                return false;

            Piece passenger = BoardManager.Instance.GetPieceAt(passengerCoordinate);
            if (!BoatManager.Instance.TryLoad(boat, passenger))
                return false;

            GameManager.Instance.ClearBonusMoveOffer();
            GameManager.Instance.MarkTurnComplete();
            GameManager.Instance.EndTurn();
            if (!HeadlessActionExecutor.SkipPresentation)
            {
                GameInputController.Instance?.RefreshHighlights();
                GameplayVisualizer.Instance?.Refresh();
            }

            return true;
        }

        public bool TryBoatUnload(Player player, Piece boat, int coordinate)
        {
            if (boat == null || BoatManager.Instance == null)
                return false;

            if (GameManager.Instance != null && GameManager.Instance.PendingBonusMove)
                return false;

            if (!BoatManager.Instance.TryUnload(boat, coordinate))
                return false;

            GameManager.Instance.ClearBonusMoveOffer();
            GameManager.Instance.MarkTurnComplete();
            GameManager.Instance.EndTurn();
            if (!HeadlessActionExecutor.SkipPresentation)
            {
                GameInputController.Instance?.RefreshHighlights();
                GameplayVisualizer.Instance?.Refresh();
            }

            return true;
        }

        public bool TryMomentumRevive(Player player, Piece piece)
        {
            if (piece == null || piece.Owner != player || piece.WiltLevel <= 0)
                return false;

            if (GameManager.Instance != null && GameManager.Instance.PendingBonusMove)
            {
                GameplayFeedback.Show("Extra Move is active — move another tile.");
                return false;
            }

            if (!MomentumManager.Instance.SpendReviveTile(player, piece))
                return false;

            GameLogManager.Instance?.Log(ActionType.Revival, player, piece.Type, piece.BoardCoordinate, piece.BoardCoordinate);
            PieceFeedbackManager.Instance?.PlayClick();
            GameManager.Instance.ClearBonusMoveOffer();
            GameManager.Instance.MarkTurnComplete();
            GameManager.Instance.EndTurn();
            GameInputController.Instance?.ClearMomentumMode();
            GameInputController.Instance?.RefreshHighlights();
            return true;
        }

        public bool TryMomentumFreeze(Player player, Piece piece)
        {
            if (piece == null || piece.Owner != player)
                return false;

            if (GameManager.Instance != null && GameManager.Instance.PendingBonusMove)
            {
                GameplayFeedback.Show("Extra Move is active — move another tile.");
                return false;
            }

            if (!MomentumManager.Instance.SpendFreezeWilt(player, piece))
                return false;

            GameLogManager.Instance?.Log(ActionType.Revival, player, piece.Type, piece.BoardCoordinate, piece.BoardCoordinate);
            PieceFeedbackManager.Instance?.PlayClick();
            GameManager.Instance.ClearBonusMoveOffer();
            GameManager.Instance.MarkTurnComplete();
            GameManager.Instance.EndTurn();
            GameInputController.Instance?.ClearMomentumMode();
            GameInputController.Instance?.RefreshHighlights();
            return true;
        }
    }
}
