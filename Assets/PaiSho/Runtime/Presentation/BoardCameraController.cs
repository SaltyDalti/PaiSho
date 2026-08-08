using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using PaiSho.Board;
using PaiSho.Game;

namespace PaiSho
{
    /// <summary>
    /// Orbit camera — brief cinematic cuts for moves/placements, then returns to where the player left it.
    /// </summary>
    public class BoardCameraController : MonoBehaviour
    {
        public static BoardCameraController Active { get; private set; }

        [Header("Orbit")]
        [SerializeField] private float orbitSensitivity = 0.28f;
        [SerializeField] private float minPitch = 14f;
        [SerializeField] private float maxPitch = 72f;

        [Header("Pan")]
        [SerializeField] private float panSpeed = 7f;
        [SerializeField] private float panDragSensitivity = 0.014f;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 9f;
        [SerializeField] private float scrollSensitivity = 0.55f;
        [SerializeField] private float minDistance = 4f;
        [SerializeField] private float maxDistance = 36f;

        [Header("Smoothing")]
        [SerializeField] private float moveSmoothing = 10f;
        [SerializeField] private float fastMultiplier = 2.2f;

        [Header("Cinematic")]
        [SerializeField] private bool enableCinematicFocus = true;
        [SerializeField] private float actionFocusBlend = 0.42f;
        [SerializeField] private float actionZoomFactor = 0.91f;
        [SerializeField] private float returnDuration = 1.1f;

        [Header("Touch")]
        [SerializeField] private float touchPinchZoomSensitivity = 0.018f;
        [SerializeField] private float touchOrbitSensitivity = 0.6f;
        [SerializeField] private float touchPanSensitivity = 0.012f;

        private Vector3 focusPoint;
        private Vector3 targetFocusPoint;
        private float yaw;
        private float pitch = 38f;
        private float distance = 14f;
        private float targetDistance = 14f;
        private float defaultYaw;
        private float defaultPitch = 38f;
        private float defaultDistance = 14f;
        private Vector3 defaultFocusPoint;
        private bool configured;
        private bool userControllingCamera;
        private Coroutine cinematicRoutine;

        // Exact view to restore after a cinematic beat (updated whenever the player moves the camera).
        private Vector3 restFocusPoint;
        private float restDistance;
        private float restYaw;
        private float restPitch;

        // Two-finger touch gesture — one finger stays free for board taps/drags.
        private bool twoFingerTouchActive;
        private Vector2 lastTouchA;
        private Vector2 lastTouchB;
        private bool touchGestureThisFrame;

        private void Awake()
        {
            Active = this;
        }

        private void OnDestroy()
        {
            if (Active == this)
                Active = null;
        }

        public void Configure(Vector3 focus, float initialDistance)
        {
            focusPoint = focus;
            targetFocusPoint = focus;
            defaultFocusPoint = focus;

            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1.6f;
            defaultPitch = aspect >= 1.15f ? 34f : 46f;
            pitch = defaultPitch;

            distance = Mathf.Clamp(initialDistance, minDistance, maxDistance);
            targetDistance = distance;
            defaultDistance = distance;
            defaultYaw = yaw;
            configured = true;
            SaveRestSnapshot();
            ApplyTransformImmediate();
        }

        public void ConfigureGameplayView(
            Camera camera,
            BoardLayout layout,
            Bounds contentBounds,
            float hudBottomReserve,
            float padding)
        {
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1.6f;
            defaultPitch = aspect >= 1.2f ? 48f : aspect >= 1.05f ? 52f : 58f;
            pitch = defaultPitch;
            defaultYaw = yaw;

            Vector3 focus = contentBounds.center;
            Vector3 boardForward = layout != null ? layout.Origin.forward : Vector3.forward;
            focus += boardForward * (contentBounds.extents.z * 0.18f);
            focus.y = contentBounds.min.y + contentBounds.size.y * 0.42f;

            defaultFocusPoint = focus;
            targetFocusPoint = focus;
            focusPoint = focus;

            float fitDistance = GameplayCameraFraming.ComputeFitDistance(camera, contentBounds, defaultPitch, padding);
            fitDistance *= 1f + Mathf.Clamp01(hudBottomReserve) * 0.55f;

            defaultDistance = Mathf.Clamp(fitDistance, minDistance, maxDistance);
            targetDistance = defaultDistance;
            distance = defaultDistance;
            configured = true;
            SaveRestSnapshot();
            ApplyTransformImmediate();
        }

