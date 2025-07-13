using UnityEngine;
using System.Collections.Generic;

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
    [Tooltip("Materials whose alpha will fade in sequence when entering/exiting the no-see zone.")]
    public List<Material> targetMaterials = new List<Material>();

    [Header("Fade Settings")]
    [Tooltip("Minimum time in seconds for the fade transition (applies to both in and out, randomized each time).")]
    [SerializeField] private float m_MinFadeDuration = 0.5f;
    [Tooltip("Maximum time in seconds for the fade transition (applies to both in and out, randomized each time).")]
    [SerializeField] private float m_MaxFadeDuration = 1.5f;
    [Tooltip("How long the screen stays black on scene load.")]
    [HideInInspector] public float initialBlackDuration = 1f;

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
    private float fadeProgress = 0f;
    private float cosThreshold;
    private bool wasLastStateFacingAudience;
    private float blackTimer = 0f;
    private bool blackTimerJustEnded = false;
    private bool hasFadedOutOnce = false;
    private float m_CurrentFadeDuration = 1f;

    void OnValidate()
    {
        m_MinFadeDuration = Mathf.Max(0.01f, m_MinFadeDuration);
        m_MaxFadeDuration = Mathf.Max(0.01f, m_MaxFadeDuration);
        if (m_MinFadeDuration > m_MaxFadeDuration)
        {
            float temp = m_MinFadeDuration;
            m_MinFadeDuration = m_MaxFadeDuration;
            m_MaxFadeDuration = temp;
        }
    }

    void Start()
    {
        if (targetMaterials == null || targetMaterials.Count == 0)
        {
            Debug.LogError("FaceAudienceMaterialFade: No targetMaterials assigned.");
            enabled = false;
            return;
        }
        // Prepare all materials for transparency
        foreach (var mat in targetMaterials)
        {
            if (mat == null) continue;
            if (mat.HasProperty("_Color"))
            {
                Color col = mat.color;
                col.a = 1f;
                mat.color = col;
                if (mat.shader.name.Contains("Standard"))
                {
                    mat.SetFloat("_Mode", 2f);
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                }
            }
        }
        cosThreshold = Mathf.Cos(angleThreshold * Mathf.Deg2Rad);
        blackTimer = 0f;
        blackTimerJustEnded = false;
        m_CurrentFadeDuration = Random.Range(m_MinFadeDuration, m_MaxFadeDuration);
    }

    void Update()
    {
        if (targetMaterials == null || targetMaterials.Count == 0) return;
        int matCount = targetMaterials.Count;
        if (blackTimer < initialBlackDuration)
        {
            blackTimer += Time.deltaTime;
            foreach (var mat in targetMaterials)
            {
                if (mat == null) continue;
                if (mat.HasProperty("_Color"))
                {
                    Color col = mat.color;
                    col.a = 1f;
                    mat.color = col;
                }
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
            float[] initialAlphas = new float[matCount];
            for (int i = 0; i < matCount; i++) initialAlphas[i] = isFacingAudienceInit ? 1f : 1f;
            for (int i = 0; i < matCount; i++)
            {
                var mat = targetMaterials[i];
                if (mat == null) continue;
                if (mat.HasProperty("_Color"))
                {
                    Color col = mat.color;
                    col.a = initialAlphas[i];
                    mat.color = col;
                }
            }
            blackTimerJustEnded = false;
        }
        if (character == null) return;
        float dot = Vector3.Dot(character.forward.normalized, Vector3.forward);
        bool isFacingAudience = dot >= cosThreshold;
        if (wasLastStateFacingAudience != isFacingAudience)
        {
            fadeProgress = 0f;
            m_CurrentFadeDuration = Random.Range(m_MinFadeDuration, m_MaxFadeDuration);
        }
        AnimationCurve currentCurve = isFacingAudience ? fadeInCurve : fadeOutCurve;
        float fadeDuration = Mathf.Max(m_CurrentFadeDuration, 0.001f);
        float totalFadeDuration = fadeDuration * matCount;
        fadeProgress = Mathf.Min(fadeProgress + (Time.deltaTime / totalFadeDuration), 1f);
        // For fade-in, fade in reverse order; for fade-out, fade in normal order
        for (int i = 0; i < matCount; i++)
        {
            int matIndex = isFacingAudience ? (matCount - 1 - i) : i;
            float matStart = (float)i / matCount;
            float matEnd = (float)(i + 1) / matCount;
            float matLocalProgress = Mathf.InverseLerp(matStart, matEnd, fadeProgress);
            matLocalProgress = Mathf.Clamp01(matLocalProgress);
            float evaluatedProgress = currentCurve.Evaluate(matLocalProgress);
            float alpha = isFacingAudience ? evaluatedProgress : 1f - evaluatedProgress;
            // Only allow fade-in if hasFadedOutOnce is true
            if (isFacingAudience && !hasFadedOutOnce)
                alpha = 1f;
            if (!isFacingAudience && alpha <= 0.001f)
                hasFadedOutOnce = true;
            var mat = targetMaterials[matIndex];
            if (mat == null) continue;
            if (mat.HasProperty("_Color"))
            {
                Color col = mat.color;
                col.a = alpha;
                mat.color = col;
            }
        }
        wasLastStateFacingAudience = isFacingAudience;
    }
}