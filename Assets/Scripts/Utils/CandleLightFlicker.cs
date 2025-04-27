using UnityEngine;

public class CandleLightFlicker : MonoBehaviour
{
    // Public variables to adjust in the Inspector
    public Color lightColor = Color.white;  // Base light color (for default color of flame)
    public float minIntensity = 0.5f;  // Minimum intensity of the light
    public float maxIntensity = 1.5f;  // Maximum intensity of the light
    public float flickerSpeed = 0.1f;  // Speed of flicker variation
    public float flickerAmount = 0.2f;  // Amount of random flicker intensity fluctuation

    public Color minColor = new Color(1f, 0.5f, 0f);  // Minimum color (e.g., dark orange)
    public Color maxColor = new Color(1f, 0.9f, 0.2f);  // Maximum color (e.g., bright yellow)

    private Light pointLight;  // Reference to the PointLight component
    private float targetIntensity;  // Target intensity for flicker
    private Color targetColor;  // Target color for flicker

    void Start()
    {
        // Get the Light component attached to the same GameObject
        pointLight = GetComponent<Light>();

        if (pointLight == null)
        {
            Debug.LogError("No Light component found on this GameObject.");
            return;
        }

        // Set the initial color of the light
        pointLight.color = lightColor;

        // Set the initial intensity to a random value between min and max
        targetIntensity = Random.Range(minIntensity, maxIntensity);
        // Set the initial color to a random value between min and max colors
        targetColor = Color.Lerp(minColor, maxColor, Random.value);
    }

    void Update()
    {
        // Smoothly transition towards the target intensity and color
        pointLight.intensity = Mathf.Lerp(pointLight.intensity, targetIntensity, flickerSpeed * Time.deltaTime);
        pointLight.color = Color.Lerp(pointLight.color, targetColor, flickerSpeed * Time.deltaTime);

        // Randomize the target intensity and color for the next flicker
        if (Random.value < flickerAmount * Time.deltaTime)
        {
            targetIntensity = Random.Range(minIntensity, maxIntensity);
            targetColor = Color.Lerp(minColor, maxColor, Random.value);
        }
    }
}
