using System.Collections.Generic;
using UnityEngine;

namespace Neuron
{
    /// <summary>
    /// This script streams a "reflected" version of the mocap-driven character.
    /// It expects that:
    /// 1. The source character is being updated by a NeuronTransformsInstance (or a similar script),
    ///    and that its bones can be accessed via GetTransforms().
    /// 2. The reflection character uses the same hierarchy (bone names must match).
    /// 3. A water plane exists at a specified Y (waterHeight) so that the reflection is calculated relative to it.
    /// 
    /// The reflection is computed by:
    /// - Reflecting each bone's world position across the horizontal plane:
    ///     reflectedPos.y = 2 * waterHeight - sourcePos.y
    /// - Reflecting the bone's rotation by inverting the vertical components of its forward/up vectors.
    /// </summary>
    public class NeuronTransformsReflection : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The source mocap-driven character using NeuronTransformsInstance.")]
        public NeuronTransformsInstance sourceCharacter;

        [Tooltip("The root transform of the reflection character (which must have the same bone hierarchy and names).")]
        public Transform reflectionRoot;

        [Header("Reflection Settings")]
        [Tooltip("The Y coordinate of the water surface. The reflection is calculated relative to this level.")]
        public float waterHeight = 0.0f;

        [Header("Mirror Trigger Settings")]
        [Tooltip("The transform whose Y position is used as the mirror plane (e.g., a hand)")]
        public Transform groundReference;
        [Tooltip("The transform for the source artist's foot (for ground height)")]
        public Transform footReference;
        [Tooltip("Right hand transform for mirror plane calculation")]
        public Transform rightHandReference;
        [Tooltip("Apparatus height in meters")] public float apparatusHeight = 0.8f;
        [Tooltip("Key to trigger the mirror effect")]
        public KeyCode mirrorKey = KeyCode.M;
        [Tooltip("Use key activation (manual) or automatic pose detection")] public bool useKeyActivation = true;
        [Tooltip("The GameObject to activate/deactivate for the mirror effect")]
        public GameObject reflectionObject;
        [Header("Arm Pose Detection")]
        public Transform leftHand;
        public Transform leftElbow;
        public Transform leftShoulder;
        public Transform rightHand;
        public Transform rightElbow;
        public Transform rightShoulder;
        [Tooltip("How close to vertical (in degrees) the arm must be")] public float verticalThreshold = 15f;
        [Tooltip("How still (in meters) the joints must be over the time window")] public float stillnessThreshold = 0.01f;
        [Tooltip("Time window for stillness check (seconds)")] public float stillnessWindow = 0.2f;
        [Tooltip("Tolerance for apparatus height check (meters)")]
        public float heightTolerance = 0.15f;

        // Dictionary to map bone names to transforms in the reflection hierarchy.
        private Dictionary<string, Transform> reflectionBoneMap;

        private bool isMirrored = false;
        private float cachedMirrorY = 0f;
        private float cachedGroundY = 0f;
        private Queue<Vector3> leftHandHistory = new Queue<Vector3>();
        private Queue<Vector3> leftElbowHistory = new Queue<Vector3>();
        private Queue<Vector3> leftShoulderHistory = new Queue<Vector3>();
        private Queue<Vector3> rightHandHistory = new Queue<Vector3>();
        private Queue<Vector3> rightElbowHistory = new Queue<Vector3>();
        private Queue<Vector3> rightShoulderHistory = new Queue<Vector3>();
        private int historySteps;
        private float lastHistoryTime = 0f;

        void Start()
        {
            // Check that we have assigned a source and reflection root.
            if (sourceCharacter == null || reflectionRoot == null)
            {
                Debug.LogError("SourceCharacter or ReflectionRoot is not assigned. Please assign both in the Inspector.");
                return;
            }

            // Build a dictionary of all bones in the reflection character for quick lookup.
            reflectionBoneMap = new Dictionary<string, Transform>();
            foreach (Transform bone in reflectionRoot.GetComponentsInChildren<Transform>())
            {
                if (!reflectionBoneMap.ContainsKey(bone.name))
                    reflectionBoneMap.Add(bone.name, bone);
            }

            if (footReference != null)
                cachedGroundY = footReference.position.y;
            else
                cachedGroundY = 0f;
            historySteps = Mathf.CeilToInt(stillnessWindow / Time.fixedDeltaTime);
        }

        void Update()
        {
            if (useKeyActivation)
            {
                // Check for mirror key press
                if (Input.GetKeyDown(mirrorKey))
                {
                    isMirrored = !isMirrored;
                    if (reflectionObject != null)
                        reflectionObject.SetActive(isMirrored);
                    if (isMirrored)
                    {
                        // Cache the ground height when activating - use lowest Y from both hands
                        float leftHandY = groundReference != null ? groundReference.position.y : float.MaxValue;
                        float rightHandY = rightHandReference != null ? rightHandReference.position.y : float.MaxValue;
                        cachedMirrorY = Mathf.Min(leftHandY, rightHandY);
                        
                        // Fallback to waterHeight if no hands are available
                        if (cachedMirrorY == float.MaxValue)
                            cachedMirrorY = waterHeight;
                    }
                }
            }
            else
            {
                // Automatic pose detection
                bool leftStand = IsArmInStandPose(leftHand, leftElbow, leftShoulder, leftHandHistory, leftElbowHistory, leftShoulderHistory);
                bool rightStand = IsArmInStandPose(rightHand, rightElbow, rightShoulder, rightHandHistory, rightElbowHistory, rightShoulderHistory);
                bool shouldMirror = leftStand || rightStand;
                if (shouldMirror != isMirrored)
                {
                    isMirrored = shouldMirror;
                    if (reflectionObject != null)
                        reflectionObject.SetActive(isMirrored);
                    if (isMirrored)
                    {
                        // Cache the ground height when activating - use lowest Y from both hands
                        float leftHandY = groundReference != null ? groundReference.position.y : float.MaxValue;
                        float rightHandY = rightHandReference != null ? rightHandReference.position.y : float.MaxValue;
                        cachedMirrorY = Mathf.Min(leftHandY, rightHandY);
                        
                        // Fallback to calculated apparatus height if no hands are available
                        if (cachedMirrorY == float.MaxValue)
                            cachedMirrorY = cachedGroundY + apparatusHeight;
                    }
                }
            }
            // Update joint histories for stillness check
            if (Time.time - lastHistoryTime > Time.fixedDeltaTime)
            {
                UpdateHistory(leftHand, leftHandHistory);
                UpdateHistory(leftElbow, leftElbowHistory);
                UpdateHistory(leftShoulder, leftShoulderHistory);
                UpdateHistory(rightHand, rightHandHistory);
                UpdateHistory(rightElbow, rightElbowHistory);
                UpdateHistory(rightShoulder, rightShoulderHistory);
                lastHistoryTime = Time.time;
            }
        }

