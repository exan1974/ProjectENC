using UnityEngine;

public class MovementToRotation : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The character whose movement/turn speed we sample.")]
    public Transform sourceCharacter;
    [Tooltip("The object that will rotate around Y.")]
    public Transform targetObject;

    [Header("Mapping Parameters")]
    [Tooltip("Multiplier for how much linear speed contributes to rotation.")]
    public float linearSpeedMultiplier = 1f;
    [Tooltip("Multiplier for how much turning speed contributes to rotation.")]
    public float angularSpeedMultiplier = 1f;
    [Tooltip("Clamp the resulting raw rotation speed between these values.")]
    public float minRotationSpeed =   0f;
    public float maxRotationSpeed = 360f;

    [Header("Smoothing")]
    [Tooltip("Higher = snappier; lower = smoother.")]
    [Range(0.1f, 20f)]
    public float smoothing = 5f;

    // private state
    Vector3    _prevPos;
    Quaternion _prevRot;
    float      _currentRotSpeed;

    void Start()
    {
        if (sourceCharacter == null || targetObject == null)
        {
            Debug.LogError("[MovementToRotation] Assign sourceCharacter and targetObject in the Inspector.");
            enabled = false;
            return;
        }

        _prevPos = sourceCharacter.position;
        _prevRot = sourceCharacter.rotation;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // 1) Compute linear speed (units/sec)
        float linearSpeed = Vector3.Distance(sourceCharacter.position, _prevPos) / dt;

        // 2) Compute angular speed (degrees/sec)
        float angleDelta = Quaternion.Angle(_prevRot, sourceCharacter.rotation);
        float angularSpeed = angleDelta / dt;

        // 3) Combine & clamp
        float rawSpeed = linearSpeed * linearSpeedMultiplier
                       + angularSpeed * angularSpeedMultiplier;
        rawSpeed = Mathf.Clamp(rawSpeed, minRotationSpeed, maxRotationSpeed);

        // 4) Smooth it
        _currentRotSpeed = Mathf.Lerp(
            _currentRotSpeed,
            rawSpeed,
            smoothing * dt
        );

        // 5) Rotate target clockwise around Y
        targetObject.Rotate(
            Vector3.up,
            _currentRotSpeed * dt,
            Space.Self
        );

        // store for next frame
        _prevPos = sourceCharacter.position;
        _prevRot = sourceCharacter.rotation;
    }
}
