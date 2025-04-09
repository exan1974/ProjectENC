using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        // Cache the main camera reference.
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCamera != null)
        {
            // Calculate a target point for the billboard.
            // The object's forward direction will point toward the camera.
            Vector3 targetPosition = transform.position + mainCamera.transform.rotation * Vector3.forward;
            Vector3 upDirection = mainCamera.transform.rotation * Vector3.up;
            transform.LookAt(targetPosition, upDirection);
        }
    }
}
