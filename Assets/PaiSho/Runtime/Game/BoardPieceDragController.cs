using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using PaiSho;
using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    /// <summary>Drag pieces already on the board to move them during play.</summary>
    [DefaultExecutionOrder(-20)]
    public class BoardPieceDragController : MonoBehaviour
    {
        public static BoardPieceDragController Instance;

        private const float DragThresholdPixels = 6f;
        private const float DragLerpSpeed = 20f;
        private const float SnapBackDuration = 0.22f;

        private BoardLayout layout;
        private Piece pendingPiece;
        private Vector2 pressScreenPosition;
        private bool pointerDown;
        private bool dragActive;
        private Piece dragPiece;
        private Vector3 dragTargetPosition;
        private int originCoordinate;
        private Vector3 originWorldPosition;
        private bool isSnappingBack;
        private Coroutine snapRoutine;
        private int? hoverCoordinate;
        private DragPiecePolish dragPolish;

        public bool IsDragging => dragActive || isSnappingBack;
        /// <summary>Returns true while a board-piece drag gesture is in progress.</summary>
        public bool HasPointerCapture => pointerDown || dragActive || isSnappingBack;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (!ShouldHandlePointer())
                return;

            if (layout == null)
                layout = FindAnyObjectByType<BoardLayout>();

            Vector2 pointer = GetPointerPosition();
            bool pressed = IsPrimaryPressed();
            bool released = IsPrimaryReleased();

            if (!pointerDown && !dragActive && pressed)
                BeginPending(pointer);

            if (pointerDown && !dragActive && pressed)
                TryPromoteToDrag(pointer);

            if (dragActive && pressed)
                UpdateDrag(pointer);

            if (dragActive && released)
                EndDrag(pointer);
            else if (pointerDown && released)
                FinishTap(pointer);

            if (dragActive)
                ApplyDragVisual();
        }

        private bool ShouldHandlePointer()
        {
            if (TitleMenu.Instance != null && TitleMenu.Instance.IsOpen)
                return false;

            if (GameUI.IsPassScrimShowing)
                return false;

            if (GameStateManager.Instance != null && GameStateManager.Instance.IsEndPhase())
                return false;

            if (GameStateManager.Instance == null || GameStateManager.Instance.IsSpringPhase())
                return false;

            if (AiController.Instance != null && AiController.Instance.IsAiTurn())
                return false;

            if (HandTrayController.Instance != null && HandTrayController.Instance.IsDragging)
                return false;

            if (BoardPointTuner.Instance != null && BoardPointTuner.Instance.IsPanelOpen)
                return false;

            if (HandTrayTuner.Instance != null && HandTrayTuner.Instance.IsPanelOpen)
                return false;

            if (GameInputController.Instance != null &&
                GameInputController.Instance.MomentumMode != MomentumSpendMode.None)
                return false;

            return Mouse.current != null || Touchscreen.current != null;
        }

        private void BeginPending(Vector2 pointer)
        {
            if (!TryPickBoardPiece(pointer, out Piece piece))
                return;

            pointerDown = true;
            pendingPiece = piece;
            pressScreenPosition = pointer;
        }

        private void TryPromoteToDrag(Vector2 pointer)
        {
            if (pendingPiece == null)
            {
                pointerDown = false;
                return;
            }

            if ((pointer - pressScreenPosition).sqrMagnitude < DragThresholdPixels * DragThresholdPixels)
                return;

            if (!CanDragPiece(pendingPiece))
            {
                pointerDown = false;
                pendingPiece = null;
                return;
            }

            BeginDrag(pendingPiece);
            pointerDown = false;
            pendingPiece = null;
            UpdateDrag(pointer);
        }

        private void BeginDrag(Piece piece)
        {
            dragActive = true;
            dragPiece = piece;
            originCoordinate = piece.BoardCoordinate;
            originWorldPosition = BoardManager.Instance.GetSeatedPieceWorldPosition(
                piece.gameObject,
                originCoordinate);
            dragTargetPosition = PieceMotion.GetBoardHoverPosition(originCoordinate);
            hoverCoordinate = originCoordinate;

            if (layout == null)
                layout = FindAnyObjectByType<BoardLayout>();

            dragPolish = dragPiece.gameObject.GetComponent<DragPiecePolish>();
            if (dragPolish == null)
                dragPolish = dragPiece.gameObject.AddComponent<DragPiecePolish>();
            dragPolish.Attach(dragPiece.transform, layout);

            GameInputController.Instance?.SelectBoardPiece(piece);
            PieceFeedbackManager.Instance?.PlayClick();
        }

        private void UpdateDrag(Vector2 pointer)
        {
            if (dragPiece == null || Camera.main == null || layout == null)
                return;

            Ray ray = Camera.main.ScreenPointToRay(pointer);
            if (TryGetBoardDragTarget(ray, pointer, out int coordinate, out Vector3 boardPosition))
            {
                hoverCoordinate = coordinate;
                dragTargetPosition = boardPosition;
                return;
            }

            hoverCoordinate = null;
            Vector3 planePoint = layout.GetSurfaceWorldPosition(BoardUtils.MiddleGate);
            var boardPlane = new Plane(Vector3.up, planePoint);
            if (boardPlane.Raycast(ray, out float enter))
                dragTargetPosition = ray.GetPoint(enter) + Vector3.up * PieceMotion.BoardHoverLift;
        }

        private void EndDrag(Vector2 pointer)
        {
            if (dragPiece == null || Camera.main == null)
            {
                CancelDrag();
                return;
            }

            Piece piece = dragPiece;
            float releaseYaw = dragPolish != null
                ? dragPolish.GetLiveYawDegrees()
                : piece.transform.eulerAngles.y;
            dragActive = false;
            dragPiece = null;
            hoverCoordinate = null;
            // Don't resume life yet — travel animation will Suspend/Resume; avoids a glow flash.
            if (dragPolish != null)
            {
                dragPolish.Detach(resumeLife: false);
                dragPolish = null;
            }

            piece.SetBoardYawDegrees(releaseYaw);

            Ray ray = Camera.main.ScreenPointToRay(pointer);
            bool resolved = BoardManager.Instance.TryResolveCoordinate(ray, pointer, out int coordinate);
            if (resolved &&
                TileSelector.Instance != null &&
                TileSelector.Instance.TryMoveTile(piece, coordinate))
            {
                GameInputController.Instance?.ClearSelection();
                return;
            }

            if (!resolved)
                GameplayFeedback.Show("Release on a highlighted space.");

            PieceFeedbackManager.Instance?.PlayClick();
            StartSnapBack(piece, originWorldPosition);
        }

        private void FinishTap(Vector2 pointer)
        {
            Piece piece = pendingPiece;
            pointerDown = false;
            pendingPiece = null;

            if (piece == null || Camera.main == null)
                return;

            Ray ray = Camera.main.ScreenPointToRay(pointer);
            if (!BoardManager.Instance.TryResolveCoordinate(ray, pointer, out int coordinate))
                return;

            GameInputController.Instance?.HandleBoardClick(coordinate);
        }

        private void ApplyDragVisual()
        {
            if (dragPiece == null)
                return;

            // Polish owns position/rotation while dragging so the lift reads clearly.
            if (dragPolish != null)
            {
                dragPolish.UpdateDrag(dragTargetPosition, hoverCoordinate);
                LegalMoveHighlighter.Instance?.SetHoveredCoordinate(hoverCoordinate);
                return;
            }

            float t = 1f - Mathf.Exp(-DragLerpSpeed * Time.deltaTime);
            Transform tile = dragPiece.transform;
            tile.position = Vector3.Lerp(tile.position, dragTargetPosition, t);
            LegalMoveHighlighter.Instance?.SetHoveredCoordinate(hoverCoordinate);
        }

        private void StartSnapBack(Piece piece, Vector3 targetPosition)
        {
            if (snapRoutine != null)
                StopCoroutine(snapRoutine);

            // Resume life glow after a cancelled drag.
            piece.GetComponent<PieceStateAnimator>()?.Resume();
            snapRoutine = StartCoroutine(SnapBackRoutine(piece, targetPosition));
        }

        private IEnumerator SnapBackRoutine(Piece piece, Vector3 targetPosition)
        {
            isSnappingBack = true;
            Transform tile = piece.transform;
            Vector3 start = tile.position;
            Quaternion startRotation = tile.rotation;
            float arcHeight = PieceMotion.ComputeTravelArcHeight(start, targetPosition, 0.08f);

            var life = piece.GetComponent<PieceStateAnimator>();
            life?.Suspend();
            PieceShadowFollower shadow = PieceShadowFollower.Attach(tile);

            Quaternion endRotation = Quaternion.Euler(0f, piece.BoardYawDegrees, 0f);

            yield return PieceMotion.AnimateBoardTravel(
                tile,
                start,
                targetPosition,
                startRotation,
                endRotation,
                SnapBackDuration,
                arcHeight,
                null);

            shadow?.Detach();
            life?.Resume();

            if (piece.UsesPrefabVisual)
                WoodTheme.SeatOnWoodSurface(piece.gameObject);

            isSnappingBack = false;
            snapRoutine = null;
            CleanupDragPolish();
            GameInputController.Instance?.ClearSelection();
        }

        private void CleanupDragPolish()
        {
            LegalMoveHighlighter.Instance?.SetHoveredCoordinate(null);

            if (dragPolish != null)
            {
                dragPolish.Detach(resumeLife: true);
                Destroy(dragPolish);
                dragPolish = null;
            }
        }

        private void CancelDrag()
        {
            if (snapRoutine != null)
            {
                StopCoroutine(snapRoutine);
                snapRoutine = null;
                isSnappingBack = false;
            }

            if (dragPiece != null)
            {
                foreach (PieceShadowFollower shadow in dragPiece.GetComponents<PieceShadowFollower>())
                    shadow.Detach();
                dragPiece.GetComponent<PieceStateAnimator>()?.Resume();

                dragPiece.transform.position = originWorldPosition;
                if (dragPiece.UsesPrefabVisual)
                    WoodTheme.SeatOnWoodSurface(dragPiece.gameObject);
            }

            dragActive = false;
            dragPiece = null;
            pointerDown = false;
            pendingPiece = null;
            hoverCoordinate = null;
            CleanupDragPolish();
        }

        private bool CanDragPiece(Piece piece)
        {
            if (piece == null || piece.IsImmovable() || BoardManager.Instance == null)
                return false;

            if (GameManager.Instance == null || piece.Owner != GameManager.Instance.GetCurrentPlayer())
                return false;

            if (MovementManager.Instance == null || PlacementValidator.Instance == null)
                return false;

            if (MovementManager.Instance.PlacedThisTurn(piece.Owner))
                return false;

            if (!MovementManager.Instance.CanMoveTile(piece))
                return false;

            return PlacementValidator.Instance.GetLegalMoves(piece).Count > 0;
        }

        private bool TryGetBoardDragTarget(Ray ray, Vector2 screenPosition, out int coordinate, out Vector3 surfacePosition)
        {
            coordinate = -1;
            surfacePosition = default;

            if (BoardManager.Instance.TryResolveCoordinate(ray, screenPosition, out coordinate))
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

        private static bool TryPickBoardPiece(Vector2 screenPosition, out Piece piece)
        {
            piece = null;
            if (Camera.main == null || BoardManager.Instance == null)
                return false;

            return BoardPickUtility.TryPickBoardPiece(Camera.main, screenPosition, BoardManager.Instance, out piece);
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

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return true;

            return false;
        }

        private static bool IsPrimaryReleased()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
                return true;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
                return true;

            return false;
        }
    }
}
