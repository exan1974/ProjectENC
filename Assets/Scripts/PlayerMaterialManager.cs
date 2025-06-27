using UnityEngine;

public class PlayerMaterialManager : MonoBehaviour
{
    [Header("Renderer References")]
    [SerializeField] private SkinnedMeshRenderer m_bodyRenderer;
    [SerializeField] private SkinnedMeshRenderer m_hairRenderer;

    [Header("Default Materials")]
    [SerializeField] private Material m_defaultBodyMaterial;
    [SerializeField] private Material m_defaultHairMaterial;

    [Header("Elemental Materials")]
    [SerializeField] private Material m_pierreMaterial;
    [SerializeField] private Material m_organicMaterial;
    [SerializeField] private Material m_vegetationMaterial;
    [SerializeField] private Material m_terreMaterial;

    [Header("Layer Masks")]
    [SerializeField] private LayerMask m_pierreLayer;
    [SerializeField] private LayerMask m_organicLayer;
    [SerializeField] private LayerMask m_vegetationLayer;
    [SerializeField] private LayerMask m_terreLayer;

    private Material m_currentBodyMaterial;
    private Material m_currentHairMaterial;
    private int m_activeElementalZones = 0;
    private Rigidbody m_rigidbody;
    private Collider m_collider;

    private void Awake()
    {
        Debug.Log($"[PlayerMaterialManager] Initializing...");
        SetupPhysicsComponents();
        ValidateSetup();
    }

