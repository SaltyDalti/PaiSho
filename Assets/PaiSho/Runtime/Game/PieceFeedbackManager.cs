using System;
using System.Collections;
using UnityEngine;
using PaiSho;
using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class PieceFeedbackManager : MonoBehaviour
    {
        public static PieceFeedbackManager Instance;

        [Header("Board travel (moves & placements)")]
        [SerializeField] private float travelDuration = 0.5f;
        [SerializeField] private float travelArcHeight = 0.14f;

        [Header("Movement")]
        [SerializeField] private float pushSlideDuration = 0.42f;

        [Header("Capture & Snap")]
        [SerializeField] private float captureToPotDuration = 0.42f;
        [SerializeField] private float captureArcHeight = 0.14f;
        [SerializeField] private float snapBackDuration = 0.26f;

        private AudioSource audioSource;
        private AudioClip clickClip;
        private AudioClip moveClip;
        private AudioClip captureClip;
        private AudioClip placeClip;
        private AudioClip landingClip;
        private AudioClip harmonyClip;

        private int activeAnimations;

        public bool IsAnimating => activeAnimations > 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0.55f;

            clickClip = CreateWoodTone(520f, 0.06f, 0.32f);
            moveClip = CreateWoodTone(280f, 0.14f, 0.38f);
            captureClip = CreateWoodTone(160f, 0.18f, 0.5f);
            placeClip = CreateWoodTone(380f, 0.12f, 0.4f);
            landingClip = CreateWoodTone(90f, 0.08f, 0.28f);
            harmonyClip = CreateWoodTone(660f, 0.16f, 0.36f);
        }

        public void PlayLanding() => Play(landingClip);

        public void PlayClick() => Play(clickClip);

        public void PlayHarmony() => Play(harmonyClip);

        /// <summary>Abort in-flight tile motion before board clears / match reset.</summary>
        public void CancelAll()
        {
            StopAllCoroutines();
            activeAnimations = 0;
        }

        public void ExecuteMove(Piece piece, int fromCoordinate, int toCoordinate, LegalMove legalMove, Action onComplete)
        {
            StartCoroutine(MoveRoutine(piece, fromCoordinate, toCoordinate, legalMove, onComplete));
        }

        public IEnumerator WaitForAnimations(float timeoutSeconds = 8f)
        {
            float elapsed = 0f;
            while (IsAnimating && elapsed < timeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        public void ExecutePlace(Piece piece, Action onComplete, PiecePlacementMotion motion = PiecePlacementMotion.TrayToBoard)
        {
            if (motion == PiecePlacementMotion.Immediate)
            {
                FinalizePlacedPiece(piece);
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(PlaceRoutine(piece, onComplete));
        }

        public void ExecuteSnapBack(Transform tile, Vector3 end, Quaternion endRotation, Action onComplete = null)
        {
            StartCoroutine(SnapBackRoutine(tile, end, endRotation, onComplete));
        }

        private IEnumerator MoveRoutine(
            Piece piece,
            int fromCoordinate,
            int toCoordinate,
            LegalMove legalMove,
            Action onComplete)
        {
            if (piece == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            activeAnimations++;
            try
            {
                Play(moveClip);

                Transform tile = piece.transform;
                var life = piece.GetComponent<PieceStateAnimator>();
                life?.Suspend();

                Vector3 start = tile.position;
                Vector3 end = BoardManager.Instance.GetSeatedPieceWorldPosition(piece.gameObject, toCoordinate);
                Quaternion startRotation = tile.rotation;
                Quaternion endRotation = Quaternion.Euler(0f, piece.BoardYawDegrees, 0f);
                float arcHeight = PieceMotion.ComputeTravelArcHeight(start, end, travelArcHeight);

                PieceShadowFollower shadow = PieceShadowFollower.Attach(tile);

                bool forceCamera = ShouldForceCamera(piece.Owner);
                BoardCameraController.Active?.FocusMove(fromCoordinate, toCoordinate, travelDuration, forceCamera);

                yield return PieceMotion.AnimateAnticipation(
                    tile,
                    PieceMotion.ComputeAnticipationLift(start, end));

                int captureFrom = -1;
                if (legalMove.IsCapture && legalMove.CaptureTarget != null)
                {
                    Piece victim = legalMove.CaptureTarget;
                    captureFrom = victim.BoardCoordinate;
                    if (victim.BoardCoordinate >= 0)
                    {
                        PotManager.Instance.RecordCapture(victim, piece.Owner, captureFrom);
                        BoardManager.Instance.ReleasePieceFromBoard(victim);
                    }
                }

                if (legalMove.HasPush && legalMove.Push.PushedPiece != null)
                {
                    Piece pushed = legalMove.Push.PushedPiece;
                    Vector3 pushStart = pushed.transform.position;
                    Vector3 pushEnd = BoardManager.Instance.GetSeatedPieceWorldPosition(
                        pushed.gameObject,
                        legalMove.Push.ToCoordinate);

                    yield return PieceMotion.AnimateParallel(
                        AnimateBoardTravel(
                            tile,
                            start,
                            end,
                            startRotation,
                            endRotation,
                            arcHeight),
                        PieceMotion.AnimateSlide(
                            pushed.transform,
                            pushStart,
                            pushEnd,
                            pushSlideDuration));
                }
                else
                {
                    yield return AnimateBoardTravel(
                        tile,
                        start,
                        end,
                        startRotation,
                        endRotation,
                        arcHeight);
                }

                shadow?.Detach();
                life?.Resume();

                BoardCameraController.Active?.ReleaseCinematicFocus(forceCamera, toCoordinate);

                if (legalMove.IsCapture && legalMove.CaptureTarget != null)
                    yield return AnimateCaptureToPot(legalMove.CaptureTarget, piece.Owner, captureFrom);

                onComplete?.Invoke();
                FinalizePlacedPiece(piece);
            }
            finally
            {
                activeAnimations = Mathf.Max(0, activeAnimations - 1);
            }
        }

        private IEnumerator PlaceRoutine(Piece piece, Action onComplete)
        {
            if (piece == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            activeAnimations++;
            try
            {
                Play(placeClip);

                Transform tile = piece.transform;
                var life = piece.GetComponent<PieceStateAnimator>();
                life?.Suspend();
                Vector3 animStart = tile.position;
                Quaternion animStartRot = tile.rotation;
                int coordinate = piece.BoardCoordinate;

                Vector3 animEnd = BoardManager.Instance.GetSeatedPieceWorldPosition(piece.gameObject, coordinate);
                float dropYaw = animStartRot.eulerAngles.y;
                piece.SetBoardYawDegrees(dropYaw);
                Quaternion endRotation = Quaternion.Euler(0f, dropYaw, 0f);

                // Match board cell scale before travel so the arc lands where finalize will seat.
                float cellSpacing = BoardManager.Instance.GetBoardLayout().CellSpacing;
                tile.localRotation = endRotation;
                tile.localScale = Vector3.one;
                WoodTheme.FitPrefabToCellSpacing(piece.gameObject, cellSpacing, alignBottomToSurface: false);
                tile.SetPositionAndRotation(animStart, animStartRot);

                float arcHeight = PieceMotion.ComputeTravelArcHeight(animStart, animEnd, travelArcHeight);

                PieceShadowFollower shadow = PieceShadowFollower.Attach(tile);

                bool forceCamera = ShouldForceCamera(piece.Owner);
                BoardCameraController.Active?.FocusPlacementTarget(coordinate, travelDuration, forceCamera);

                yield return PieceMotion.AnimateAnticipation(
                    tile,
                    PieceMotion.ComputeAnticipationLift(animStart, animEnd));

                animStart = tile.position;
                arcHeight = PieceMotion.ComputeTravelArcHeight(animStart, animEnd, travelArcHeight);

                yield return AnimateBoardTravel(
                    tile,
                    animStart,
                    animEnd,
                    animStartRot,
                    endRotation,
                    arcHeight);

                shadow?.Detach();
                life?.Resume();
                FinalizePlacedPiece(piece);
                BoardCameraController.Active?.ReleaseCinematicFocus(forceCamera, coordinate);
                onComplete?.Invoke();
            }
            finally
            {
                activeAnimations = Mathf.Max(0, activeAnimations - 1);
            }
        }

        private static bool ShouldForceCamera(Player player) =>
            AiController.Instance != null && AiController.Instance.IsAiPlayer(player);

        private IEnumerator AnimateBoardTravel(
            Transform tile,
            Vector3 start,
            Vector3 end,
            Quaternion startRotation,
            Quaternion endRotation,
            float arcHeight)
        {
            yield return PieceMotion.AnimateBoardTravel(
                tile,
                start,
                end,
                startRotation,
                endRotation,
                travelDuration,
                arcHeight,
                PlayLanding);
        }

        private IEnumerator AnimateCaptureToPot(Piece captured, Player capturer, int captureFromCoordinate = -1)
        {
            if (captured == null)
                yield break;

            Play(captureClip);

            // Caller usually already recorded/released before travel.
            if (captured.BoardCoordinate >= 0)
            {
                int from = captureFromCoordinate >= 0 ? captureFromCoordinate : captured.BoardCoordinate;
                PotManager.Instance.RecordCapture(captured, capturer, from);
                BoardManager.Instance.ReleasePieceFromBoard(captured);
            }

            Vector3 start = captured.transform.position;
            Vector3 end = PotVisualManager.Instance != null
                ? PotVisualManager.Instance.PreviewStackPosition(captured, capturer)
                : start;

            foreach (Collider collider in captured.GetComponentsInChildren<Collider>())
                collider.enabled = false;

            var life = captured.GetComponent<PieceStateAnimator>();
            life?.Suspend();
            PieceShadowFollower shadow = PieceShadowFollower.Attach(captured.transform);

            yield return PieceMotion.AnimateCaptureToPot(
                captured.transform,
                start,
                end,
                captureToPotDuration,
                captureArcHeight);

            shadow?.Detach();
            life?.Resume();

            PotVisualManager.Instance?.FinalizeInPot(captured, capturer);
            BloomingManager.Instance?.RefreshAllLotusBlooms();
            GameplayVisualizer.Instance?.Refresh();
        }

        private IEnumerator SnapBackRoutine(Transform tile, Vector3 end, Quaternion endRotation, Action onComplete)
        {
            if (tile == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            Vector3 start = tile.position;
            Quaternion startRotation = tile.rotation;
            float arcHeight = PieceMotion.ComputeTravelArcHeight(start, end, travelArcHeight * 0.65f);

            var life = tile.GetComponent<PieceStateAnimator>();
            life?.Suspend();
            PieceShadowFollower shadow = PieceShadowFollower.Attach(tile);

            if (end.y < start.y - 0.005f)
            {
                yield return PieceMotion.AnimateBoardTravel(
                    tile,
                    start,
                    end,
                    startRotation,
                    endRotation,
                    snapBackDuration,
                    arcHeight,
                    null);
            }
            else
            {
                yield return PieceMotion.AnimateSnap(tile, start, end, startRotation, endRotation, snapBackDuration);
            }

            shadow?.Detach();
            life?.Resume();
            onComplete?.Invoke();
        }

        private static void FinalizePlacedPiece(Piece piece)
        {
            if (piece == null || !piece.UsesPrefabVisual || BoardManager.Instance == null)
                return;

            BoardManager.Instance.ApplyBoardSeatedVisual(
                piece.gameObject,
                piece.BoardCoordinate,
                piece.BoardYawDegrees);
        }

        private void Play(AudioClip clip)
        {
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
        }

        private static AudioClip CreateWoodTone(float fundamental, float duration, float volume)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var clip = AudioClip.Create($"wood_{fundamental}", sampleCount, 1, sampleRate, false);
            var data = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float progress = t / duration;
                float attack = Mathf.Clamp01(t / 0.012f);
                float decay = Mathf.Exp(-t * 6.5f) * (1f - progress * 0.25f);
                float envelope = attack * decay;

                float sample =
                    Mathf.Sin(2f * Mathf.PI * fundamental * t) * 0.52f +
                    Mathf.Sin(2f * Mathf.PI * fundamental * 2.02f * t) * 0.18f +
                    Mathf.Sin(2f * Mathf.PI * fundamental * 0.5f * t) * 0.12f;

                data[i] = sample * envelope * volume;
            }

            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateTone(float frequency, float duration, float volume)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var clip = AudioClip.Create($"tone_{frequency}", sampleCount, 1, sampleRate, false);
            var data = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = 1f - (t / duration);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volume;
            }

            clip.SetData(data, 0);
            return clip;
        }
    }
}
