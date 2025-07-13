using UnityEngine;
using System.Collections;

public class ElementalMaterialTransition : MonoBehaviour
{
    [Header("Renderer References")]
    [SerializeField] private SkinnedMeshRenderer m_bodyRenderer;
    [SerializeField] private SkinnedMeshRenderer m_hairRenderer;

    [Header("Materials (optional, overrides renderer if set)")]
    [SerializeField] private Material m_bodyMaterial;
    [SerializeField] private Material m_hairMaterial;

    [Header("Elemental Textures")]
    [SerializeField] private Texture2D m_organicTex;
    [SerializeField] private Texture2D m_vegetationTex;
    [SerializeField] private Texture2D m_pierreTex;
    [SerializeField] private Texture2D m_terreTex;

    [Header("Layer Masks")]
    [SerializeField] private LayerMask m_organicLayer;
    [SerializeField] private LayerMask m_vegetationLayer;
    [SerializeField] private LayerMask m_pierreLayer;
    [SerializeField] private LayerMask m_terreLayer;

    [Header("Transition Settings")]
    [SerializeField] private float m_transitionDuration = 2f;
    [SerializeField] private string m_secTexProperty = "_SecTex";
    [SerializeField] private string m_diffuseTransitionProperty = "_DiffuseTransition";

    [Header("Original Textures")]
    [SerializeField] private Texture2D m_originalBodyTex;
    [SerializeField] private Texture2D m_originalHairTex;

    private Coroutine m_bodyTransitionCoroutine;
    private Coroutine m_hairTransitionCoroutine;

    private void OnEnable()
    {
        // On enable, set FirstTex to original textures if not already set
        SetFirstTex(m_bodyMaterial != null ? m_bodyMaterial : m_bodyRenderer?.material, m_originalBodyTex);
        SetFirstTex(m_hairMaterial != null ? m_hairMaterial : m_hairRenderer?.material, m_originalHairTex);
    }

    private void OnDisable()
    {
        // On disable (scene change or exiting play), reset FirstTex to original textures
        SetFirstTex(m_bodyMaterial != null ? m_bodyMaterial : m_bodyRenderer?.material, m_originalBodyTex);
        SetFirstTex(m_hairMaterial != null ? m_hairMaterial : m_hairRenderer?.material, m_originalHairTex);
        // Reset DiffuseTransition to 0
        ResetDiffuseTransition(m_bodyMaterial != null ? m_bodyMaterial : m_bodyRenderer?.material);
        ResetDiffuseTransition(m_hairMaterial != null ? m_hairMaterial : m_hairRenderer?.material);
    }

    private void SetFirstTex(Material mat, Texture2D tex)
    {
        if (mat != null && tex != null)
        {
            mat.SetTexture("_FirstTex", tex);
        }
    }

    private void ResetDiffuseTransition(Material mat)
    {
        if (mat != null)
        {
            int propID = Shader.PropertyToID(m_diffuseTransitionProperty);
            mat.SetFloat(propID, 0f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[ElementalMaterialTransition] Trigger Enter: {other.gameObject.name} (Layer: {LayerMask.LayerToName(other.gameObject.layer)})");
        Texture2D selectedTex = null;
        if (IsInLayerMask(other.gameObject.layer, m_organicLayer))
        {
            selectedTex = m_organicTex;
            Debug.Log("[ElementalMaterialTransition] Organic zone detected");
        }
        else if (IsInLayerMask(other.gameObject.layer, m_vegetationLayer))
        {
            selectedTex = m_vegetationTex;
            Debug.Log("[ElementalMaterialTransition] Vegetation zone detected");
        }
        else if (IsInLayerMask(other.gameObject.layer, m_pierreLayer))
        {
            selectedTex = m_pierreTex;
            Debug.Log("[ElementalMaterialTransition] Pierre zone detected");
        }
        else if (IsInLayerMask(other.gameObject.layer, m_terreLayer))
        {
            selectedTex = m_terreTex;
            Debug.Log("[ElementalMaterialTransition] Terre zone detected");
        }
        else
        {
            Debug.Log("[ElementalMaterialTransition] Non-elemental trigger, ignoring");
            return;
        }

        // Apply to body
        Material bodyMat = m_bodyMaterial != null ? m_bodyMaterial : m_bodyRenderer?.material;
        if (bodyMat != null && selectedTex != null)
        {
            bodyMat.SetTexture(m_secTexProperty, selectedTex);
            if (m_bodyTransitionCoroutine != null) StopCoroutine(m_bodyTransitionCoroutine);
            m_bodyTransitionCoroutine = StartCoroutine(DiffuseTransitionCoroutine(bodyMat, selectedTex));
        }
        // Apply to hair
        Material hairMat = m_hairMaterial != null ? m_hairMaterial : m_hairRenderer?.material;
        if (hairMat != null && selectedTex != null)
        {
            hairMat.SetTexture(m_secTexProperty, selectedTex);
            if (m_hairTransitionCoroutine != null) StopCoroutine(m_hairTransitionCoroutine);
            m_hairTransitionCoroutine = StartCoroutine(DiffuseTransitionCoroutine(hairMat, selectedTex));
        }
    }

    private IEnumerator DiffuseTransitionCoroutine(Material mat, Texture2D secTex)
    {
        int propID = Shader.PropertyToID(m_diffuseTransitionProperty);
        mat.SetFloat(propID, 0f);
        float elapsed = 0f;
        while (elapsed < m_transitionDuration)
        {
            float t = elapsed / m_transitionDuration;
            mat.SetFloat(propID, Mathf.Lerp(0f, 1f, t));
            elapsed += Time.deltaTime;
            yield return null;
        }
        mat.SetFloat(propID, 1f);
        // After transition, set both FirstTex and SecTex to the new texture
        mat.SetTexture("_FirstTex", secTex);
        mat.SetTexture(m_secTexProperty, secTex);
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return ((1 << layer) & mask.value) != 0;
    }
} 