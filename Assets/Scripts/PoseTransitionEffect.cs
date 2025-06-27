using UnityEngine;
using System.Linq;

public class PoseTransitionEffect : MonoBehaviour
{
    [Header("Character References")]
    [SerializeField] private Transform m_leftHand;
    [SerializeField] private Transform m_rightHand;
    [SerializeField] private Transform m_head;  // Added head reference

    [Header("Material References")]
    [SerializeField] private SkinnedMeshRenderer m_bodyRenderer;
    [SerializeField] private SkinnedMeshRenderer m_hairRenderer;
    [SerializeField] private Material m_defaultBodyMaterial;
    [SerializeField] private Material m_defaultHairMaterial;
    [SerializeField] private Material m_specialMaterial;
    [SerializeField] private string m_blendPropertyName = "_BlendAmount"; // Add property for shader blend parameter

    [Header("Detection Settings")]
    [Tooltip("How far the hands should be from the body center (in meters)")]
    [SerializeField] private float m_armStretchThreshold = 0.5f;
    [Tooltip("Maximum height difference allowed between hands (in meters)")]
    [SerializeField] private float m_heightDifferenceThreshold = 0.2f;
    [Tooltip("Distance below head for chest height (in meters)")]
    [SerializeField] private float m_chestHeightOffset = 0.3f;
    [Tooltip("Allowed variation from chest height (in meters)")]
    [SerializeField] private float m_chestHeightTolerance = 0.1f;
    [Tooltip("How long hands must be in position to trigger (in seconds)")]
    [SerializeField] private float m_detectionTime = 0.5f;
    [SerializeField] private bool m_useKeyPressInsteadOfPose = false;  // Toggle between keypress and pose detection
    [SerializeField] private KeyCode m_triggerKey = KeyCode.Return;    // Configurable key for triggering

