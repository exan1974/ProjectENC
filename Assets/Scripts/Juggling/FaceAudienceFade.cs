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
    [Tooltip("Time in seconds to fade in/out.")]
    public float fadeDuration = 0.5f;

    // Internal state
    private float currentAlpha = 0f;
    private float cosThreshold;

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
            col.a = 0f;
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
    }

    void Update()
    {
        if (character == null) return;

        // Determine if character is in the 'no-see' zone (not facing audience)
        float dot = Vector3.Dot(character.forward.normalized, Vector3.forward);
        bool inNoSee = dot < cosThreshold;

        // Target alpha: 1 when in no-see zone, 0 otherwise
        float targetAlpha = inNoSee ? 1f : 0f;

        // Smoothly update currentAlpha towards targetAlpha
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime / fadeDuration);

        // Apply alpha to material
        if (targetMaterial.HasProperty("_Color"))
        {
            Color col = targetMaterial.color;
            col.a = currentAlpha;
            targetMaterial.color = col;
        }
    }
}