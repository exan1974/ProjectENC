using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ConstellationTriggerMode
{
    Key,
    Timer,
    Pose
}

public class ConstellationEffect : MonoBehaviour
{
    [Header("Prefabs & Skeleton")]
    public GameObject posePrefab;            // your frozen‐pose rig prefab
    public Transform characterRoot;          // live skeleton root
    public Transform characterCentralBone;   // e.g. Hips, for re‑anchoring

    [Header("Placement")]
    public Transform centralPosition;        // where new poses spawn
    public Transform[] storedPositions;      // where old poses end up
    public float scaleReduction = 0.5f;      // how much older poses shrink

    [Header("Fade & Display Timing")]
    [Tooltip("Seconds to fade in/out body and stars")]
    public float fadeDuration = 0.5f;
    [Tooltip("Seconds to stay fully visible at center")]
    public float displayDuration = 2f;

    [Header("Trigger Settings")]
    [Tooltip("How the constellation effect is triggered")]
    public ConstellationTriggerMode triggerMode = ConstellationTriggerMode.Key;
    [Tooltip("Key to trigger the constellation effect")]
    public KeyCode triggerKey = KeyCode.Space;
    [Tooltip("Time interval between automatic captures (in seconds)")]
    public float captureInterval = 3f;
    [Tooltip("How long to stay in the same pose to trigger (in seconds)")]
    public float poseHoldDuration = 2f;

    [Header("Pose Detection")]
    [Tooltip("Transform of the right hand (for pose detection)")]
    public Transform rightHand;
    [Tooltip("Transform of the left hand (for pose detection)")]
    public Transform leftHand;
    [Tooltip("Transform of the right shoulder (for pose detection)")]
    public Transform rightShoulder;
    [Tooltip("Transform of the left shoulder (for pose detection)")]
    public Transform leftShoulder;
    [Tooltip("Transform of the right foot (for pose detection)")]
    public Transform rightFoot;
    [Tooltip("Transform of the left foot (for pose detection)")]
    public Transform leftFoot;
    [Tooltip("How close positions must be to be considered the same pose (in meters)")]
    public float poseThreshold = 0.1f;

    private float m_timeUntilNextCapture;
    private float m_poseHoldTime = 0f;
    private Vector3 m_lastRightHandPos;
    private Vector3 m_lastLeftHandPos;
    private Vector3 m_lastRightShoulderPos;
    private Vector3 m_lastLeftShoulderPos;
    private Vector3 m_lastRightFootPos;
    private Vector3 m_lastLeftFootPos;
    private bool m_isInPose = false;

    int currentStoreIndex = 0;

    void Start()
    {
        m_timeUntilNextCapture = captureInterval;
        
        // Initialize pose detection
        if (triggerMode == ConstellationTriggerMode.Pose)
        {
            InitializePoseDetection();
        }
    }

    void Update()
    {
        switch (triggerMode)
        {
            case ConstellationTriggerMode.Key:
                if (Input.GetKeyDown(triggerKey))
                {
                    CapturePose();
                }
                break;

            case ConstellationTriggerMode.Timer:
                m_timeUntilNextCapture -= Time.deltaTime;
                if (m_timeUntilNextCapture <= 0)
                {
                    CapturePose();
                    m_timeUntilNextCapture = captureInterval;
                }
                break;

            case ConstellationTriggerMode.Pose:
                UpdatePoseDetection();
                break;
        }
    }

    private void InitializePoseDetection()
    {
        if (rightHand != null) m_lastRightHandPos = rightHand.position;
        if (leftHand != null) m_lastLeftHandPos = leftHand.position;
        if (rightShoulder != null) m_lastRightShoulderPos = rightShoulder.position;
        if (leftShoulder != null) m_lastLeftShoulderPos = leftShoulder.position;
        if (rightFoot != null) m_lastRightFootPos = rightFoot.position;
        if (leftFoot != null) m_lastLeftFootPos = leftFoot.position;
    }

    private void UpdatePoseDetection()
    {
        if (rightHand == null || leftHand == null || rightShoulder == null || leftShoulder == null ||
            rightFoot == null || leftFoot == null)
        {
            Debug.LogWarning("Pose detection transforms not assigned! All hands, shoulders, and feet are required.");
            return;
        }

        // Check if current pose is similar to last pose
        bool poseChanged = false;
        
        if (Vector3.Distance(rightHand.position, m_lastRightHandPos) > poseThreshold ||
            Vector3.Distance(leftHand.position, m_lastLeftHandPos) > poseThreshold ||
            Vector3.Distance(rightShoulder.position, m_lastRightShoulderPos) > poseThreshold ||
            Vector3.Distance(leftShoulder.position, m_lastLeftShoulderPos) > poseThreshold ||
            Vector3.Distance(rightFoot.position, m_lastRightFootPos) > poseThreshold ||
            Vector3.Distance(leftFoot.position, m_lastLeftFootPos) > poseThreshold)
        {
            poseChanged = true;
        }

        if (poseChanged)
        {
            // Pose changed, reset timer
            m_poseHoldTime = 0f;
            m_isInPose = false;
            
            // Update last positions
            m_lastRightHandPos = rightHand.position;
            m_lastLeftHandPos = leftHand.position;
            m_lastRightShoulderPos = rightShoulder.position;
            m_lastLeftShoulderPos = leftShoulder.position;
            m_lastRightFootPos = rightFoot.position;
            m_lastLeftFootPos = leftFoot.position;
        }
        else
        {
            // Pose maintained, increment timer
            m_poseHoldTime += Time.deltaTime;
            
            if (!m_isInPose && m_poseHoldTime >= poseHoldDuration)
            {
                // Pose held long enough, trigger constellation
                m_isInPose = true;
                CapturePose();
                m_poseHoldTime = 0f; // Reset for next trigger
            }
        }
    }

