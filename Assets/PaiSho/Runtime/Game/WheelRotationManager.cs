
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class WheelRotationManager : MonoBehaviour
    {
        public static WheelRotationManager Instance;

        [SerializeField] private float rotateDuration = 0.36f;

        private bool isAnimating;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public void RotateAdjacentTiles(Piece wheel)
        {
            RotateAdjacentTiles(wheel, null);
        }

        public void RotateAdjacentTiles(Piece wheel, Action onComplete)
        {
            if (isAnimating)
            {
                onComplete?.Invoke();
                return;
            }

            if (wheel == null || !wheel.CausesRotation())
            {
                onComplete?.Invoke();
                return;
            }

            if (HeadlessActionExecutor.IsActive)
            {
                RotateImmediate(wheel);
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(RotateRoutine(wheel, onComplete));
        }

        private void RotateImmediate(Piece wheel)
        {
            int center = wheel.BoardCoordinate;
            List<int> ring = BoardUtils.GetClockwiseSquareRing(center);
            if (ring.Count < 2)
                return;

            var snapshot = new Piece[ring.Count];
            for (int i = 0; i < ring.Count; i++)
                snapshot[i] = BoardManager.Instance.GetPieceAt(ring[i]);

            wheel.SetBoardYawDegrees((wheel.BoardYawDegrees - 90f + 360f) % 360f);
            if (wheel.transform != null)
                wheel.transform.rotation = Quaternion.Euler(0f, wheel.BoardYawDegrees, 0f);

            ApplyRotation(wheel, ring, snapshot);
        }

        private IEnumerator RotateRoutine(Piece wheel, Action onComplete)
        {
            isAnimating = true;

            int center = wheel.BoardCoordinate;
            List<int> ring = BoardUtils.GetClockwiseSquareRing(center);
            if (ring.Count < 2)
            {
                isAnimating = false;
                onComplete?.Invoke();
                yield break;
            }

            var snapshot = new Piece[ring.Count];
            for (int i = 0; i < ring.Count; i++)
                snapshot[i] = BoardManager.Instance.GetPieceAt(ring[i]);

            var moves = new List<(Piece piece, Vector3 start, Vector3 end)>();
            for (int i = 0; i < ring.Count; i++)
            {
                Piece moving = snapshot[i];
                if (moving == null || moving == wheel || moving.IsImmovable())
                    continue;

                int destinationIndex = FindClockwiseDestination(ring, snapshot, i);
                if (destinationIndex < 0)
                    continue;

                int destination = ring[destinationIndex];
                if (destination == center)
                    continue;

                moves.Add((
                    moving,
                    moving.transform.position,
                    BoardManager.Instance.GetPieceWorldPosition(destination)));
            }

            if (moves.Count > 0)
            {
                float elapsed = 0f;
                Quaternion wheelStart = wheel.transform.rotation;
                Quaternion wheelEnd = wheelStart * Quaternion.Euler(0f, -90f, 0f);

                while (elapsed < rotateDuration)
                {
                    elapsed += Time.deltaTime;
                    float u = PieceMotion.EaseInOutCubic(Mathf.Clamp01(elapsed / rotateDuration));

                    if (wheel != null)
                        wheel.transform.rotation = Quaternion.Slerp(wheelStart, wheelEnd, u);

                    foreach (var move in moves)
                    {
                        if (move.piece != null)
                            move.piece.transform.position = Vector3.Lerp(move.start, move.end, u);
                    }

                    yield return null;
                }

                if (wheel != null)
                {
                    wheel.SetBoardYawDegrees((wheel.BoardYawDegrees - 90f + 360f) % 360f);
                    wheel.transform.rotation = Quaternion.Euler(0f, wheel.BoardYawDegrees, 0f);
                }
            }
            else
            {
                // Still spin the wheel even when nothing moved on the ring.
                float elapsed = 0f;
                Quaternion wheelStart = wheel.transform.rotation;
                Quaternion wheelEnd = wheelStart * Quaternion.Euler(0f, -90f, 0f);
                while (elapsed < rotateDuration)
                {
                    elapsed += Time.deltaTime;
                    float u = PieceMotion.EaseInOutCubic(Mathf.Clamp01(elapsed / rotateDuration));
                    if (wheel != null)
                        wheel.transform.rotation = Quaternion.Slerp(wheelStart, wheelEnd, u);
                    yield return null;
                }

                if (wheel != null)
                {
                    wheel.SetBoardYawDegrees((wheel.BoardYawDegrees - 90f + 360f) % 360f);
                    wheel.transform.rotation = Quaternion.Euler(0f, wheel.BoardYawDegrees, 0f);
                }
            }

            ApplyRotation(wheel, ring, snapshot);
            isAnimating = false;
            onComplete?.Invoke();
        }

        private void ApplyRotation(Piece wheel, List<int> ring, Piece[] snapshot)
        {
            for (int i = 0; i < ring.Count; i++)
            {
                Piece moving = snapshot[i];
                if (moving == null || moving == wheel || moving.IsImmovable())
                    continue;

                BoardManager.Instance.LiftPiece(moving);
            }

            for (int i = 0; i < ring.Count; i++)
            {
                Piece moving = snapshot[i];
                if (moving == null || moving == wheel || moving.IsImmovable())
                    continue;

                int destinationIndex = FindClockwiseDestination(ring, snapshot, i);
                if (destinationIndex < 0)
                    continue;

                int destination = ring[destinationIndex];
                if (destination == wheel.BoardCoordinate)
                    continue;

                BoardManager.Instance.PlacePiece(destination, moving);
                DebugLogger.Log($">>> Wheel rotated {moving.Type} to {destination}");
            }

            BoardManager.Instance.RefreshAllHarmony();
            KnotweedManager.Instance?.ProcessDrainEffects();
            GameplayVisualizer.Instance?.Refresh();
        }

        private static int FindClockwiseDestination(List<int> ring, Piece[] snapshot, int sourceIndex)
        {
            int ringCount = ring.Count;
            int destinationIndex = (sourceIndex + 1) % ringCount;
            int attempts = 0;

            while (attempts < ringCount)
            {
                Piece blocker = snapshot[destinationIndex];
                if (blocker == null || (!blocker.IsImmovable() && !blocker.CausesRotation()))
                    return destinationIndex;

                destinationIndex = (destinationIndex + 1) % ringCount;
                attempts++;
            }

            return -1;
        }
    }
}
