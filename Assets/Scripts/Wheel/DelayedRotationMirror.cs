using UnityEngine;
using System.Collections.Generic;

public class DelayedRotationMirror : MonoBehaviour
{
    [Header("Reference to the artist and rings")]
    public Transform artist;              // The artist's transform (chest reference)
    public List<Transform> rings;         // The rings that will mirror the rotation

    [Header("Delay Settings")]
    public float delayStep = 0.2f;        // Delay between rings in seconds

    [Header("Rotation Offset Settings")]
    public Vector3 offsetEuler = new Vector3(90, 0, 0);  // Offset in Euler angles to adjust ring alignment
    public bool offsetFirst = false;      // If true, apply offset first (offset * rotation); if false, apply offset after (rotation * offset)

    // A simple class to store rotation samples over time
    private class RotationSample
    {
        public float time;
        public Quaternion rotation;
        public RotationSample(float time, Quaternion rotation)
        {
            this.time = time;
            this.rotation = rotation;
        }
    }

    // List to keep a history of rotations
    private List<RotationSample> rotationHistory = new List<RotationSample>();
    // How long (in seconds) to keep history (should be longer than the maximum delay)
    private float historyDuration = 10f;

    void Update()
    {
        // Record the artist's current rotation (normalized to avoid interpolation issues)
        rotationHistory.Add(new RotationSample(Time.time, artist.rotation.normalized));

        // Remove old samples
        while (rotationHistory.Count > 0 && Time.time - rotationHistory[0].time > historyDuration)
        {
            rotationHistory.RemoveAt(0);
        }

        // For each ring, calculate the delayed rotation and apply the offset
        for (int i = 0; i < rings.Count; i++)
        {
            // Compute delay for this ring
            float targetDelay = i * delayStep;
            float targetTime = Time.time - targetDelay;
            Quaternion delayedRotation = GetRotationAtTime(targetTime);

            // Build the offset quaternion
            Quaternion offsetQuat = Quaternion.Euler(offsetEuler);
            // Apply the offset either before or after the delayed rotation based on the flag
            Quaternion finalRotation = offsetFirst ? offsetQuat * delayedRotation : delayedRotation * offsetQuat;
            rings[i].rotation = finalRotation;
        }
    }

    // Finds (and interpolates) the rotation at a specific target time from our history
    Quaternion GetRotationAtTime(float targetTime)
    {
        if (rotationHistory.Count == 0)
            return artist.rotation;

        // If target time is before any samples, use the earliest
        if (targetTime <= rotationHistory[0].time)
            return rotationHistory[0].rotation;

        // If target time is after all samples, use the latest
        if (targetTime >= rotationHistory[rotationHistory.Count - 1].time)
            return rotationHistory[rotationHistory.Count - 1].rotation;

        // Find the two samples surrounding the target time
        RotationSample before = null;
        RotationSample after = null;

        for (int j = 0; j < rotationHistory.Count; j++)
        {
            if (rotationHistory[j].time <= targetTime)
                before = rotationHistory[j];
            else
            {
                after = rotationHistory[j];
                break;
            }
        }

        // If no 'after' sample (shouldn't happen due to checks above), use latest
        if (after == null)
            return rotationHistory[rotationHistory.Count - 1].rotation;

        // If no 'before' sample (shouldn't happen due to checks above), use earliest
        if (before == null)
            return rotationHistory[0].rotation;

        // If times are too close, skip interpolation to avoid division by zero
        if (Mathf.Approximately(after.time, before.time))
            return before.rotation;

        // Normalize the quaternions to ensure valid interpolation
        Quaternion bRot = before.rotation.normalized;
        Quaternion aRot = after.rotation.normalized;

        // Ensure we take the shortest path
        if (Quaternion.Dot(bRot, aRot) < 0f)
            aRot = new Quaternion(-aRot.x, -aRot.y, -aRot.z, -aRot.w);

        // Interpolate
        float t = (targetTime - before.time) / (after.time - before.time);
        return Quaternion.Slerp(bRot, aRot, t).normalized;
    }
}