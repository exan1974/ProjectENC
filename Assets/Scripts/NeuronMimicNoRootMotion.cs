using System.Collections.Generic;
using UnityEngine;
using Neuron;   // make sure you have access to NeuronTransformsInstance and NeuronBones

public class NeuronMimicNoRootMotion : MonoBehaviour
{
    [Header("Source (Mocap)")]
    [Tooltip("Your NeuronTransformsInstance on the character that's moving with root motion.")]
    public NeuronTransformsInstance sourceInstance;

    [Header("Target (Static Root)")]
    [Tooltip("Root Transform of the character that should stay in place.")]
    public Transform targetRoot;

    // Internal arrays of bone transforms
    private Transform[] srcBones;
    private Transform[] tgtBones;

    void Start()
    {
        if (sourceInstance == null || targetRoot == null)
        {
            Debug.LogError("[NeuronMimicNoRootMotion] Assign both sourceInstance and targetRoot.");
            enabled = false;
            return;
        }

        // Grab the array of source bones from the Neuron script
        srcBones = sourceInstance.GetTransforms();

        // Build a lookup of all target bones by name
        var map = new Dictionary<string, Transform>();
        foreach (var t in targetRoot.GetComponentsInChildren<Transform>())
            map[t.name] = t;

        // Match source → target by bone name
        tgtBones = new Transform[srcBones.Length];
        for (int i = 0; i < srcBones.Length; i++)
        {
            if (srcBones[i] != null && map.TryGetValue(srcBones[i].name, out var found))
                tgtBones[i] = found;
        }
    }

    void LateUpdate()
    {
        if (srcBones == null) return;

        // Find source root (hips) world position
        int hipsIndex = (int)NeuronBones.Hips;
        var srcRootPos = srcBones[hipsIndex]?.position ?? Vector3.zero;
        var tgtRootPos = targetRoot.position;

        // For each bone, copy world-rotation & world-position relative to root
        for (int i = 0; i < srcBones.Length; i++)
        {
            var s = srcBones[i];
            var t = tgtBones[i];
            if (s == null || t == null) 
                continue;

            // 1) rotation: match exactly
            t.rotation = s.rotation;

            // 2) position: take sourceWorldPos - sourceRootPos, then offset at targetRootPos
            Vector3 offset = s.position - srcRootPos;
            t.position = tgtRootPos + offset;
        }
    }
}
