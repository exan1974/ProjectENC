using UnityEngine;

public class TVStaticParticles : MonoBehaviour
{
    [Header("Particle Settings")]
    [SerializeField] private int particleCount = 1000;
    [SerializeField] private float particleSpeed = 5f;
    [SerializeField] private float particleSize = 0.02f;
    [SerializeField] private Color particleColor = Color.white;
    
    [Header("Static Behavior")]
    [SerializeField] private float staticIntensity = 0.8f;
    [SerializeField] private float flickerSpeed = 1f;
    [SerializeField] private float flickerIntensity = 0.1f;
    
    [Header("Transparency")]
    [Range(0, 1)]
    [SerializeField] private float transparency = 1f;
    [SerializeField] private bool animateTransparency = false;
    [SerializeField] private float transparencySpeed = 1f;
    [SerializeField] private float minTransparency = 0.3f;
    [SerializeField] private float maxTransparency = 1f;
    
    private ParticleSystem m_particleSystem;
    private ParticleSystem.MainModule m_mainModule;
    private ParticleSystem.EmissionModule m_emissionModule;
    
    void Start()
    {
        SetupParticleSystem();
    }
    
    void Update()
    {
        // Animate flicker
        float flicker = 1f + Mathf.Sin(Time.time * flickerSpeed) * flickerIntensity;
        
        // Animate transparency if enabled
        float currentTransparency = transparency;
        if (animateTransparency)
        {
            currentTransparency = Mathf.Lerp(minTransparency, maxTransparency,
                (Mathf.Sin(Time.time * transparencySpeed) + 1f) * 0.5f);
        }
        
        // Apply flicker and transparency to particle color
        Color finalColor = particleColor * flicker;
        finalColor.a = currentTransparency;
        m_mainModule.startColor = finalColor;
        
        // Animate emission rate based on static intensity
        m_emissionModule.rateOverTime = particleCount * staticIntensity;
    }
    
    private void SetupParticleSystem()
    {
        // Get or create particle system
        m_particleSystem = GetComponent<ParticleSystem>();
        if (m_particleSystem == null)
        {
            m_particleSystem = gameObject.AddComponent<ParticleSystem>();
        }
        
        // Configure main module
        m_mainModule = m_particleSystem.main;
        m_mainModule.startLifetime = 0.1f;
        m_mainModule.startSpeed = particleSpeed;
        m_mainModule.startSize = particleSize;
        m_mainModule.startColor = particleColor;
        m_mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
        m_mainModule.maxParticles = particleCount;
        
        // Configure emission module
        m_emissionModule = m_particleSystem.emission;
        m_emissionModule.rateOverTime = particleCount * staticIntensity;
        
        // Configure shape module
        var shapeModule = m_particleSystem.shape;
        shapeModule.enabled = true;
        shapeModule.shapeType = ParticleSystemShapeType.Rectangle;
        shapeModule.scale = new Vector3(2f, 1f, 0.1f);
        
        // Configure velocity over lifetime
        var velocityModule = m_particleSystem.velocityOverLifetime;
        velocityModule.enabled = true;
        velocityModule.space = ParticleSystemSimulationSpace.World;
        velocityModule.x = new ParticleSystem.MinMaxCurve(-particleSpeed, particleSpeed);
        velocityModule.y = new ParticleSystem.MinMaxCurve(-particleSpeed, particleSpeed);
        velocityModule.z = new ParticleSystem.MinMaxCurve(0, 0);
        
        // Configure size over lifetime
        var sizeModule = m_particleSystem.sizeOverLifetime;
        sizeModule.enabled = true;
        sizeModule.size = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        
        // Configure color over lifetime with transparency
        var colorModule = m_particleSystem.colorOverLifetime;
        colorModule.enabled = true;
        colorModule.color = new ParticleSystem.MinMaxGradient(
            new Gradient()
            {
                colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(particleColor, 0f),
                    new GradientColorKey(particleColor, 1f)
                },
                alphaKeys = new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(transparency, 0.1f),
                    new GradientAlphaKey(transparency, 0.9f),
                    new GradientAlphaKey(0f, 1f)
                }
            }
        );
    }
    
    // Public methods for external control
    public void SetStaticIntensity(float intensity)
    {
        staticIntensity = Mathf.Clamp01(intensity);
        if (m_emissionModule.enabled)
        {
            m_emissionModule.rateOverTime = particleCount * staticIntensity;
        }
    }
    
    public void SetParticleColor(Color color)
    {
        particleColor = color;
        Color finalColor = color;
        finalColor.a = transparency;
        m_mainModule.startColor = finalColor;
    }
    
    public void SetTransparency(float alpha)
    {
        transparency = Mathf.Clamp01(alpha);
        Color finalColor = particleColor;
        finalColor.a = transparency;
        m_mainModule.startColor = finalColor;
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
    
    public void EnableStatic(bool enable)
    {
        if (m_particleSystem != null)
        {
            if (enable)
                m_particleSystem.Play();
            else
                m_particleSystem.Stop();
        }
    }
} 