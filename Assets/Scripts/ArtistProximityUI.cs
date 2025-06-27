using UnityEngine;
using UnityEngine.UI;

public class ArtistProximityUI : MonoBehaviour
{
    [Header("Artist References")]
    [Tooltip("First artist transform")]
    public Transform artist1;
    [Tooltip("Second artist transform")]
    public Transform artist2;

    [Header("UI Reference")]
    [Tooltip("The UI Image to control")]
    public Image targetImage;

    [Header("Distance Settings")]
    [Tooltip("Minimum distance for full transparency")]
    public float minDistance = 1f;
    [Tooltip("Maximum distance for full opacity")]
    public float maxDistance = 10f;

    [Header("Smoothing")]
    [Tooltip("How quickly the alpha changes")]
    [Range(0.1f, 10f)]
    public float smoothingSpeed = 2f;

    private float currentAlpha;
    private float targetAlpha;
    private Color imageColor;

    private void Start()
    {
        if (artist1 == null || artist2 == null || targetImage == null)
        {
            Debug.LogError("[ArtistProximityUI] Please assign all required references in the Inspector.");
            enabled = false;
            return;
        }

        // Initialize with current image alpha
        imageColor = targetImage.color;
        currentAlpha = imageColor.a;
        targetAlpha = currentAlpha;
    }

    private void Update()
    {
        if (artist1 == null || artist2 == null || targetImage == null) return;

        // Calculate distance between artists
        float distance = Vector3.Distance(artist1.position, artist2.position);

        // Map distance to 0-1 range (closer = more transparent)
        float normalizedDistance = Mathf.InverseLerp(minDistance, maxDistance, distance);
        
        // Clamp the normalized distance
        normalizedDistance = Mathf.Clamp01(normalizedDistance);

        // Set target alpha (now closer = 0, further = 1)
        targetAlpha = normalizedDistance;

        // Smoothly interpolate current alpha
        currentAlpha = Mathf.Lerp(
            currentAlpha,
            targetAlpha,
            Time.deltaTime * smoothingSpeed
        );

        // Apply the new alpha
        imageColor.a = currentAlpha;
        targetImage.color = imageColor;
    }

    private void OnValidate()
    {
        // Ensure min distance is less than max distance
        minDistance = Mathf.Min(minDistance, maxDistance);
    }
} 