        private void UpdateHistory(Transform t, Queue<Vector3> history)
        {
            if (t == null) return;
            history.Enqueue(t.position);
            while (history.Count > historySteps)
                history.Dequeue();
        }

        private bool IsArmInStandPose(Transform hand, Transform elbow, Transform shoulder, Queue<Vector3> handHist, Queue<Vector3> elbowHist, Queue<Vector3> shoulderHist)
        {
            if (hand == null || elbow == null || shoulder == null) return false;
            // Check vertical alignment (Y order and XZ closeness)
            Vector3 h = hand.position, e = elbow.position, s = shoulder.position;
            // Hand below elbow below shoulder (for handstand)
            bool yOrder = h.y < e.y && e.y < s.y;
            // XZ closeness
            float xzDist1 = Vector2.Distance(new Vector2(h.x, h.z), new Vector2(e.x, e.z));
            float xzDist2 = Vector2.Distance(new Vector2(e.x, e.z), new Vector2(s.x, s.z));
            bool xzClose = xzDist1 < 0.1f && xzDist2 < 0.1f;
            // Verticality (angle between arm and world down)
            Vector3 upper = (s - e).normalized;
            Vector3 lower = (e - h).normalized;
            float upperAngle = Vector3.Angle(upper, Vector3.down);
            float lowerAngle = Vector3.Angle(lower, Vector3.down);
            bool vertical = upperAngle < verticalThreshold && lowerAngle < verticalThreshold;
            // Height: must be within apparatusHeight ± tolerance
            float apparatusY = cachedGroundY + apparatusHeight;
            bool onApparatus = Mathf.Abs(h.y - apparatusY) < heightTolerance;
            // Stillness
            bool still = IsStill(handHist) && IsStill(elbowHist) && IsStill(shoulderHist);
            bool result = yOrder && xzClose && vertical && onApparatus && still;
            Debug.Log($"[NeuronTransformsReflection] HandY: {h.y:F3}, ApparatusY: {apparatusY:F3}, GroundY: {cachedGroundY:F3}, yOrder: {yOrder}, xzClose: {xzClose}, vertical: {vertical}, onApparatus: {onApparatus}, still: {still}, result: {result}");
            return result;
        }

        private bool IsStill(Queue<Vector3> history)
        {
            if (history.Count < 2) return false;
            Vector3 min = history.Peek(), max = history.Peek();
            foreach (var v in history)
            {
                min = Vector3.Min(min, v);
                max = Vector3.Max(max, v);
            }
            return (max - min).magnitude < stillnessThreshold;
        }

        void LateUpdate()
        {
            if (!isMirrored)
                return;
            // Ensure the source character and its transforms are available.
            if (sourceCharacter == null || reflectionBoneMap == null)
                return;

            // Get the array of source bone transforms.
            Transform[] sourceBones = sourceCharacter.GetTransforms();

            if (sourceBones == null)
                return;

            // Use the cached ground height as the mirror plane
            float mirrorY = cachedMirrorY;

            // For every source bone, update the corresponding reflection bone.
            foreach (Transform srcBone in sourceBones)
            {
                if (srcBone == null)
                    continue;

                // Look for a matching bone in the reflection hierarchy.
                if (reflectionBoneMap.TryGetValue(srcBone.name, out Transform reflBone))
                {
                    // --- Reflect Position ---
                    // Mirror the source world position relative to mirrorY.
                    Vector3 srcWorldPos = srcBone.position;
                    Vector3 reflWorldPos = new Vector3(srcWorldPos.x, 2 * mirrorY - srcWorldPos.y, srcWorldPos.z);
                    reflBone.position = reflWorldPos;

                    // --- Reflect Rotation ---
                    // To reflect the rotation, we mirror the forward and up directions.
                    Quaternion srcWorldRot = srcBone.rotation;
                    Vector3 srcForward = srcWorldRot * Vector3.forward;
                    Vector3 srcUp = srcWorldRot * Vector3.up;

                    // Flip the Y components to get the reflection.
                    Vector3 reflForward = new Vector3(srcForward.x, -srcForward.y, srcForward.z);
                    Vector3 reflUp = new Vector3(srcUp.x, -srcUp.y, srcUp.z);
                    Quaternion reflWorldRot = Quaternion.LookRotation(reflForward, reflUp);
                    reflBone.rotation = reflWorldRot;
                }
            }
        }
    }
}