        public void ReframeGameplayView(Camera camera, BoardLayout layout, Bounds contentBounds, float hudBottomReserve, float padding)
        {
            ConfigureGameplayView(camera, layout, contentBounds, hudBottomReserve, padding);
        }

        public void FocusPlacementTarget(int coordinate, float travelDuration, bool force = false)
        {
            if (!ShouldUseCinematicFocus(force) || BoardManager.Instance == null)
                return;

            SaveRestSnapshot();
            Vector3 world = BoardManager.Instance.GetPieceWorldPosition(coordinate);
            Vector3 focus = Vector3.Lerp(restFocusPoint, world, actionFocusBlend);
            BeginCinematicFocus(focus, restDistance * actionZoomFactor, travelDuration);
        }

        public void FocusMove(int fromCoordinate, int toCoordinate, float travelDuration, bool force = false)
        {
            if (!ShouldUseCinematicFocus(force) || BoardManager.Instance == null)
                return;

            SaveRestSnapshot();
            Vector3 from = BoardManager.Instance.GetPieceWorldPosition(fromCoordinate);
            Vector3 to = BoardManager.Instance.GetPieceWorldPosition(toCoordinate);
            Vector3 mid = Vector3.Lerp(from, to, 0.5f);
            Vector3 focus = Vector3.Lerp(restFocusPoint, mid, actionFocusBlend);
            BeginCinematicFocus(focus, restDistance * actionZoomFactor, travelDuration);
        }

        public void ReleaseCinematicFocus(bool force = false, int restCoordinate = -1)
        {
            if (!ShouldUseCinematicFocus(force) || userControllingCamera)
                return;

            BeginReturnToRest(returnDuration);
        }

        private bool ShouldUseCinematicFocus(bool force = false) =>
            configured && (force || (enableCinematicFocus && !GameSession.ReduceMotion));

        private void LateUpdate()
        {
            if (!configured)
                return;

            touchGestureThisFrame = false;
            HandleTouchInput();
            userControllingCamera = IsUserControllingCamera();
            HandleInput();
            SmoothValues();
            ApplyTransformImmediate();
        }

        private bool IsUserControllingCamera()
        {
            if (touchGestureThisFrame)
                return true;

            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;
            if (mouse == null)
                return false;

            if (mouse.rightButton.isPressed || mouse.middleButton.isPressed)
                return true;

            if (keyboard != null &&
                (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed) &&
                mouse.leftButton.isPressed)
            {
                return true;
            }

            return Mathf.Abs(mouse.scroll.ReadValue().y) > 0.01f;
        }

        /// <summary>Two-finger pinch to zoom, twist to orbit, drag to pan. One finger stays free for the board.</summary>
        private void HandleTouchInput()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                twoFingerTouchActive = false;
                return;
            }

            Vector2 a = default;
            Vector2 b = default;
            int activeCount = 0;

            foreach (var touch in touchscreen.touches)
            {
                if (!touch.press.isPressed)
                    continue;

                if (activeCount == 0)
                    a = touch.position.ReadValue();
                else if (activeCount == 1)
                    b = touch.position.ReadValue();

                activeCount++;
                if (activeCount >= 2)
                    break;
            }

            if (activeCount < 2)
            {
                twoFingerTouchActive = false;
                return;
            }

