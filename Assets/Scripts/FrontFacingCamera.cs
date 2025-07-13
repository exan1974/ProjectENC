using UnityEngine;

public class FrontFacingCamera : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; // The character to follow
    public Vector3 targetOffset = new Vector3(0, 1.5f, 0); // Offset from character's position

    [Header("Camera Settings")]
    public float defaultDistance = 5f; // Default distance from character
    public float transitionSpeed = 5f; // Speed of camera transition

    [Header("Input")]
    public KeyCode lockOnKey = KeyCode.Space;

    private Vector3 defaultPosition;
    private Quaternion defaultRotation;
    private bool isLockedOn = false;
    private bool isTransitioning = false;
    private float transitionProgress = 0f;

    // Lock-on state
    private float lockedDistance = 0f;
    private float lockedYPosition = 0f;
    private float lockedYRotation = 0f;
    private Vector3 transitionStartPos;
    private Vector3 transitionTargetPos;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("No target assigned to FrontFacingCamera script!");
            return;
        }

        // Initialize default camera position
        defaultPosition = transform.position;
        defaultRotation = transform.rotation;
    }

    void Update()
    {
        if (target == null) return;

        // Toggle lock-on when space is pressed
        if (Input.GetKeyDown(lockOnKey))
        {
            isLockedOn = !isLockedOn;
            isTransitioning = true;
            transitionProgress = 0f;
        if (isLockedOn)
        {
                // Calculate lock-on parameters
                Vector3 targetPos = target.position;
                Vector3 camToTarget = transform.position - targetPos;
                Vector2 camToTargetXZ = new Vector2(camToTarget.x, camToTarget.z);
                lockedDistance = camToTargetXZ.magnitude;
                lockedYPosition = transform.position.y;
                lockedYRotation = transform.eulerAngles.y;

                // Set transition targets
                transitionStartPos = transform.position;
                Vector2 targetForwardXZ = new Vector2(target.forward.x, target.forward.z).normalized;
                Vector3 finalPos = new Vector3(
                    targetPos.x + targetForwardXZ.x * lockedDistance,
                    lockedYPosition,
                    targetPos.z + targetForwardXZ.y * lockedDistance
                );
                transitionTargetPos = finalPos;
            }
            else
            {
                // Transition back to default
                transitionStartPos = transform.position;
                transitionTargetPos = defaultPosition;
            }
        }

        if (isTransitioning)
        {
            transitionProgress += Time.deltaTime * transitionSpeed;
            float t = Mathf.Clamp01(transitionProgress);
            transform.position = Vector3.Lerp(transitionStartPos, transitionTargetPos, t);
            if (t >= 1f)
            {
                isTransitioning = false;
            }
        }
        else if (isLockedOn)
        {
            // Calculate position in XZ plane only
            Vector2 targetForwardXZ = new Vector2(target.forward.x, target.forward.z).normalized;
            Vector3 newPos = new Vector3(
                target.position.x + targetForwardXZ.x * lockedDistance,
                lockedYPosition,
                target.position.z + targetForwardXZ.y * lockedDistance
            );
            transform.position = newPos;
        }

        // Always maintain the locked Y rotation when locked on
        if (isLockedOn)
        {
            Vector3 currentEuler = transform.eulerAngles;
            transform.eulerAngles = new Vector3(currentEuler.x, lockedYRotation, currentEuler.z);
        }
        else if (!isTransitioning)
        {
            transform.rotation = defaultRotation;
        }
    }

    void LateUpdate()
    {
        if (!isTransitioning && !isLockedOn)
        {
            // Your default camera behavior can go here
        }
    }
}