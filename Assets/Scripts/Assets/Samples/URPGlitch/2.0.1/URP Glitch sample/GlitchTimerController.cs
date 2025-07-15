using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Rendering;
using URPGlitch;
using Cinemachine;

public class GlitchTimerController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI timerText;

    [Header("Timer Settings")]
    [SerializeField] private float timerDuration = 30f;
    private float currentTime;
    private bool isTimerRunning;

    // Public property to access timer state
    public bool IsTimerRunning => isTimerRunning;

    [Header("Glitch Settings")]
    [SerializeField] private Volume analogGlitchVolume;
    [SerializeField] private Volume digitalGlitchVolume;
    [SerializeField] private float storedScanLineJitter;
    [SerializeField] private float storedVerticalJump;
    [SerializeField] private float storedHorizontalShake;
    [SerializeField] private float storedColorDrift;
    [SerializeField] private float storedDigitalIntensity;
    [SerializeField] private bool isGlitchActive = false;
    [SerializeField] private KeyCode activateGlitchKey = KeyCode.G;

    [Header("Material and Transparency")]
    [SerializeField] private Material targetMaterial;
    [SerializeField] private float targetTransparency = 1f;
    private float m_currentTransparency = 0f;
    private bool m_isLerpingTransparency = false;
    private bool m_transparencySet = false;

    [Header("Screen Switching")]
    [SerializeField] private GameObject screen1;
    [SerializeField] private GameObject screen2;
    [SerializeField] private CinemachineVirtualCamera cameraFirst;
    [SerializeField] private CinemachineVirtualCamera cameraSecond;
    private float transparencyLerpSpeed = 1f;

    private bool m_glitchMode = true;

    // Lerp control variables
    private float m_lerpStartValue = 0f;
    private float m_lerpProgress = 0f;

    void Start()
    {
        if (analogGlitchVolume == null || digitalGlitchVolume == null)
        {
            Debug.LogError("[GlitchTimerController] Assign both analog and digital glitch volumes!");
            enabled = false;
            return;
        }

        // Set transparency to 0 initially
        if (targetMaterial != null && targetMaterial.HasProperty("_Transparency"))
        {
            targetMaterial.SetFloat("_Transparency", 0f);
            m_currentTransparency = 0f;
        }
        
        // Set glitch values to zero
        SetGlitchValues(0f);
        
        // Initialize timer
        currentTime = timerDuration;
        isTimerRunning = true;
        
        // Subscribe to scene change event
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void OnDestroy()
    {
        // Unsubscribe from scene change event
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        
        // Reset glitch values when destroyed
        SetGlitchValues(0f);
        ResetTransparency();
    }

    private void OnSceneUnloaded(Scene scene)
    {
        // Reset glitch values when leaving the scene
        SetGlitchValues(0f);
        ResetTransparency();
    }

    void Update()
    {
        if (!isTimerRunning) return;

        // Check for glitch activation key
        if (Input.GetKeyDown(activateGlitchKey))
        {
            ActivateGlitch();
        }

        if (m_glitchMode)
        {
            // Original glitch timer logic
            if (isGlitchActive)
            {
                // Start transparency lerp when glitch becomes active
                if (!m_transparencySet)
                {
                    StartTransparencyLerp();
                    m_transparencySet = true;
                }

                // Update transparency lerp if active
                if (m_isLerpingTransparency)
                {
                    UpdateTransparencyLerp();
                }

                currentTime -= Time.deltaTime;
                UpdateTimerDisplay();

                if (currentTime <= 0f)
                {
                    currentTime = 0f;
                    isTimerRunning = false;
                    RestoreStoredSettings();
                    UpdateTimerDisplay();
                    
                    // Switch to screen switching mode
                    m_glitchMode = false;
                    SwitchScreensBasedOnCameraPriority();
                    m_isLerpingTransparency = true;
                    Debug.Log("[GlitchTimerController] Timer expired, starting transparency lerp");
                }
            }
            else
            {
                // Reset transparency flag when glitch becomes inactive
                m_transparencySet = false;
                m_isLerpingTransparency = false;
                
                // If glitch is not active, still count down timer
                currentTime -= Time.deltaTime;
                UpdateTimerDisplay();

                if (currentTime <= 0f)
                {
                    currentTime = 0f;
                    isTimerRunning = false;
                    UpdateTimerDisplay();
                    
                    // Switch to screen switching mode
                    m_glitchMode = false;
                    SwitchScreensBasedOnCameraPriority();
                    m_isLerpingTransparency = true;
                    Debug.Log("[GlitchTimerController] Timer expired (glitch inactive), starting transparency lerp");
                }
            }
        }
        else if (m_isLerpingTransparency)
        {
            UpdateTransparencyLerp();
        }
    }

    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    private void SwitchScreensBasedOnCameraPriority()
    {
        if (screen1 == null || screen2 == null || cameraFirst == null || cameraSecond == null)
        {
            Debug.LogWarning("[GlitchTimerController] Screen or camera references not assigned!");
            return;
        }

        // Check which Cinemachine Virtual Camera has higher priority
        bool isCameraFirstActive = cameraFirst.Priority > cameraSecond.Priority;
        
        // Activate appropriate screen
        screen1.SetActive(isCameraFirstActive);
        screen2.SetActive(!isCameraFirstActive);
        
        Debug.Log($"[GlitchTimerController] Switched to {(isCameraFirstActive ? "Screen1" : "Screen2")} based on Cinemachine camera priority (First: {cameraFirst.Priority}, Second: {cameraSecond.Priority})");
    }

    private void UpdateTransparencyLerp()
    {
        if (targetMaterial == null || !targetMaterial.HasProperty("_Transparency")) 
        {
            Debug.LogWarning("[GlitchTimerController] Target material or _Transparency property not found!");
            m_isLerpingTransparency = false;
            return;
        }

        // Calculate progress (0 to 1) based on time and speed
        m_lerpProgress += Time.deltaTime * transparencyLerpSpeed;
        m_lerpProgress = Mathf.Clamp01(m_lerpProgress);

        // Smooth interpolation
        float smoothedProgress = Mathf.SmoothStep(0f, 1f, m_lerpProgress);
        m_currentTransparency = Mathf.Lerp(m_lerpStartValue, targetTransparency, smoothedProgress);
        targetMaterial.SetFloat("_Transparency", m_currentTransparency);

        // Debug log can be removed in production
        Debug.Log($"[GlitchTimerController] Lerping transparency: {m_currentTransparency:F2} (Progress: {m_lerpProgress:F2})");

        // Check if lerp is complete
        if (m_lerpProgress >= 1f)
        {
            m_currentTransparency = targetTransparency;
            targetMaterial.SetFloat("_Transparency", targetTransparency);
            m_isLerpingTransparency = false;
            Debug.Log("[GlitchTimerController] Transparency lerp completed");
        }
    }

    private void StartTransparencyLerp()
    {
        if (targetMaterial != null && targetMaterial.HasProperty("_Transparency"))
        {
            m_lerpStartValue = 0f; // Always start from 0
            m_lerpProgress = 0f;
            m_isLerpingTransparency = true;
            
            // Initialize material
            targetMaterial.SetFloat("_Transparency", 0f);
            m_currentTransparency = 0f;
            
            Debug.Log($"[GlitchTimerController] Started transparency lerp (0 → {targetTransparency} in ~{1f/transparencyLerpSpeed:F2}s)");
        }
        else
        {
            Debug.LogWarning("[GlitchTimerController] Cannot start lerp - material missing");
            m_isLerpingTransparency = false;
        }
    }

    private void ResetTransparency()
    {
        if (targetMaterial != null && targetMaterial.HasProperty("_Transparency"))
        {
            targetMaterial.SetFloat("_Transparency", 0f);
            m_currentTransparency = 0f;
        }
        m_isLerpingTransparency = false;
        m_lerpProgress = 0f;
    }

    private void SetGlitchValues(float value)
    {
        // Set analog glitch values
        if (analogGlitchVolume.profile.TryGet(out AnalogGlitchVolume analogGlitch))
        {
            analogGlitch.scanLineJitter.value = value;
            analogGlitch.verticalJump.value = value;
            analogGlitch.horizontalShake.value = value;
            analogGlitch.colorDrift.value = value;
        }

        // Set digital glitch value
        if (digitalGlitchVolume.profile.TryGet(out DigitalGlitchVolume digitalGlitch))
        {
            digitalGlitch.intensity.value = value;
        }
    }

    private void RestoreStoredSettings()
    {
        // Restore analog glitch values
        if (analogGlitchVolume.profile.TryGet(out AnalogGlitchVolume analogGlitch))
        {
            analogGlitch.scanLineJitter.value = storedScanLineJitter;
            analogGlitch.verticalJump.value = storedVerticalJump;
            analogGlitch.horizontalShake.value = storedHorizontalShake;
            analogGlitch.colorDrift.value = storedColorDrift;
        }

        // Restore digital glitch value
        if (digitalGlitchVolume.profile.TryGet(out DigitalGlitchVolume digitalGlitch))
        {
            digitalGlitch.intensity.value = storedDigitalIntensity;
        }
    }

    public void RestartTimer()
    {
        currentTime = timerDuration;
        isTimerRunning = true;
        m_glitchMode = true;
        m_isLerpingTransparency = false;
        m_transparencySet = false;
        isGlitchActive = false;
        SetGlitchValues(0f);
        
        // Reset transparency to 0
        ResetTransparency();
        
        UpdateTimerDisplay();
    }

    // Public methods for external control
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
        m_transparencySet = false;
        isGlitchActive = false;
        currentTime = timerDuration;
        isTimerRunning = true;
        
        ResetTransparency();
        SetGlitchValues(0f);
        UpdateTimerDisplay();
    }

    // Public method to set target transparency
    public void SetTargetTransparency(float transparency)
    {
        targetTransparency = Mathf.Clamp01(transparency);
    }

    // Test method to manually trigger transparency lerping
    public void TestTransparencyLerp()
    {
        Debug.Log("[GlitchTimerController] Manually triggering transparency lerp test");
        m_glitchMode = false;
        StartTransparencyLerp();
    }

    // Test method to manually trigger glitch activation with lerp
    public void TestGlitchActivationWithLerp()
    {
        Debug.Log("[GlitchTimerController] Manually triggering glitch activation with lerp");
        ActivateGlitch();
        StartTransparencyLerp();
        m_transparencySet = true;
    }

    // Public method to activate glitch
    public void ActivateGlitch()
    {
        if (!isGlitchActive)
        {
            isGlitchActive = true;
            Debug.Log("[GlitchTimerController] Glitch activated!");
            
            // Store current glitch values before activating
            if (analogGlitchVolume.profile.TryGet(out AnalogGlitchVolume analogGlitch))
            {
                storedScanLineJitter = analogGlitch.scanLineJitter.value;
                storedVerticalJump = analogGlitch.verticalJump.value;
                storedHorizontalShake = analogGlitch.horizontalShake.value;
                storedColorDrift = analogGlitch.colorDrift.value;
            }

            if (digitalGlitchVolume.profile.TryGet(out DigitalGlitchVolume digitalGlitch))
            {
                storedDigitalIntensity = digitalGlitch.intensity.value;
            }
            
            // Set glitch values to maximum
            SetGlitchValues(1f);
        }
    }

    // Public method to deactivate glitch
    public void DeactivateGlitch()
    {
        if (isGlitchActive)
        {
            isGlitchActive = false;
            Debug.Log("[GlitchTimerController] Glitch deactivated!");
            RestoreStoredSettings();
            ResetTransparency();
            m_transparencySet = false;
        }
    }

    // Method to check current state for debugging
    public void DebugCurrentState()
    {
        Debug.Log($"[GlitchTimerController] Current State:");
        Debug.Log($"  - Timer Running: {isTimerRunning}");
        Debug.Log($"  - Glitch Mode: {m_glitchMode}");
        Debug.Log($"  - Glitch Active: {isGlitchActive}");
        Debug.Log($"  - Lerping Transparency: {m_isLerpingTransparency}");
        Debug.Log($"  - Transparency Set: {m_transparencySet}");
        Debug.Log($"  - Current Time: {currentTime}");
        Debug.Log($"  - Current Transparency: {m_currentTransparency}");
        Debug.Log($"  - Target Transparency: {targetTransparency}");
        Debug.Log($"  - Target Material: {(targetMaterial != null ? "Assigned" : "NULL")}");
        if (targetMaterial != null)
        {
            Debug.Log($"  - Has _Transparency Property: {targetMaterial.HasProperty("_Transparency")}");
            Debug.Log($"  - Current Material Transparency: {targetMaterial.GetFloat("_Transparency")}");
        }
    }
}