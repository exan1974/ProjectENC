using UnityEngine;

public class TVStaticController : MonoBehaviour
{
    [Header("TV Static Settings")]
    [SerializeField] private Material tvStaticMaterial;
    [SerializeField] private bool enableStatic = true;
    
    [Header("Static Parameters")]
    [Range(0, 1)]
    [SerializeField] private float staticIntensity = 0.8f;
    [Range(0, 10)]
    [SerializeField] private float staticSpeed = 5f;
    [Range(0.1f, 10f)]
    [SerializeField] private float noiseScale = 1f;
    
    [Header("Scanline Effect")]
    [Range(0, 1)]
    [SerializeField] private float scanlines = 0.3f;
    [Range(0, 10)]
    [SerializeField] private float scanlineSpeed = 2f;
    
    [Header("Flicker Effect")]
    [Range(0, 10)]
    [SerializeField] private float flickerSpeed = 1f;
    [Range(0, 1)]
    [SerializeField] private float flickerIntensity = 0.1f;
    
    [Header("Transparency")]
    [Range(0, 1)]
    [SerializeField] private float transparency = 1f;
    [SerializeField] private bool animateTransparency = false;
    [SerializeField] private float transparencySpeed = 1f;
    [SerializeField] private float minTransparency = 0.3f;
    [SerializeField] private float maxTransparency = 1f;
    
    [Header("Animation")]
    [SerializeField] private bool animateParameters = false;
    [SerializeField] private float animationSpeed = 1f;
    [SerializeField] private float minIntensity = 0.3f;
    [SerializeField] private float maxIntensity = 1f;
    
    private Renderer m_renderer;
    private Material m_materialInstance;
    
    void Start()
    {
        // Get the renderer component
        m_renderer = GetComponent<Renderer>();
        
        if (m_renderer == null)
        {
            Debug.LogError("TVStaticController: No Renderer component found!");
            return;
        }
        
        // Create a material instance to avoid modifying the original
        if (tvStaticMaterial != null)
        {
            m_materialInstance = new Material(tvStaticMaterial);
            m_renderer.material = m_materialInstance;
        }
        else
        {
            Debug.LogWarning("TVStaticController: No TV Static Material assigned!");
        }
        
        // Apply initial settings
        UpdateMaterialProperties();
    }
    
    void Update()
    {
        if (!enableStatic || m_materialInstance == null) return;
        
        // Animate parameters if enabled
        if (animateParameters)
        {
            AnimateStaticParameters();
        }
        
        // Update material properties
        UpdateMaterialProperties();
    }
    
    private void UpdateMaterialProperties()
    {
        if (m_materialInstance == null) return;
        
        m_materialInstance.SetFloat("_StaticIntensity", staticIntensity);
        m_materialInstance.SetFloat("_StaticSpeed", staticSpeed);
        m_materialInstance.SetFloat("_NoiseScale", noiseScale);
        m_materialInstance.SetFloat("_Scanlines", scanlines);
        m_materialInstance.SetFloat("_ScanlineSpeed", scanlineSpeed);
        m_materialInstance.SetFloat("_FlickerSpeed", flickerSpeed);
        m_materialInstance.SetFloat("_FlickerIntensity", flickerIntensity);
        m_materialInstance.SetFloat("_Transparency", transparency);
    }
    
    private void AnimateStaticParameters()
    {
        // Animate static intensity
        staticIntensity = Mathf.Lerp(minIntensity, maxIntensity, 
            (Mathf.Sin(Time.time * animationSpeed) + 1f) * 0.5f);
        
        // Slightly animate noise scale
        noiseScale = 1f + Mathf.Sin(Time.time * animationSpeed * 0.5f) * 0.2f;
        
        // Animate transparency if enabled
        if (animateTransparency)
        {
            transparency = Mathf.Lerp(minTransparency, maxTransparency,
                (Mathf.Sin(Time.time * transparencySpeed) + 1f) * 0.5f);
        }
    }
    
    // Public methods for external control
    public void SetStaticIntensity(float intensity)
    {
        staticIntensity = Mathf.Clamp01(intensity);
    }
    
    public void SetStaticSpeed(float speed)
    {
        staticSpeed = Mathf.Clamp(speed, 0, 10);
    }
    
    public void EnableStatic(bool enable)
    {
        enableStatic = enable;
        if (m_renderer != null)
        {
            m_renderer.enabled = enable;
        }
    }
    
    public void SetScanlines(float intensity)
    {
        scanlines = Mathf.Clamp01(intensity);
    }
    
    public void SetFlicker(float intensity)
    {
        flickerIntensity = Mathf.Clamp01(intensity);
    }
    
    public void SetTransparency(float alpha)
    {
        transparency = Mathf.Clamp01(alpha);
    }
    
    public void EnableTransparencyAnimation(bool enable)
    {
        animateTransparency = enable;
    }
    
    public void SetTransparencyRange(float min, float max)
    {
        minTransparency = Mathf.Clamp01(min);
        maxTransparency = Mathf.Clamp01(max);
    }
    
    // Preset methods for different TV states
    public void SetPreset_NoSignal()
    {
        staticIntensity = 0.9f;
        staticSpeed = 8f;
        noiseScale = 1.2f;
        scanlines = 0.4f;
        scanlineSpeed = 3f;
        flickerIntensity = 0.15f;
        flickerSpeed = 2f;
        transparency = 1f;
    }
    
    public void SetPreset_WeakSignal()
    {
        staticIntensity = 0.6f;
        staticSpeed = 4f;
        noiseScale = 0.8f;
        scanlines = 0.2f;
        scanlineSpeed = 1f;
        flickerIntensity = 0.05f;
        flickerSpeed = 0.5f;
        transparency = 0.8f;
    }
    
    public void SetPreset_StaticOnly()
    {
        staticIntensity = 1f;
        staticSpeed = 10f;
        noiseScale = 1.5f;
        scanlines = 0f;
        scanlineSpeed = 0f;
        flickerIntensity = 0f;
        flickerSpeed = 0f;
        transparency = 1f;
    }
    
    public void SetPreset_Ghostly()
    {
        staticIntensity = 0.4f;
        staticSpeed = 3f;
        noiseScale = 0.6f;
        scanlines = 0.1f;
        scanlineSpeed = 0.5f;
        flickerIntensity = 0.2f;
        flickerSpeed = 1.5f;
        transparency = 0.3f;
    }
    
    void OnDestroy()
    {
        // Clean up material instance
        if (m_materialInstance != null)
        {
            DestroyImmediate(m_materialInstance);
        }
    }
} 