using UnityEngine;

public class MaterialFadeZone : MonoBehaviour
{
    [Header("Material Settings")]
    [SerializeField] private Material m_targetMaterial;
    [SerializeField] private float m_maxAlpha = 1f;
    [SerializeField] private float m_minAlpha = 0f;
    [SerializeField] private float m_fadeSpeed = 1f;
    
    [Header("Optional Settings")]
    [SerializeField] private string m_playerTag = "Player";
    [Tooltip("If true, will find the _BaseColor property (URP). If false, will use _Color (Standard)")]
    [SerializeField] private bool m_isURPMaterial = true;

    private bool m_playerInZone = false;
    private float m_currentAlpha;
    private readonly string m_colorProperty = "_BaseColor";
    private readonly string m_standardColorProperty = "_Color";
    private Color m_originalColor;
    private string m_activeColorProperty;

    private void Start()
    {
        if (m_targetMaterial == null)
        {
            Debug.LogError("[MaterialFadeZone] Target material is not assigned!");
            enabled = false;
            return;
        }

        // Determine which color property to use
        m_activeColorProperty = m_isURPMaterial ? m_colorProperty : m_standardColorProperty;

        // Store the original color
        m_originalColor = m_targetMaterial.GetColor(m_activeColorProperty);
        
        // Set initial alpha
        m_currentAlpha = m_minAlpha;
        UpdateMaterialAlpha();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(m_playerTag))
        {
            m_playerInZone = true;
            Debug.Log("[MaterialFadeZone] Player entered fade zone");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(m_playerTag))
        {
            m_playerInZone = false;
            Debug.Log($"[MaterialFadeZone] Player exited fade zone. Alpha maintained at: {m_currentAlpha:F2}");
        }
    }

    private void Update()
    {
        if (m_targetMaterial == null) return;

        // Only increase alpha while player is in the zone
        if (m_playerInZone && m_currentAlpha < m_maxAlpha)
        {
            m_currentAlpha = Mathf.MoveTowards(m_currentAlpha, m_maxAlpha, m_fadeSpeed * Time.deltaTime);
            UpdateMaterialAlpha();
        }
    }

    private void UpdateMaterialAlpha()
    {
        Color newColor = m_originalColor;
        newColor.a = m_currentAlpha;
        m_targetMaterial.SetColor(m_activeColorProperty, newColor);
    }

    private void OnValidate()
    {
        // Clamp values for safety
        m_maxAlpha = Mathf.Clamp01(m_maxAlpha);
        m_minAlpha = Mathf.Clamp01(m_minAlpha);
        m_fadeSpeed = Mathf.Max(0, m_fadeSpeed);
    }

    // Optional: Add method to reset alpha if needed
    public void ResetAlpha()
    {
        m_currentAlpha = m_minAlpha;
        UpdateMaterialAlpha();
        Debug.Log("[MaterialFadeZone] Alpha reset to minimum");
    }

    private void OnApplicationQuit()
    {
        if (m_targetMaterial != null)
        {
            // Store the original color with 0 alpha
            Color resetColor = m_originalColor;
            resetColor.a = 0f;
            
            // Reset the material color
            m_targetMaterial.SetColor(m_activeColorProperty, resetColor);
            Debug.Log("[MaterialFadeZone] Material alpha reset to 0 on application quit");
        }
    }

#if UNITY_EDITOR
    private void OnDestroy()
    {
        // Only execute in editor and when exiting play mode
        if (!Application.isPlaying) return;
        
        if (m_targetMaterial != null)
        {
            // Store the original color with 0 alpha
            Color resetColor = m_originalColor;
            resetColor.a = 0f;
            
            // Reset the material color
            m_targetMaterial.SetColor(m_activeColorProperty, resetColor);
            Debug.Log("[MaterialFadeZone] Material alpha reset to 0 on destroy (play mode exit)");
        }
    }
#endif
} 