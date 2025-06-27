using UnityEngine;

[ExecuteAlways]
public class FaceAudienceMaterialFade : MonoBehaviour
{
    [Header("Facing Settings")]
    [Tooltip("Character whose forward direction is checked.")]
    public Transform character;

    [Tooltip("Max angle (degrees) from world forward for 'facing audience'.")]
    [Range(0f, 180f)]
    public float angleThreshold = 45f;

    [Header("Fade Material")]
    [Tooltip("Material whose alpha will fade when entering/exiting the no-see zone.")]
    public Material targetMaterial;

    [Header("Fade Settings")]
    [Tooltip("Time in seconds to fade in (become visible).")]
    public float fadeInDuration = 0.5f;
    [Tooltip("Time in seconds to fade out (become transparent).")]
    public float fadeOutDuration = 0.5f;
    [Tooltip("How long the screen stays black on scene load.")]
    public float initialBlackDuration = 1f;

    [Header("Fade Curves")]
    [Tooltip("Animation curve for fade in. X-axis is time (0-1), Y-axis is alpha progress (0-1).")]
    public AnimationCurve fadeInCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0.1f, 0.1f),    // Start slow
        new Keyframe(0.7f, 0.3f, 1f, 1f),    // Accelerate in middle
        new Keyframe(1f, 1f, 2f, 2f)         // Fast at end
    );

    [Tooltip("Animation curve for fade out. X-axis is time (0-1), Y-axis is alpha progress (0-1).")]
    public AnimationCurve fadeOutCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0.1f, 0.1f),    // Start slow
        new Keyframe(0.7f, 0.3f, 1f, 1f),    // Accelerate in middle
        new Keyframe(1f, 1f, 2f, 2f)         // Fast at end
    );

    // Internal state
    private float currentAlpha = 0f;
    private float fadeProgress = 0f;
    private float cosThreshold;
    private bool wasLastStateFacingAudience;
    private float blackTimer = 0f;
    private bool blackTimerJustEnded = false;
    private bool hasFadedOutOnce = false;

    void Start()
    {
        if (targetMaterial == null)
        {
            Debug.LogError("FaceAudienceMaterialFade: No targetMaterial assigned.");
            enabled = false;
            return;
        }

        // Prepare material for transparency
        if (targetMaterial.HasProperty("_Color"))
        {
            Color col = targetMaterial.color;
            col.a = 1f;
            targetMaterial.color = col;

            // If using Standard shader, switch to transparent
            if (targetMaterial.shader.name.Contains("Standard"))
            {
                targetMaterial.SetFloat("_Mode", 2f);
                targetMaterial.EnableKeyword("_ALPHABLEND_ON");
                targetMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
        }

        // Precompute cosine of the threshold angle for dot comparison
        cosThreshold = Mathf.Cos(angleThreshold * Mathf.Deg2Rad);
        blackTimer = 0f;
        blackTimerJustEnded = false;
    }

    void Update()
    {
        if (blackTimer < initialBlackDuration)
        {
            blackTimer += Time.deltaTime;
            if (targetMaterial.HasProperty("_Color"))
            {
                Color col = targetMaterial.color;
                col.a = 1f;
                targetMaterial.color = col;
            }
            if (blackTimer >= initialBlackDuration)
            {
                blackTimerJustEnded = true;
            }
            return;
        }

        if (blackTimerJustEnded)
        {
            float dotInit = character != null ? Vector3.Dot(character.forward.normalized, Vector3.forward) : 1f;
            bool isFacingAudienceInit = dotInit >= cosThreshold;
            if (isFacingAudienceInit)
            {
                fadeProgress = 1f;
                currentAlpha = 1f;
                if (targetMaterial.HasProperty("_Color"))
                {
                    Color col = targetMaterial.color;
                    col.a = 1f;
                    targetMaterial.color = col;
                }
            }
            else
            {
                fadeProgress = 0f;
                currentAlpha = 1f;
                if (targetMaterial.HasProperty("_Color"))
                {
                    Color col = targetMaterial.color;
                    col.a = 1f;
                    targetMaterial.color = col;
                }
            }
            blackTimerJustEnded = false;
        }

        if (character == null) return;

        float dot = Vector3.Dot(character.forward.normalized, Vector3.forward);
        bool isFacingAudience = dot >= cosThreshold;

        // Only reset fadeProgress when changing direction
        if (wasLastStateFacingAudience != isFacingAudience)
        {
            fadeProgress = 0f;
        }

        // If facing the audience and fadeProgress is 1, do not increment further
        if (isFacingAudience && fadeProgress >= 1f)
        {
            // Only allow fade-in if hasFadedOutOnce is true
            currentAlpha = hasFadedOutOnce ? 1f : 1f;
        }
        else if (isFacingAudience && !hasFadedOutOnce)
        {
            // Before first fade-out, keep alpha at 1
            currentAlpha = 1f;
        }
        else
        {
            // Choose fade duration and curve based on direction
            float fadeDuration = isFacingAudience ? fadeInDuration : fadeOutDuration;
            AnimationCurve currentCurve = isFacingAudience ? fadeInCurve : fadeOutCurve;
            fadeDuration = Mathf.Max(fadeDuration, 0.001f);
            fadeProgress = Mathf.Min(fadeProgress + (Time.deltaTime / fadeDuration), 1f);
            float evaluatedProgress = currentCurve.Evaluate(fadeProgress);
            if (!isFacingAudience)
            {
                evaluatedProgress = 1f - evaluatedProgress;
            }
            currentAlpha = evaluatedProgress;
            // If fade-out just completed, set flag
            if (!isFacingAudience && currentAlpha <= 0.001f)
            {
                hasFadedOutOnce = true;
            }
        }

        // Apply alpha to material
        if (targetMaterial.HasProperty("_Color"))
        {
            Color col = targetMaterial.color;
            col.a = currentAlpha;
            targetMaterial.color = col;
        }

        wasLastStateFacingAudience = isFacingAudience;
    }
}