    void CapturePose()
    {
        if (currentStoreIndex >= storedPositions.Length)
        {
            Debug.LogWarning("No more stored positions!");
            return;
        }

        // 1) Instantiate & freeze the pose
        GameObject capture = Instantiate(
            posePrefab,
            centralPosition.position,
            centralPosition.rotation
        );
        if (capture.TryGetComponent<Animator>(out var anim))
            anim.enabled = false;
        CopyPoseRecursive(characterRoot, capture.transform);

        // 2) Re‑anchor around the central bone
        if (characterCentralBone != null)
        {
            Transform newBone = capture.transform.Find(characterCentralBone.name);
            if (newBone != null)
            {
                Vector3 offset = capture.transform.position - newBone.position;
                capture.transform.position = centralPosition.position + offset;
            }
        }

        // 3) Gather all SkinnedMeshRenderers for body‑alpha fading
        var bodyRenderers = new List<Renderer>();
        foreach (var sk in capture.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            sk.materials = sk.materials; // instantiate materials
            bodyRenderers.Add(sk);
        }

        // 4) Gather all MeshRenderers for star‑scale tweening
        var stars = new List<StarData>();
        foreach (var mr in capture.GetComponentsInChildren<MeshRenderer>(true))
        {
            // skip any skinned ones (just in case)
            if (mr is SkinnedMeshRenderer) 
                continue;

            stars.Add(new StarData {
                transform     = mr.transform,
                originalScale = mr.transform.localScale
            });
        }

        // 5) Start the fade/hold/move coroutine
        StartCoroutine(PoseLifecycle(capture, bodyRenderers, stars, currentStoreIndex));
        currentStoreIndex++;
    }

    IEnumerator PoseLifecycle(
        GameObject capture,
        List<Renderer> bodyRenderers,
        List<StarData> stars,
        int storeIdx
    )
    {
        // INITIAL: body alpha=0, stars scale=0
        SetBodyAlpha(bodyRenderers, 0f);
        SetStarScale(stars,      0f);

        // FADE IN center (0→1)
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / fadeDuration);
            SetBodyAlpha(bodyRenderers, f);
            SetStarScale(stars, f);
            yield return null;
        }
        SetBodyAlpha(bodyRenderers, 1f);
        SetStarScale(stars,      1f);

        // HOLD fully visible
        yield return new WaitForSeconds(displayDuration);

        // FADE OUT center (1→0)
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / fadeDuration);
            SetBodyAlpha(bodyRenderers, 1f - f);
            SetStarScale(stars,      1f - f);
            yield return null;
        }
        SetBodyAlpha(bodyRenderers, 0f);
        SetStarScale(stars,      0f);

        // MOVE & shrink into stored slot
        var stored = storedPositions[storeIdx];
        capture.transform.position = stored.position;
        capture.transform.rotation = stored.rotation;
        capture.transform.localScale *= scaleReduction;

        // FADE IN stored (0→1)
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / fadeDuration);
            SetBodyAlpha(bodyRenderers, f);
            SetStarScale(stars, f);
            yield return null;
        }
        SetBodyAlpha(bodyRenderers, 1f);
        SetStarScale(stars,      1f);
    }

    // fades all body renderers’ material alpha
    void SetBodyAlpha(List<Renderer> renderers, float alpha)
    {
        foreach (var rend in renderers)
        {
            foreach (var mat in rend.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                }
                else if (mat.HasProperty("_BaseColor"))
                {
                    Color c = mat.GetColor("_BaseColor");
                    c.a = alpha;
                    mat.SetColor("_BaseColor", c);
                }
            }
        }
    }

    // scales each star from 0→originalScale by factor
    void SetStarScale(List<StarData> stars, float factor)
    {
        foreach (var sd in stars)
            sd.transform.localScale = sd.originalScale * factor;
    }

    // copy local transforms so the pose is frozen
    void CopyPoseRecursive(Transform src, Transform dst)
    {
        dst.localPosition = src.localPosition;
        dst.localRotation = src.localRotation;
        dst.localScale    = src.localScale;
        for (int i = 0; i < src.childCount && i < dst.childCount; i++)
            CopyPoseRecursive(src.GetChild(i), dst.GetChild(i));
    }

    class StarData
    {
        public Transform transform;
        public Vector3   originalScale;
    }
}