    private void SetupPhysicsComponents()
    {
        // Get or add Rigidbody
        m_rigidbody = GetComponent<Rigidbody>();
        if (m_rigidbody == null)
        {
            Debug.Log("[PlayerMaterialManager] Adding required Rigidbody component");
            m_rigidbody = gameObject.AddComponent<Rigidbody>();
            m_rigidbody.useGravity = false; // Disable gravity since this is for trigger detection only
            m_rigidbody.isKinematic = true; // Make it kinematic so it doesn't affect physics
            m_rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        // Get collider and ensure it's not a trigger
        m_collider = GetComponent<Collider>();
        if (m_collider != null && m_collider.isTrigger)
        {
            Debug.Log("[PlayerMaterialManager] Setting collider to non-trigger for proper detection");
            m_collider.isTrigger = false;
        }
    }

    private void ValidateSetup()
    {
        if (m_bodyRenderer == null) Debug.LogError("[PlayerMaterialManager] Body Renderer is not assigned!");
        if (m_hairRenderer == null) Debug.LogError("[PlayerMaterialManager] Hair Renderer is not assigned!");
        if (m_defaultBodyMaterial == null) Debug.LogError("[PlayerMaterialManager] Default Body Material is not assigned!");
        if (m_defaultHairMaterial == null) Debug.LogError("[PlayerMaterialManager] Default Hair Material is not assigned!");
        if (m_pierreMaterial == null) Debug.LogError("[PlayerMaterialManager] Pierre Material is not assigned!");
        if (m_organicMaterial == null) Debug.LogError("[PlayerMaterialManager] Organic Material is not assigned!");
        if (m_vegetationMaterial == null) Debug.LogError("[PlayerMaterialManager] Vegetation Material is not assigned!");
        if (m_terreMaterial == null) Debug.LogError("[PlayerMaterialManager] Terre Material is not assigned!");
        
        Debug.Log($"[PlayerMaterialManager] Layer Masks - Pierre: {m_pierreLayer.value}, Organic: {m_organicLayer.value}, " +
                 $"Vegetation: {m_vegetationLayer.value}, Terre: {m_terreLayer.value}");

        // Log final physics setup
        Debug.Log($"[PlayerMaterialManager] Final Physics Setup - Rigidbody: {m_rigidbody != null}, Collider: {m_collider != null}, " +
                 $"IsKinematic: {m_rigidbody?.isKinematic}, UseGravity: {m_rigidbody?.useGravity}, " +
                 $"IsTrigger: {m_collider?.isTrigger}");
    }

    private void Start()
    {
        Debug.Log("[PlayerMaterialManager] Setting initial materials...");
        // Initialize with default materials
        m_currentBodyMaterial = m_defaultBodyMaterial;
        m_currentHairMaterial = m_defaultHairMaterial;
        UpdateMaterials(m_defaultBodyMaterial, m_defaultHairMaterial);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[PlayerMaterialManager] Trigger Enter detected with object: {other.gameObject.name} on layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        m_activeElementalZones++;
        
        // Check which elemental zone we're entering and swap materials accordingly
        if (IsInLayerMask(other.gameObject.layer, m_pierreLayer))
        {
            Debug.Log("[PlayerMaterialManager] Entering Pierre zone");
            UpdateMaterials(m_pierreMaterial, m_pierreMaterial);
        }
        else if (IsInLayerMask(other.gameObject.layer, m_organicLayer))
        {
            Debug.Log("[PlayerMaterialManager] Entering Organic zone");
            UpdateMaterials(m_organicMaterial, m_organicMaterial);
        }
        else if (IsInLayerMask(other.gameObject.layer, m_vegetationLayer))
        {
            Debug.Log("[PlayerMaterialManager] Entering Vegetation zone");
            UpdateMaterials(m_vegetationMaterial, m_vegetationMaterial);
        }
        else if (IsInLayerMask(other.gameObject.layer, m_terreLayer))
        {
            Debug.Log("[PlayerMaterialManager] Entering Terre zone");
            UpdateMaterials(m_terreMaterial, m_terreMaterial);
        }
        else
        {
            Debug.Log("[PlayerMaterialManager] Entering non-elemental zone");
            m_activeElementalZones--; // Don't count non-elemental triggers
        }

        Debug.Log($"[PlayerMaterialManager] Active elemental zones: {m_activeElementalZones}");
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"[PlayerMaterialManager] Trigger Exit detected with object: {other.gameObject.name} on layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        
        // Only process if it's one of our elemental layers
        if (IsInLayerMask(other.gameObject.layer, m_pierreLayer) ||
            IsInLayerMask(other.gameObject.layer, m_organicLayer) ||
            IsInLayerMask(other.gameObject.layer, m_vegetationLayer) ||
            IsInLayerMask(other.gameObject.layer, m_terreLayer))
        {
            m_activeElementalZones--;
            Debug.Log($"[PlayerMaterialManager] Exiting elemental zone. Remaining zones: {m_activeElementalZones}");

            // Only revert to default if we're not in any other elemental zone
            if (m_activeElementalZones <= 0)
            {
                m_activeElementalZones = 0; // Ensure we don't go negative
                Debug.Log("[PlayerMaterialManager] No active zones remaining, reverting to default materials");
                UpdateMaterials(m_defaultBodyMaterial, m_defaultHairMaterial);
            }
        }
    }

    private void UpdateMaterials(Material bodyMaterial, Material hairMaterial)
    {
        Debug.Log($"[PlayerMaterialManager] Updating materials - Body: {bodyMaterial?.name}, Hair: {hairMaterial?.name}");
        
        if (m_bodyRenderer != null)
        {
            m_bodyRenderer.material = bodyMaterial;
            m_currentBodyMaterial = bodyMaterial;
            Debug.Log("[PlayerMaterialManager] Body material updated successfully");
        }
        else
        {
            Debug.LogError("[PlayerMaterialManager] Failed to update body material - Renderer is null");
        }

        if (m_hairRenderer != null)
        {
            m_hairRenderer.material = hairMaterial;
            m_currentHairMaterial = hairMaterial;
            Debug.Log("[PlayerMaterialManager] Hair material updated successfully");
        }
        else
        {
            Debug.LogError("[PlayerMaterialManager] Failed to update hair material - Renderer is null");
        }
    }

    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        bool result = ((1 << layer) & layerMask) != 0;
        Debug.Log($"[PlayerMaterialManager] Layer check - Layer: {layer}, LayerMask: {layerMask.value}, Result: {result}");
        return result;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"[PlayerMaterialManager] Regular collision detected with: {collision.gameObject.name} on layer: {LayerMask.LayerToName(collision.gameObject.layer)}");
    }

    private void OnTriggerStay(Collider other)
    {
        Debug.Log($"[PlayerMaterialManager] Currently inside trigger: {other.gameObject.name} on layer: {LayerMask.LayerToName(other.gameObject.layer)}");
    }

    private void OnEnable()
    {
        Debug.Log($"[PlayerMaterialManager] Component enabled on GameObject: {gameObject.name}");
        Debug.Log($"[PlayerMaterialManager] Has Rigidbody: {GetComponent<Rigidbody>() != null}");
        Debug.Log($"[PlayerMaterialManager] Has Collider: {GetComponent<Collider>() != null}");
        if (GetComponent<Collider>() != null)
        {
            Debug.Log($"[PlayerMaterialManager] Collider isTrigger: {GetComponent<Collider>().isTrigger}");
        }
    }
} 