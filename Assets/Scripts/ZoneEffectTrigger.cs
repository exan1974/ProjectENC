using UnityEngine;
using System.Collections;

public class ZoneEffectTrigger : MonoBehaviour
{
    [Header("Zone Material")]
    [SerializeField] private Material m_zoneMaterial;
    [SerializeField] private string m_effectProperty = "Effect";
    [SerializeField] private string m_blackHoleProperty = "BlackHole";
    [SerializeField] private float m_effectDuration = 2f;

    [Header("Diffuse Option")]
    [SerializeField] private bool useDiffuse = false;
    private readonly string m_diffuseProperty = "_DiffuseTransition";

    private Coroutine m_effectCoroutine;
    private Transform m_playerChest;
    private bool m_playerInside = false;

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<ElementalMaterialTransition>();
        if (player != null)
        {
            // Try to get the chest transform from the player, fallback to root if not found
            var chestField = player.GetType().GetField("mixamorig:Spine1", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            m_playerChest = chestField != null ? (Transform)chestField.GetValue(player) : player.transform;
            m_playerInside = true;
            if (m_effectCoroutine != null) StopCoroutine(m_effectCoroutine);
            m_effectCoroutine = StartCoroutine(AnimateEffect());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var player = other.GetComponent<ElementalMaterialTransition>();
        if (player != null)
        {
            m_playerInside = false;
            if (m_effectCoroutine != null) StopCoroutine(m_effectCoroutine);
            // Do not reset Effect here; leave it at its current value
        }
    }

    private void Update()
    {
        if (m_playerInside && m_playerChest != null && m_zoneMaterial != null)
        {
            Vector3 blackHolePos = m_playerChest.position + Vector3.up;
            m_zoneMaterial.SetVector(m_blackHoleProperty, blackHolePos);
        }
    }

    private IEnumerator AnimateEffect()
    {
        float elapsed = 0f;
        while (elapsed < m_effectDuration)
        {
            float t = elapsed / m_effectDuration;
            m_zoneMaterial.SetFloat(m_effectProperty, Mathf.Lerp(0f, 1f, t));
            if (useDiffuse)
            {
                m_zoneMaterial.SetFloat(m_diffuseProperty, Mathf.Lerp(0f, 1f, t));
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        m_zoneMaterial.SetFloat(m_effectProperty, 1f);
        if (useDiffuse)
        {
            m_zoneMaterial.SetFloat(m_diffuseProperty, 1f);
        }
        // Do not deactivate the GameObject
    }

    private void OnDisable()
    {
        if (m_zoneMaterial != null)
        {
            m_zoneMaterial.SetFloat(m_effectProperty, 0f);
            if (useDiffuse)
            {
                m_zoneMaterial.SetFloat(m_diffuseProperty, 0f);
            }
        }
    }
} 