using UnityEngine;
using System.Collections.Generic;

public class DelayedRotationMirrorWithSpeedControl : MonoBehaviour
{
    [Header("Reference to the artist and rings")]
    public Transform artist;
    public List<Transform> rings;

    [Header("Delay Settings")]
    public float delayStep = 0.2f;

    [Header("Rotation Offset Settings")]
    public Vector3 offsetEuler = new Vector3(90, 0, 0);
    public bool offsetFirst = false;

    [Header("Rotation Speed (Percentage)")]
    [Range(0, 100)]
    [Tooltip("Rotation speed as percentage of character's rotation speed.")]
    public float rotationSpeedPercent = 50f;

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

    private List<RotationSample> rotationHistory = new List<RotationSample>();
    private float historyDuration = 10f;

    void Update()
    {
        rotationHistory.Add(new RotationSample(Time.time, artist.rotation.normalized));

        while (rotationHistory.Count > 0 && Time.time - rotationHistory[0].time > historyDuration)
        {
            rotationHistory.RemoveAt(0);
        }

        float lerpFactor = Mathf.Clamp01(rotationSpeedPercent / 100f);

        for (int i = 0; i < rings.Count; i++)
        {
            float targetDelay = i * delayStep;
            float targetTime = Time.time - targetDelay;
            Quaternion delayedRotation = GetRotationAtTime(targetTime);

            Quaternion offsetQuat = Quaternion.Euler(offsetEuler);
            Quaternion finalRotation = offsetFirst ? offsetQuat * delayedRotation : delayedRotation * offsetQuat;

            // Smoothly interpolate rotation based on percentage
            rings[i].rotation = Quaternion.Slerp(rings[i].rotation, finalRotation, lerpFactor);
        }
    }

    Quaternion GetRotationAtTime(float targetTime)
    {
        if (rotationHistory.Count == 0)
            return artist.rotation;

        if (targetTime <= rotationHistory[0].time)
            return rotationHistory[0].rotation;

        if (targetTime >= rotationHistory[rotationHistory.Count - 1].time)
            return rotationHistory[rotationHistory.Count - 1].rotation;

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

        if (after == null)
            return rotationHistory[rotationHistory.Count - 1].rotation;

        if (before == null)
            return rotationHistory[0].rotation;

        if (Mathf.Approximately(after.time, before.time))
            return before.rotation;

        Quaternion bRot = before.rotation.normalized;
        Quaternion aRot = after.rotation.normalized;

        if (Quaternion.Dot(bRot, aRot) < 0f)
            aRot = new Quaternion(-aRot.x, -aRot.y, -aRot.z, -aRot.w);

        float t = (targetTime - before.time) / (after.time - before.time);
        return Quaternion.Slerp(bRot, aRot, t).normalized;
    }
}