            if (!twoFingerTouchActive)
            {
                twoFingerTouchActive = true;
                lastTouchA = a;
                lastTouchB = b;
                return;
            }

            touchGestureThisFrame = true;

            float previousDistance = Vector2.Distance(lastTouchA, lastTouchB);
            float currentDistance = Vector2.Distance(a, b);
            float pinchDelta = currentDistance - previousDistance;
            if (Mathf.Abs(pinchDelta) > 0.01f)
                targetDistance = Mathf.Clamp(targetDistance - pinchDelta * touchPinchZoomSensitivity, minDistance, maxDistance);

            Vector2 previousMid = (lastTouchA + lastTouchB) * 0.5f;
            Vector2 currentMid = (a + b) * 0.5f;
            Vector2 midDelta = currentMid - previousMid;

            Vector2 previousSpan = lastTouchB - lastTouchA;
            Vector2 currentSpan = b - a;
            float twistDegrees = Vector2.SignedAngle(previousSpan, currentSpan);

            if (Mathf.Abs(twistDegrees) > 0.05f)
                yaw += twistDegrees * touchOrbitSensitivity;
            else
                PanScreenDelta(-midDelta.x, -midDelta.y, touchPanSensitivity);

            lastTouchA = a;
            lastTouchB = b;
            SaveRestSnapshot();
        }

        private void HandleInput()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null)
                return;

            if (userControllingCamera && cinematicRoutine != null)
            {
                StopCoroutine(cinematicRoutine);
                cinematicRoutine = null;
                SaveRestSnapshot();
            }

            bool manualInput = false;
            bool fast = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            float speedScale = fast ? fastMultiplier : 1f;

            if (mouse != null)
            {
                Vector2 delta = mouse.delta.ReadValue();

                bool orbitDrag = mouse.rightButton.isPressed ||
                                 (mouse.leftButton.isPressed && (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed));
                if (orbitDrag)
                {
                    yaw += delta.x * orbitSensitivity;
                    pitch -= delta.y * orbitSensitivity;
                    pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
                    manualInput = true;
                }

                if (mouse.middleButton.isPressed)
                {
                    PanScreenDelta(-delta.x, -delta.y, panDragSensitivity * speedScale);
                    manualInput = true;
                }

                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    targetDistance = Mathf.Clamp(targetDistance - scroll * scrollSensitivity * 0.01f, minDistance, maxDistance);
                    manualInput = true;
                }
            }

            float horizontal = 0f;
            float vertical = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical -= 1f;

            if (Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f)
            {
                Vector3 right = transform.right;
                right.y = 0f;
                right.Normalize();
                Vector3 forward = Vector3.Cross(Vector3.up, right);
                targetFocusPoint += (right * horizontal + forward * vertical) * (panSpeed * speedScale * Time.unscaledDeltaTime);
                manualInput = true;
            }

            float zoomInput = 0f;
            if (keyboard.leftBracketKey.isPressed) zoomInput -= 1f;
            if (keyboard.rightBracketKey.isPressed) zoomInput += 1f;
            if (Mathf.Abs(zoomInput) > 0.01f)
            {
                targetDistance = Mathf.Clamp(targetDistance + zoomInput * zoomSpeed * speedScale * Time.unscaledDeltaTime, minDistance, maxDistance);
                manualInput = true;
            }

            if (manualInput && cinematicRoutine == null)
                SaveRestSnapshot();

            if (keyboard.homeKey.wasPressedThisFrame || keyboard.cKey.wasPressedThisFrame)
            {
                var layout = FindAnyObjectByType<BoardLayout>();
                if (layout != null && GameplayCameraFraming.TryBuildGameplayBounds(layout, out Bounds bounds))
                    ReframeGameplayView(Camera.main, layout, bounds, 0.16f, 1.22f);
                else
                    ResetView();
            }
        }

        private void PanScreenDelta(float deltaX, float deltaY, float sensitivity)
        {
            Vector3 right = transform.right;
            right.y = 0f;
            right.Normalize();
            Vector3 forward = Vector3.Cross(Vector3.up, right);
            targetFocusPoint += (-right * deltaX - forward * deltaY) * sensitivity * targetDistance;
        }

        private void ResetView()
        {
            if (cinematicRoutine != null)
            {
                StopCoroutine(cinematicRoutine);
                cinematicRoutine = null;
            }

            targetFocusPoint = defaultFocusPoint;
            yaw = defaultYaw;
            pitch = defaultPitch;
            targetDistance = defaultDistance;
            SaveRestSnapshot();
        }

        private void SaveRestSnapshot()
        {
            restFocusPoint = targetFocusPoint;
            restDistance = targetDistance;
            restYaw = yaw;
            restPitch = pitch;
        }

        private void BeginCinematicFocus(Vector3 focus, float zoomDistance, float duration)
        {
            if (userControllingCamera)
                return;

            if (cinematicRoutine != null)
                StopCoroutine(cinematicRoutine);

            cinematicRoutine = StartCoroutine(CinematicFocusRoutine(focus, zoomDistance, duration));
        }

        private void BeginReturnToRest(float duration)
        {
            if (userControllingCamera)
                return;

            if (cinematicRoutine != null)
                StopCoroutine(cinematicRoutine);

            cinematicRoutine = StartCoroutine(CinematicReturnRoutine(
                restFocusPoint,
                restDistance,
                restYaw,
                restPitch,
                duration));
        }

        private IEnumerator CinematicFocusRoutine(Vector3 focus, float zoomDistance, float duration)
        {
            Vector3 startFocus = targetFocusPoint;
            float startDistance = targetDistance;
            float elapsed = 0f;
            duration = Mathf.Max(0.12f, duration);

            while (elapsed < duration)
            {
                if (userControllingCamera)
                    yield break;

                elapsed += Time.unscaledDeltaTime;
                float u = EaseOutCubic(Mathf.Clamp01(elapsed / duration));
                targetFocusPoint = Vector3.Lerp(startFocus, focus, u);
                targetDistance = Mathf.Lerp(startDistance, zoomDistance, u);
                yield return null;
            }

            targetFocusPoint = focus;
            targetDistance = zoomDistance;
            cinematicRoutine = null;
        }

        private IEnumerator CinematicReturnRoutine(
            Vector3 focus,
            float zoomDistance,
            float returnYaw,
            float returnPitch,
            float duration)
        {
            Vector3 startFocus = targetFocusPoint;
            float startDistance = targetDistance;
            float startYaw = yaw;
            float startPitch = pitch;
            float elapsed = 0f;
            duration = Mathf.Max(0.12f, duration);

            while (elapsed < duration)
            {
                if (userControllingCamera)
                    yield break;

                elapsed += Time.unscaledDeltaTime;
                float u = EaseOutCubic(Mathf.Clamp01(elapsed / duration));
                targetFocusPoint = Vector3.Lerp(startFocus, focus, u);
                targetDistance = Mathf.Lerp(startDistance, zoomDistance, u);
                yaw = Mathf.LerpAngle(startYaw, returnYaw, u);
                pitch = Mathf.Lerp(startPitch, returnPitch, u);
                yield return null;
            }

            targetFocusPoint = focus;
            targetDistance = zoomDistance;
            yaw = returnYaw;
            pitch = returnPitch;
            cinematicRoutine = null;
        }

        private static float EaseOutCubic(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u;
        }

        private void SmoothValues()
        {
            float t = 1f - Mathf.Exp(-moveSmoothing * Time.unscaledDeltaTime);
            focusPoint = Vector3.Lerp(focusPoint, targetFocusPoint, t);
            distance = Mathf.Lerp(distance, targetDistance, t);
        }

        private void ApplyTransformImmediate()
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -distance);
            transform.position = focusPoint + offset;
            transform.rotation = rotation;
        }
    }
}
