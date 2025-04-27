using UnityEngine;

namespace PaiSho.Game
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 10f;
        public float panSpeed = 0.5f;
        public float zoomSpeed = 1000f;
        public float minFOV = 20f;
        public float maxFOV = 60f;

        [Header("Home Position")]
        public Vector3 homePosition = new Vector3(14f, 20f, -20f);
        public Vector3 homeRotation = new Vector3(45f, 0f, 0f);

        private Camera cam;
        private Vector3 lastMousePosition;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            Cursor.lockState = CursorLockMode.None; // Free mouse!
            Cursor.visible = true;
        }

        private void Update()
        {
            HandleMovement();
            HandleZoom();
            HandleMousePanning();
            HandleHotkeys();
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            Vector3 direction = new Vector3(horizontal, 0f, vertical);
            transform.Translate(direction * moveSpeed * Time.deltaTime, Space.World);
        }

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            if (scroll != 0f)
            {
                cam.fieldOfView -= scroll * zoomSpeed * Time.deltaTime;
                cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, minFOV, maxFOV);
            }
        }

        private void HandleMousePanning()
        {
            if (Input.GetMouseButtonDown(2)) // Middle Mouse Button pressed
            {
                lastMousePosition = Input.mousePosition;
            }

            if (Input.GetMouseButton(2)) // Holding Middle Mouse Button
            {
                Vector3 delta = Input.mousePosition - lastMousePosition;
                Vector3 move = new Vector3(-delta.x, 0, -delta.y) * panSpeed * Time.deltaTime;

                transform.Translate(move, Space.Self);

                lastMousePosition = Input.mousePosition;
            }
        }

        private void HandleHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.Home))
            {
                SnapToHome();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                QuitGame();
            }
        }

        private void SnapToHome()
        {
            transform.position = homePosition;
            transform.eulerAngles = homeRotation;
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
