using UnityEngine;

[ExecuteAlways]
public class SunCycle : MonoBehaviour
{
    [Header("Cycle Settings")]
    [Tooltip("How long a full day lasts, in real-time seconds.")]
    public float dayLengthInSeconds = 120f;

    [Tooltip("Current time of day (0 = midnight, 0.5 = noon, 1 = next midnight)")]
    [Range(0f, 1f)]
    public float timeOfDay = 0f;

    [Tooltip("Automatically advance time of day in play mode")]
    public bool autoCycle = true;

    [Header("Sun Rotation Settings")]
    [Tooltip("Direction the sun rises from (e.g., 0,0,0 = straight forward)")]
    public Vector3 sunDirection = new Vector3(0f, 0f, 0f);

    [Tooltip("Tilt of the sun (e.g., 45 degrees for a typical sun path)")]
    public float sunTilt = 45f;

    private Light sun;

    void OnEnable()
    {
        sun = GetComponent<Light>();
    }

    void Update()
    {
        // Only update time of day if autoCycle is enabled and in play mode
        if (Application.isPlaying && autoCycle && dayLengthInSeconds > 0)
        {
            timeOfDay += Time.deltaTime / dayLengthInSeconds;
            if (timeOfDay > 1f) timeOfDay -= 1f;
        }

        UpdateSunRotation();
    }

    void UpdateSunRotation()
    {
        // Calculate the sun’s position over the course of a day
        float angle = timeOfDay * 360f - 90f; // Start at sunrise
        Quaternion sunRotation = Quaternion.Euler(angle, sunDirection.y, sunDirection.z);
        transform.rotation = Quaternion.Euler(sunTilt, 0f, 0f) * sunRotation;
    }
}
