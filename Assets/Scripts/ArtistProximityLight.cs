using UnityEngine;

public class ArtistProximityLight : MonoBehaviour
{
    [Header("Artist References")]
    [Tooltip("First artist transform")]
    public Transform artist1;
    [Tooltip("Second artist transform")]
    public Transform artist2;

    [Header("Light Settings")]
    [Tooltip("The directional light to control")]
    public Light directionalLight;

    [Header("Intensity Control")]
    [Tooltip("Minimum light intensity when artists are far apart")]
    public float minIntensity = 0.5f;
    [Tooltip("Maximum light intensity when artists are close")]
    public float maxIntensity = 2f;
    [Tooltip("Minimum distance for max intensity")]
    public float minDistance = 1f;
    [Tooltip("Maximum distance for min intensity")]
    public float maxDistance = 10f;

    [Header("Smoothing")]
    [Tooltip("Curve to control intensity transition")]
    public AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("How quickly the light intensity changes")]
    [Range(0.1f, 10f)]
    public float smoothingSpeed = 2f;

    private float currentIntensity;
    private float targetIntensity;

    private void Start()
    {
        if (artist1 == null || artist2 == null || directionalLight == null)
        {
            Debug.LogError("[ArtistProximityLight] Please assign all required references in the Inspector.");
            enabled = false;
            return;
        }

        // Initialize with current light intensity
        currentIntensity = directionalLight.intensity;
        targetIntensity = currentIntensity;
    }

    private void Update()
    {
        if (artist1 == null || artist2 == null || directionalLight == null) return;

        // Calculate distance between artists
        float distance = Vector3.Distance(artist1.position, artist2.position);

        // Map distance to 0-1 range
        float normalizedDistance = Mathf.InverseLerp(maxDistance, minDistance, distance);
        
        // Clamp the normalized distance
        normalizedDistance = Mathf.Clamp01(normalizedDistance);

        // Apply the curve to get smooth transition
        float curveValue = intensityCurve.Evaluate(normalizedDistance);

        // Calculate target intensity
        targetIntensity = Mathf.Lerp(minIntensity, maxIntensity, curveValue);

        // Smoothly interpolate current intensity
        currentIntensity = Mathf.Lerp(
            currentIntensity,
            targetIntensity,
            Time.deltaTime * smoothingSpeed
        );

        // Apply the new intensity
        directionalLight.intensity = currentIntensity;
    }

    private void OnValidate()
    {
        // Ensure min values are less than max values
        minDistance = Mathf.Min(minDistance, maxDistance);
        minIntensity = Mathf.Min(minIntensity, maxIntensity);
    }
} 