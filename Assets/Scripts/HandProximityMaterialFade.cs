using System.Collections;
using UnityEngine;

public class HandProximityMaterialFade : MonoBehaviour
{
    public float m_fadeDuration = 1f;
    public string m_alphaProperty = "_Color";
    public Material m_targetMaterial;

    [Header("Hand References")]
    [SerializeField] private Transform m_aLeftHand;
    [SerializeField] private Transform m_aRightHand;
    [SerializeField] private Transform m_bLeftHand;
    [SerializeField] private Transform m_bRightHand;

    [Header("Proximity Settings")]
    [SerializeField] private float m_nearThreshold = 0.2f;

    private float m_currentAlpha = 1f;
    private float m_initialAlpha = 1f;
    private bool m_handsAreNear = false;
    private Coroutine m_fadeCoroutine;

    private void Start()
    {
        // Initialize and save the initial alpha
        m_initialAlpha = GetCurrentAlpha();
        m_currentAlpha = m_initialAlpha;
        SetAlpha(m_initialAlpha);
    }

    private void Update()
    {
        bool anyNear = AreAnyHandsNear();
        if (anyNear != m_handsAreNear)
        {
            m_handsAreNear = anyNear;
            if (m_fadeCoroutine != null) StopCoroutine(m_fadeCoroutine);
            m_fadeCoroutine = StartCoroutine(FadeAlpha(m_handsAreNear ? 0f : m_initialAlpha));
        }
    }

    private IEnumerator FadeAlpha(float targetAlpha)
    {
        float startAlpha = m_currentAlpha;
        float elapsed = 0f;
        while (elapsed < m_fadeDuration)
        {
            m_currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / m_fadeDuration);
            SetAlpha(m_currentAlpha);
            elapsed += Time.deltaTime;
            yield return null;
        }
        m_currentAlpha = targetAlpha;
        SetAlpha(m_currentAlpha);
    }

    private float GetCurrentAlpha()
    {
        if (m_targetMaterial == null) return 1f;
        if (m_alphaProperty == "_Color" && m_targetMaterial.HasProperty("_Color"))
        {
            return m_targetMaterial.color.a;
        }
        else if (m_targetMaterial.HasProperty(m_alphaProperty))
        {
            return m_targetMaterial.GetFloat(m_alphaProperty);
        }
        return 1f;
    }

    private void OnDisable()
    {
        SetAlpha(m_initialAlpha);
    }

    private bool AreAnyHandsNear()
    {
        if (!m_aLeftHand || !m_aRightHand || !m_bLeftHand || !m_bRightHand) return false;
        float t = m_nearThreshold;
        return
            Vector3.Distance(m_aLeftHand.position, m_bLeftHand.position) < t ||
            Vector3.Distance(m_aLeftHand.position, m_bRightHand.position) < t ||
            Vector3.Distance(m_aRightHand.position, m_bLeftHand.position) < t ||
            Vector3.Distance(m_aRightHand.position, m_bRightHand.position) < t;
    }

    private void SetAlpha(float alpha)
    {
        if (m_targetMaterial == null) return;
        if (m_alphaProperty == "_Color" && m_targetMaterial.HasProperty("_Color"))
        {
            m_targetMaterial.color = new Color(m_targetMaterial.color.r, m_targetMaterial.color.g, m_targetMaterial.color.b, alpha);
        }
        else if (m_targetMaterial.HasProperty(m_alphaProperty))
        {
            m_targetMaterial.SetFloat(m_alphaProperty, alpha);
        }
    }
} 