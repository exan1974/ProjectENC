using UnityEngine;

public class LightFollower : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The target to follow (usually the player)")]
    public Transform target;

    [Header("Position Settings")]
    [Tooltip("Height offset above the target")]
    public float heightOffset = 2f;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogError("[LightFollower] Please assign a target in the Inspector.");
            enabled = false;
            return;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Get target's position
        Vector3 targetPosition = target.position;
        
        // Create new position with same X and Z as target, but with our height offset
        Vector3 newPosition = new Vector3(
            targetPosition.x,
            targetPosition.y + heightOffset,
            targetPosition.z
        );

        // Update our position
        transform.position = newPosition;
    }
} 