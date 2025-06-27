using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraLayerSetup : MonoBehaviour
{
    private void Start()
    {
        Camera cam = GetComponent<Camera>();
        
        // Set up camera to render all our layers
        cam.cullingMask = LayerMask.GetMask(
            "Default",
            "Background",
            "TrailEffects",
            "Player"
        );

        // Clear flags to solid color for clean background
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
    }
} 