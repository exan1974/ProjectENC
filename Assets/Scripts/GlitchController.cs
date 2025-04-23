using UnityEngine;

public class GlitchController : MonoBehaviour
{
    public Transform character;
    public Material mat;

    public float maxNoiseAmount = 100f;
    public float maxGlitchStrength = 1f;
    public float maxScanLinesStrength = 1f;

    public float groundLevel = 0f;     // Normal y-value
    public float threshold = 2f;       // Distance from groundLevel where effects reach maximum

    void Start()
    {
        // Initialize values to zero at start
        mat.SetFloat("_NoiseAmount", 0);
        mat.SetFloat("_GlitchStrength", 0);
        mat.SetFloat("_ScanLinesStrength", 0);
    }

    void Update()
    {
        float distanceFromGround = Mathf.Abs(character.position.y - groundLevel);

        // Determine how much the character is deviating from groundLevel (normalized 0 to 1)
        float normalizedDeviation = Mathf.Clamp01(distanceFromGround / threshold);

        // Lerp glitch effects smoothly based on deviation
        float currentNoise = Mathf.Lerp(0, maxNoiseAmount, normalizedDeviation);
        float currentGlitch = Mathf.Lerp(0, maxGlitchStrength, normalizedDeviation);
        float currentScanLines = Mathf.Lerp(0, maxScanLinesStrength, normalizedDeviation);

        // Apply values to material
        mat.SetFloat("_NoiseAmount", currentNoise);
        mat.SetFloat("_GlitchStrength", currentGlitch);
        mat.SetFloat("_ScanLinesStrength", currentScanLines);
    }
}