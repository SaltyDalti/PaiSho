using UnityEngine;

public class CameraFly : MonoBehaviour
{
    public float moveSpeed = 5f; // Speed at which the camera moves
    public float lookSpeed = 2f; // Speed at which the camera rotates
    public float verticalSpeed = 3f; // Speed for moving up/down (Q/E)
    public float pitchLimit = 80f; // Limit for vertical camera rotation (up/down)

    private float rotationX = 0f; // Rotation around the x-axis (up/down)
    private float rotationY = 0f; // Rotation around the y-axis (left/right)

    void Update()
    {
        // Movement input (WASD + QE)
        float moveX = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;
        float moveY = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;
        float moveZ = 0f;

        if (Input.GetKey(KeyCode.Q)) moveZ = -verticalSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.E)) moveZ = verticalSpeed * Time.deltaTime;

        transform.Translate(moveX, moveZ, moveY);

        // Mouse input for looking around (right-left & up-down)
        rotationX -= Input.GetAxis("Mouse Y") * lookSpeed;
        rotationY += Input.GetAxis("Mouse X") * lookSpeed;

        // Clamp the vertical rotation to prevent flipping upside down
        rotationX = Mathf.Clamp(rotationX, -pitchLimit, pitchLimit);

        // Apply the rotations
        transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0);
    }
}
