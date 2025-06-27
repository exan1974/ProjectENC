using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Rendering;
using URPGlitch;

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

    void Start()
    {
        if (analogGlitchVolume == null || digitalGlitchVolume == null)
        {
            Debug.LogError("[GlitchTimerController] Assign both analog and digital glitch volumes!");
            enabled = false;
            return;
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
    }

    private void OnSceneUnloaded(Scene scene)
    {
        // Reset glitch values when leaving the scene
        SetGlitchValues(0f);
    }

    void Update()
    {
        if (!isTimerRunning || !isGlitchActive) return;

        currentTime -= Time.deltaTime;
        UpdateTimerDisplay();

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            isTimerRunning = false;
            RestoreStoredSettings();
            UpdateTimerDisplay();
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
        SetGlitchValues(0f);
        UpdateTimerDisplay();
    }
} 