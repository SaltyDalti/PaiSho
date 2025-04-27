using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(Light))]
public class MatchSunToSkybox : MonoBehaviour
{
    [Tooltip("How much to boost the intensity of the color")]
    [Range(0f, 5f)]
    public float intensityMultiplier = 1f;

    [Tooltip("Smoothing for transitions (0 = instant, higher = slower)")]
    public float smoothSpeed = 2f;

    private Light directionalLight;
    private Color targetColor;

    void OnEnable()
    {
        directionalLight = GetComponent<Light>();
        UpdateLightColor();
    }

    void Update()
    {
        UpdateLightColor();
    }

    void UpdateLightColor()
    {
        // Sample the skybox ambient lighting from Unity's lighting settings
        Color ambientColor = RenderSettings.ambientSkyColor;

        // Use it as the light color, possibly boosted
        targetColor = ambientColor * intensityMultiplier;

        // Smooth transition
        directionalLight.color = Color.Lerp(directionalLight.color, targetColor, Time.deltaTime * smoothSpeed);
    }
}
