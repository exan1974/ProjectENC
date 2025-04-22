using UnityEngine;

public class ProximityInverterTargets : MonoBehaviour
{
    [Header("Reference Objects (read‐only)")]
    public Transform refA;
    public Transform refB;

    [Header("Target Objects (to be moved on X axis only)")]
    public Transform targetA;
    public Transform targetB;

    [Header("Tuning")]
    [Tooltip("How strongly to invert their movement. 1 = match the magnitude of the refs’ motion.")]
    public float invertSpeed = 1f;
    [Tooltip("Ignore tiny distance changes to avoid jitter.")]
    public float threshold = 0.001f;

    private float lastRefDistance;

    void Start()
    {
        if (refA == null || refB == null || targetA == null || targetB == null)
        {
            Debug.LogError("[ProximityInverterTargets] Assign refA, refB, targetA and targetB.");
            enabled = false;
            return;
        }

        lastRefDistance = Vector3.Distance(refA.position, refB.position);
    }

    void Update()
    {
        // 1) How much the refs’ separation changed since last frame
        float currentRefDistance = Vector3.Distance(refA.position, refB.position);
        float delta = currentRefDistance - lastRefDistance;

        if (Mathf.Abs(delta) > threshold)
        {
            // 2) Invert that change
            float moveAmount = -delta * invertSpeed;

            // 3) Split it in two, along world‐X only
            float halfMove = moveAmount * 0.5f;

            targetA.position = new Vector3(
                targetA.position.x - halfMove,
                targetA.position.y,
                targetA.position.z
            );

            targetB.position = new Vector3(
                targetB.position.x + halfMove,
                targetB.position.y,
                targetB.position.z
            );
        }

        // 4) Remember for next frame
        lastRefDistance = currentRefDistance;
    }
}
