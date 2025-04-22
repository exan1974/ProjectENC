using System.Collections.Generic;
using UnityEngine;

namespace Neuron
{
    /// <summary>
    /// This script streams a “reflected” version of the mocap-driven character.
    /// It expects that:
    /// 1. The source character is being updated by a NeuronTransformsInstance (or a similar script),
    ///    and that its bones can be accessed via GetTransforms().
    /// 2. The reflection character uses the same hierarchy (bone names must match).
    /// 3. A water plane exists at a specified Y (waterHeight) so that the reflection is calculated relative to it.
    /// 
    /// The reflection is computed by:
    /// - Reflecting each bone’s world position across the horizontal plane:
    ///     reflectedPos.y = 2 * waterHeight - sourcePos.y
    /// - Reflecting the bone’s rotation by inverting the vertical components of its forward/up vectors.
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

        // Dictionary to map bone names to transforms in the reflection hierarchy.
        private Dictionary<string, Transform> reflectionBoneMap;

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
        }

        void LateUpdate()
        {
            // Ensure the source character and its transforms are available.
            if (sourceCharacter == null || reflectionBoneMap == null)
                return;

            // Get the array of source bone transforms.
            Transform[] sourceBones = sourceCharacter.GetTransforms();

            if (sourceBones == null)
                return;

            // For every source bone, update the corresponding reflection bone.
            foreach (Transform srcBone in sourceBones)
            {
                if (srcBone == null)
                    continue;

                // Look for a matching bone in the reflection hierarchy.
                if (reflectionBoneMap.TryGetValue(srcBone.name, out Transform reflBone))
                {
                    // --- Reflect Position ---
                    // Mirror the source world position relative to waterHeight.
                    Vector3 srcWorldPos = srcBone.position;
                    Vector3 reflWorldPos = new Vector3(srcWorldPos.x, 2 * waterHeight - srcWorldPos.y, srcWorldPos.z);
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
