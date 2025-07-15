using UnityEngine;
using UnityEngine.SceneManagement;

public class GlitchController : MonoBehaviour
{
    [Header("Original Glitch Settings")]
    public Transform character;
    public Material mat;
    public float maxNoiseAmount = 100f;
    public float maxGlitchStrength = 1f;
    public float maxScanLinesStrength = 1f;
    public float groundLevel = 0f;     // Normal y-value
    public float threshold = 2f;       // Distance from groundLevel where effects reach maximum

    [Header("Screen Switching")]
    public GameObject screen1;
    public GameObject screen2;
    public Camera cameraFirst;
    public Camera cameraSecond;
    public float transparencyLerpSpeed = 2f;

    [Header("Timer")]
    public float timerDuration = 10f;
    public bool useTimer = false;

    private float m_originalTransparency;
    private float m_currentTransparency = 0f;
    private bool m_isLerpingTransparency = false;
    private float m_timer = 0f;
    private bool m_glitchMode = true;

    void Start()
    {
        // Save original transparency value
        if (mat != null && mat.HasProperty("_Transparency"))
        {
            m_originalTransparency = mat.GetFloat("_Transparency");
            mat.SetFloat("_Transparency", 0f);
        }

        // Initialize glitch values to zero at start
        if (mat != null)
        {
            mat.SetFloat("_NoiseAmount", 0);
            mat.SetFloat("_GlitchStrength", 0);
            mat.SetFloat("_ScanLinesStrength", 0);
        }

        // Subscribe to scene change events
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void Update()
    {
        if (useTimer)
        {
            m_timer += Time.deltaTime;
            
            if (m_timer >= timerDuration && m_glitchMode)
            {
                // Switch from glitch mode to screen switching mode
                m_glitchMode = false;
                SwitchScreensBasedOnCameraPriority();
                m_isLerpingTransparency = true;
            }
        }

        if (m_glitchMode)
        {
            UpdateGlitchLogic();
        }
        else if (m_isLerpingTransparency)
        {
            UpdateTransparencyLerp();
        }
    }

    private void UpdateGlitchLogic()
    {
        if (character == null || mat == null) return;

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

    private void SwitchScreensBasedOnCameraPriority()
    {
        if (screen1 == null || screen2 == null || cameraFirst == null || cameraSecond == null)
        {
            Debug.LogWarning("GlitchController: Screen or camera references not assigned!");
            return;
        }

        // Check which camera has higher priority
        bool isCameraFirstActive = cameraFirst.depth > cameraSecond.depth;
        
        // Activate appropriate screen
        screen1.SetActive(isCameraFirstActive);
        screen2.SetActive(!isCameraFirstActive);
        
        Debug.Log($"GlitchController: Switched to {(isCameraFirstActive ? "Screen1" : "Screen2")} based on camera priority");
    }

    private void UpdateTransparencyLerp()
    {
        if (mat == null || !mat.HasProperty("_Transparency")) return;

        // Lerp transparency from 0 to original value
        m_currentTransparency = Mathf.Lerp(m_currentTransparency, m_originalTransparency, Time.deltaTime * transparencyLerpSpeed);
        mat.SetFloat("_Transparency", m_currentTransparency);

        // Check if lerping is complete
        if (Mathf.Abs(m_currentTransparency - m_originalTransparency) < 0.01f)
        {
            m_currentTransparency = m_originalTransparency;
            mat.SetFloat("_Transparency", m_originalTransparency);
            m_isLerpingTransparency = false;
            Debug.Log("GlitchController: Transparency lerp completed");
        }
    }

    private void OnSceneUnloaded(Scene scene)
    {
        RestoreOriginalTransparency();
    }

    void OnApplicationQuit()
    {
        RestoreOriginalTransparency();
    }

    void OnDestroy()
    {
        RestoreOriginalTransparency();
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void RestoreOriginalTransparency()
    {
        if (mat != null && mat.HasProperty("_Transparency"))
        {
            mat.SetFloat("_Transparency", m_originalTransparency);
            Debug.Log("GlitchController: Restored original transparency value");
        }
    }

    // Public methods for external control
    public void StartTimer()
    {
        useTimer = true;
        m_timer = 0f;
        m_glitchMode = true;
        m_isLerpingTransparency = false;
    }

    public void StopTimer()
    {
        useTimer = false;
        m_timer = 0f;
    }

    public void ForceScreenSwitch()
    {
        m_glitchMode = false;
        SwitchScreensBasedOnCameraPriority();
        m_isLerpingTransparency = true;
    }

    public void ResetToGlitchMode()
    {
        m_glitchMode = true;
        m_isLerpingTransparency = false;
        m_timer = 0f;
        
        // Reset transparency to 0
        if (mat != null && mat.HasProperty("_Transparency"))
        {
            mat.SetFloat("_Transparency", 0f);
            m_currentTransparency = 0f;
        }
    }
}