    [Header("Transition Settings")]
    [SerializeField] private float m_transitionDuration = 1f;
    [SerializeField] private AnimationCurve m_transitionCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField] private float m_fadeInSpeed = 1f;  // Speed when transitioning to special material
    [SerializeField] private float m_fadeOutSpeed = 1f; // Speed when transitioning back to default material

    // Internal state
    private float m_currentTransitionProgress = 0f;
    private float m_timeInPosition = 0f;
    private bool m_isInPose = false;
    private Material m_transitionBodyMaterial;
    private Material m_transitionHairMaterial;
    private bool m_hasTriggeredTransition = false; // Add flag to track if transition has been triggered

    private void Start()
    {
        ValidateSetup();
        InitializeTransitionMaterials();
    }

    private void ValidateSetup()
    {
        if (m_leftHand == null) Debug.LogError("Left hand reference missing!");
        if (m_rightHand == null) Debug.LogError("Right hand reference missing!");
        if (m_head == null) Debug.LogError("Head reference missing!");
        if (m_bodyRenderer == null) Debug.LogError("Body renderer missing!");
        if (m_hairRenderer == null) Debug.LogError("Hair renderer missing!");
        if (m_defaultBodyMaterial == null) Debug.LogError("Default body material missing!");
        if (m_defaultHairMaterial == null) Debug.LogError("Default hair material missing!");
        if (m_specialMaterial == null) Debug.LogError("Special material missing!");
    }

    private void InitializeTransitionMaterials()
    {
        // Create instance materials while preserving their original shaders
        m_transitionBodyMaterial = new Material(m_defaultBodyMaterial);
        m_transitionHairMaterial = new Material(m_defaultHairMaterial);

        // Set initial blend amount if the property exists
        if (m_transitionBodyMaterial.HasProperty(m_blendPropertyName))
        {
            m_transitionBodyMaterial.SetFloat(m_blendPropertyName, 0f);
        }
        if (m_transitionHairMaterial.HasProperty(m_blendPropertyName))
        {
            m_transitionHairMaterial.SetFloat(m_blendPropertyName, 0f);
        }

        // Apply instance materials to renderers
        m_bodyRenderer.material = m_transitionBodyMaterial;
        m_hairRenderer.material = m_transitionHairMaterial;
    }

    private void Update()
    {
        bool shouldTriggerTransition;
        
        if (m_useKeyPressInsteadOfPose)
        {
            if (Input.GetKeyDown(m_triggerKey) && !m_hasTriggeredTransition)
            {
                Debug.Log("[PoseTransitionEffect] Key press detected!");
                m_isInPose = true;
                m_hasTriggeredTransition = true;
                Debug.Log("[PoseTransitionEffect] Starting transition...");
            }
        }
        else
        {
            shouldTriggerTransition = CheckPosePosition();
            if (shouldTriggerTransition)
            {
                Debug.Log($"[PoseTransitionEffect] Pose detected! Height difference: {GetHeightDifference()}");
                
                m_timeInPosition += Time.deltaTime;
                if (m_timeInPosition >= m_detectionTime && !m_isInPose && !m_hasTriggeredTransition)
                {
                    m_isInPose = true;
                    m_hasTriggeredTransition = true;
                    Debug.Log("[PoseTransitionEffect] Starting transition...");
                }
            }
            else
            {
                m_timeInPosition = 0f;
            }
        }

        UpdateMaterialTransition();
    }

    private bool CheckPosePosition()
    {
        if (m_leftHand == null || m_rightHand == null || m_head == null)
        {
            Debug.LogError("[PoseTransitionEffect] Missing required transform references!");
            return false;
        }

        // Calculate chest height (slightly below head)
        float chestHeight = m_head.position.y - m_chestHeightOffset;
        Vector3 bodyCenter = m_head.position;
        bodyCenter.y = chestHeight;

        // Check if hands are at chest height
        float leftHandHeightDiff = Mathf.Abs(m_leftHand.position.y - chestHeight);
        float rightHandHeightDiff = Mathf.Abs(m_rightHand.position.y - chestHeight);
        bool handsAtChestHeight = leftHandHeightDiff < m_chestHeightTolerance && 
                                rightHandHeightDiff < m_chestHeightTolerance;

        // Check if hands are at similar heights
        float heightDifference = Mathf.Abs(m_leftHand.position.y - m_rightHand.position.y);
        bool handsLevel = heightDifference < m_heightDifferenceThreshold;

        // Check if hands are stretched out to the sides
        float leftDistance = Vector3.Distance(new Vector3(m_leftHand.position.x, bodyCenter.y, m_leftHand.position.z), 
                                           new Vector3(bodyCenter.x, bodyCenter.y, bodyCenter.z));
        float rightDistance = Vector3.Distance(new Vector3(m_rightHand.position.x, bodyCenter.y, m_rightHand.position.z), 
                                            new Vector3(bodyCenter.x, bodyCenter.y, bodyCenter.z));

        bool armsStretched = leftDistance > m_armStretchThreshold && rightDistance > m_armStretchThreshold;

        // Check if hands are roughly opposite sides of the body
        Vector3 leftToRight = m_rightHand.position - m_leftHand.position;
        Vector3 horizontalLeftToRight = new Vector3(leftToRight.x, 0, leftToRight.z).normalized;
        float horizontalAlignment = Vector3.Dot(horizontalLeftToRight, Vector3.right);
        bool handsOpposite = Mathf.Abs(horizontalAlignment) > 0.7f; // Allow some flexibility in alignment

        bool inPose = handsLevel && armsStretched && handsOpposite && handsAtChestHeight;

        // Debug visualization
        if (inPose)
        {
            Debug.Log($"[PoseTransitionEffect] Cross pose detected! Height difference: {heightDifference:F2}, " +
                     $"Left distance: {leftDistance:F2}, Right distance: {rightDistance:F2}, " +
                     $"Left height from chest: {leftHandHeightDiff:F2}, Right height from chest: {rightHandHeightDiff:F2}");
        }

        return inPose;
    }

    private float GetHeightDifference()
    {
        if (m_leftHand == null || m_rightHand == null)
        {
            Debug.LogError("[PoseTransitionEffect] Missing required transform references!");
            return 0f;
        }

        float heightDifference = Mathf.Abs(m_leftHand.position.y - m_rightHand.position.y);
        return heightDifference;
    }

    private void UpdateMaterialTransition()
    {
        float target = m_isInPose ? 1f : 0f;
        
        // Update progress
        if (!Mathf.Approximately(m_currentTransitionProgress, target))
        {
            float deltaTime = Time.deltaTime;
            float speed = m_isInPose ? m_fadeInSpeed : m_fadeOutSpeed;
            m_currentTransitionProgress = Mathf.MoveTowards(m_currentTransitionProgress, target, deltaTime * speed);

            // Apply transition curve
            float curvedProgress = m_transitionCurve.Evaluate(m_currentTransitionProgress);

            // Update materials
            LerpMaterials(m_transitionBodyMaterial, curvedProgress);
            LerpMaterials(m_transitionHairMaterial, curvedProgress);
        }
    }

    private void LerpMaterials(Material targetMaterial, float t)
    {
        if (targetMaterial == null)
        {
            Debug.LogError("[PoseTransitionEffect] Target material is null");
            return;
        }

        // Only update the blend amount if the property exists
        if (targetMaterial.HasProperty(m_blendPropertyName))
        {
            targetMaterial.SetFloat(m_blendPropertyName, t);
        }
    }

    private void OnDisable()
    {
        // Reset state when disabled
        m_isInPose = false;
        m_hasTriggeredTransition = false;
        m_timeInPosition = 0f;
        m_currentTransitionProgress = 0f;

        // Reset materials if they exist
        if (m_transitionBodyMaterial != null && m_transitionBodyMaterial.HasProperty(m_blendPropertyName))
        {
            m_transitionBodyMaterial.SetFloat(m_blendPropertyName, 0f);
        }
        if (m_transitionHairMaterial != null && m_transitionHairMaterial.HasProperty(m_blendPropertyName))
        {
            m_transitionHairMaterial.SetFloat(m_blendPropertyName, 0f);
        }
    }

    public void ResetTransition()
    {
        Debug.Log("[PoseTransitionEffect] Resetting transition");
        m_isInPose = false;
        m_hasTriggeredTransition = false;
        m_timeInPosition = 0f;
        m_currentTransitionProgress = 0f;

        if (m_transitionBodyMaterial != null && m_transitionBodyMaterial.HasProperty(m_blendPropertyName))
        {
            m_transitionBodyMaterial.SetFloat(m_blendPropertyName, 0f);
        }
        if (m_transitionHairMaterial != null && m_transitionHairMaterial.HasProperty(m_blendPropertyName))
        {
            m_transitionHairMaterial.SetFloat(m_blendPropertyName, 0f);
        }
    }

    private void OnDrawGizmos()
    {
        if (m_leftHand != null && m_rightHand != null && m_head != null)
        {
            // Calculate chest height
            float chestHeight = m_head.position.y - m_chestHeightOffset;
            Vector3 bodyCenter = m_head.position;
            bodyCenter.y = chestHeight;

            // Draw arm stretch threshold sphere at chest height
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(bodyCenter, m_armStretchThreshold);

            // Draw lines from center to hands
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(bodyCenter, m_leftHand.position);
            Gizmos.DrawLine(bodyCenter, m_rightHand.position);

            // Draw chest height plane
            Gizmos.color = Color.green;
            float size = m_armStretchThreshold * 2f;
            Vector3 chestCenter = new Vector3(bodyCenter.x, chestHeight, bodyCenter.z);
            Gizmos.DrawLine(chestCenter + Vector3.left * size, chestCenter + Vector3.right * size);
            Gizmos.DrawLine(chestCenter + Vector3.forward * size, chestCenter + Vector3.back * size);
            
            // Draw chest height tolerance bounds
            Gizmos.color = Color.cyan;
            Vector3 upperBound = chestCenter + Vector3.up * m_chestHeightTolerance;
            Vector3 lowerBound = chestCenter - Vector3.up * m_chestHeightTolerance;
            
            // Upper bound
            Gizmos.DrawLine(upperBound + Vector3.left * size, upperBound + Vector3.right * size);
            Gizmos.DrawLine(upperBound + Vector3.forward * size, upperBound + Vector3.back * size);
            
            // Lower bound
            Gizmos.DrawLine(lowerBound + Vector3.left * size, lowerBound + Vector3.right * size);
            Gizmos.DrawLine(lowerBound + Vector3.forward * size, lowerBound + Vector3.back * size);

            // Draw head reference
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(m_head.position, 0.1f);
        }
    }
} 