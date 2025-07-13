using UnityEngine;

public class MaterialTransitionManager : MonoBehaviour
{
    [System.Serializable]
    private class MaterialProperties
    {
        public float diffuseTransition = 1f;
        public float surfaceMovementSpeed = 0.03f;
        public float noiseScale = 0.02f;
        public float scrollingSpeed = 0.08f;
    }

    [Header("Materials")]
    [SerializeField] private Material m_bodyMaterial;
    [SerializeField] private Material m_hairMaterial;

    [Header("Transition Settings")]
    [SerializeField] private float m_transitionDuration = 2f;
    [SerializeField] private AnimationCurve m_transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool usePoseTrigger = false;
    [SerializeField] private KeyCode triggerKey = KeyCode.Return;
    [Header("Pose Detection")]
    [Tooltip("Transform of the right hand (for up direction)")]
    [SerializeField] private Transform rightHand;
    [Tooltip("Transform of the left hand (for left direction)")]
    [SerializeField] private Transform leftHand;
    [Tooltip("Transform of the right shoulder (for right arm direction)")]
    [SerializeField] private Transform rightShoulder;
    [Tooltip("Transform of the left shoulder (for left arm direction)")]
    [SerializeField] private Transform leftShoulder;
    [Tooltip("Angle threshold in degrees for pose detection")]
    [SerializeField, Range(0, 30)] private float poseAngleThreshold = 15f;
    [SerializeField] private PoseType poseType = PoseType.NineOClock;

    // Property names in the shader
    private readonly string DIFFUSE_PROP = "_DiffuseTransition";
    private readonly string MOVEMENT_SPEED_PROP = "_SurfaceMovementSpeed";
    private readonly string NOISE_SCALE_PROP = "_NoiseScale";
    private readonly string SCROLLING_SPEED_PROP = "_ScrollingSpeed";

    // Stored initial values
    private MaterialProperties m_initialValues = new MaterialProperties();
    
    // Transition state
    private float m_transitionTime = 0f;
    private bool m_isTransitioning = false;
    private bool m_hasStoredValues = false;

    private void Start()
    {
        if (!ValidateSetup()) return;

        // Set initial values to 0
        SetMaterialValues(m_bodyMaterial, 0, 0, 0, 0);
        SetMaterialValues(m_hairMaterial, 0, 0, 0, 0);

        m_hasStoredValues = true;
        Debug.Log($"Using values - Diffuse: {m_initialValues.diffuseTransition}, " +
                 $"Movement: {m_initialValues.surfaceMovementSpeed}, " +
                 $"Noise: {m_initialValues.noiseScale}, " +
                 $"Scrolling: {m_initialValues.scrollingSpeed}");
    }

    private void Update()
    {
        if (!m_hasStoredValues) return;

        if (!usePoseTrigger)
        {
            // Check for trigger key press
            if (Input.GetKeyDown(triggerKey) && !m_isTransitioning)
            {
                StartTransition();
            }
        }
        else
        {
            // Pose detection
            if (!m_isTransitioning && IsPoseDetected())
            {
                StartTransition();
            }
        }

        // Update transition
        if (m_isTransitioning)
        {
            UpdateTransition();
        }
    }

    private void StartTransition()
    {
        m_isTransitioning = true;
        m_transitionTime = 0f;
        Debug.Log("Starting transition...");
    }

    private void UpdateTransition()
    {
        m_transitionTime += Time.deltaTime;
        float normalizedTime = m_transitionTime / m_transitionDuration;
        
        if (normalizedTime >= 1f)
        {
            // Finish transition
            SetMaterialValues(m_bodyMaterial, 
                m_initialValues.diffuseTransition,
                m_initialValues.surfaceMovementSpeed,
                m_initialValues.noiseScale,
                m_initialValues.scrollingSpeed);
            
            SetMaterialValues(m_hairMaterial,
                m_initialValues.diffuseTransition,
                m_initialValues.surfaceMovementSpeed,
                m_initialValues.noiseScale,
                m_initialValues.scrollingSpeed);

            m_isTransitioning = false;
            Debug.Log("Transition complete!");
            return;
        }

        // Calculate lerped values using transition curve
        float t = m_transitionCurve.Evaluate(normalizedTime);
        float currentDiffuse = Mathf.Lerp(0, m_initialValues.diffuseTransition, t);
        float currentMovement = Mathf.Lerp(0, m_initialValues.surfaceMovementSpeed, t);
        float currentNoise = Mathf.Lerp(0, m_initialValues.noiseScale, t);
        float currentScrolling = Mathf.Lerp(0, m_initialValues.scrollingSpeed, t);

        // Apply to both materials
        SetMaterialValues(m_bodyMaterial, currentDiffuse, currentMovement, currentNoise, currentScrolling);
        SetMaterialValues(m_hairMaterial, currentDiffuse, currentMovement, currentNoise, currentScrolling);
    }

    private void SetMaterialValues(Material material, float diffuse, float movement, float noise, float scrolling)
    {
        material.SetFloat(DIFFUSE_PROP, diffuse);
        material.SetFloat(MOVEMENT_SPEED_PROP, movement);
        material.SetFloat(NOISE_SCALE_PROP, noise);
        material.SetFloat(SCROLLING_SPEED_PROP, scrolling);
    }

    private bool ValidateSetup()
    {
        if (m_bodyMaterial == null)
        {
            Debug.LogError("Body material is not assigned!");
            return false;
        }
        if (m_hairMaterial == null)
        {
            Debug.LogError("Hair material is not assigned!");
            return false;
        }
        return true;
    }

    // Public method to reset the transition
    public void ResetTransition()
    {
        m_isTransitioning = false;
        m_transitionTime = 0f;
        SetMaterialValues(m_bodyMaterial, 0, 0, 0, 0);
        SetMaterialValues(m_hairMaterial, 0, 0, 0, 0);
        Debug.Log("Transition reset!");
    }

    private bool IsPoseDetected()
    {
        if (rightHand == null || leftHand == null || rightShoulder == null || leftShoulder == null) return false;
        Vector3 rightArmDir = (rightHand.position - rightShoulder.position).normalized;
        Vector3 leftArmDir = (leftHand.position - leftShoulder.position).normalized;
        if (poseType == PoseType.NineOClock)
        {
            float rightArmAngle = Vector3.Angle(rightArmDir, Vector3.up);
            float leftArmAngle = Vector3.Angle(leftArmDir, Vector3.left);
            return rightArmAngle <= poseAngleThreshold && leftArmAngle <= poseAngleThreshold;
        }
        else if (poseType == PoseType.NineFifteen)
        {
            float rightArmAngle = Vector3.Angle(rightArmDir, Vector3.right);
            float leftArmAngle = Vector3.Angle(leftArmDir, Vector3.left);
            return rightArmAngle <= poseAngleThreshold && leftArmAngle <= poseAngleThreshold;
        }
        return false;
    }

    public enum PoseType { NineOClock, NineFifteen }
} 