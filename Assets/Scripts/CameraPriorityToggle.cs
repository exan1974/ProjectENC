using UnityEngine;
using Cinemachine;
using TMPro;

public class CameraPriorityToggle : MonoBehaviour
{
    [Header("Virtual Camera References")]
    [Tooltip("The first virtual camera (default view)")]
    public CinemachineVirtualCamera defaultVCam;
    [Tooltip("The second virtual camera (alternate view)")]
    public CinemachineVirtualCamera alternateVCam;

    [Header("UI References")]
    [Tooltip("Text display showing which camera is active")]
    public TextMeshProUGUI cameraStateText;

    [Header("Settings")]
    [Tooltip("Priority value for the active camera")]
    public int activePriority = 20;
    [Tooltip("Priority value for the inactive camera")]
    public int inactivePriority = 10;
    [Tooltip("Key to toggle between cameras")]
    public KeyCode toggleKey = KeyCode.Space;

    private bool m_isAlternateActive = false;
    private const string FIXED_CAM_TEXT = "Camera: Fixed";
    private const string FOLLOW_CAM_TEXT = "Camera: Follow";

    void Start()
    {
        if (defaultVCam == null || alternateVCam == null)
        {
            Debug.LogError("Virtual camera references not set in CameraPriorityToggle!");
            enabled = false;
            return;
        }

        // Initialize priorities
        defaultVCam.Priority = activePriority;
        alternateVCam.Priority = inactivePriority;

        // Initialize UI text
        UpdateCameraText();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            m_isAlternateActive = !m_isAlternateActive;
            
            // Switch priorities
            defaultVCam.Priority = m_isAlternateActive ? inactivePriority : activePriority;
            alternateVCam.Priority = m_isAlternateActive ? activePriority : inactivePriority;

            // Update UI text
            UpdateCameraText();
        }
    }

    private void UpdateCameraText()
    {
        if (cameraStateText != null)
        {
            cameraStateText.text = m_isAlternateActive ? FOLLOW_CAM_TEXT : FIXED_CAM_TEXT;
        }
    }
} 