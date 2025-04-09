using UnityEngine;
using TMPro;  // For TextMeshPro support

public class ConstellationEffect : MonoBehaviour
{
    [Header("Prefabs and References")]
    public GameObject posePrefab;           // The prefab used to capture the pose
    public Transform characterRoot;         // The root of the animated character (e.g., the whole model)
    public Transform characterCentralBone;  // The central bone (e.g., the Hips) to re-anchor the capture
    public TextMeshProUGUI nameDisplay;     // External UI element to display the name on screen for the active snapshot

    [Header("Constellation Settings")]
    public Transform centralPosition;       // The position where the new snapshot will be instantiated (exposed position)
    public Transform[] storedPositions;     // Predefined positions for previously captured snapshots
    public float scaleReduction = 0.5f;       // Scale reduction factor for snapshots moved to stored positions

    [Header("Name Settings")]
    [Tooltip("List of possible names for the captured pose")]
    public string[] names;                  // List of names to choose from randomly

    // Internal state tracking
    private int currentStoreIndex = 0;
    private GameObject activeCapture = null;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            HandleSpacePress();
        }
    }

    /// <summary>
    /// Handles the Space key press:
    /// - Moves any previous snapshot to a stored position.
    /// - Instantiates a new snapshot at the central position and copies the animated pose to it.
    /// - Re-anchors the snapshot based on the central bone.
    /// - Chooses a random name from the list and updates the external UI display (nameDisplay).
    /// </summary>
    void HandleSpacePress()
    {
        // If a previous capture exists, move it to a stored position and scale it down.
        if (activeCapture != null)
        {
            if (currentStoreIndex < storedPositions.Length)
            {
                activeCapture.transform.localScale *= scaleReduction;
                activeCapture.transform.position = storedPositions[currentStoreIndex].position;
                currentStoreIndex++;
            }
            else
            {
                Debug.LogWarning("No more stored positions available for additional captures.");
            }
        }

        // Instantiate a new snapshot at the central (exposed) position.
        GameObject newCapture = Instantiate(posePrefab, centralPosition.position, centralPosition.rotation);

        // Optional: disable the Animator on the new snapshot so the pose remains frozen.
        Animator newAnimator = newCapture.GetComponent<Animator>();
        if (newAnimator != null)
            newAnimator.enabled = false;

        // Copy the animated pose from the character's skeleton to the new snapshot.
        CopyPoseRecursive(characterRoot, newCapture.transform);

        // Re-anchor the snapshot using the central bone.
        if (characterCentralBone != null)
        {
            // Find the corresponding bone in the new capture by name (assuming names match).
            Transform targetCentralBone = newCapture.transform.Find(characterCentralBone.name);
            if (targetCentralBone != null)
            {
                // Compute the offset so that the new capture's central bone aligns with centralPosition.
                Vector3 offset = newCapture.transform.position - targetCentralBone.position;
                newCapture.transform.position = centralPosition.position + offset;
            }
            else
            {
                Debug.LogWarning("Central bone (" + characterCentralBone.name + ") not found in the new capture.");
            }
        }

        // Choose a random name from the names list and update the external UI.
        string randomName = (names != null && names.Length > 0) ? names[Random.Range(0, names.Length)] : "Default Name";
        if (nameDisplay != null)
            nameDisplay.text = randomName;

        // Set the new snapshot as the currently active capture.
        activeCapture = newCapture;
    }

    /// <summary>
    /// Recursively copies transform data (localPosition, localRotation, localScale)
    /// from the source animated character to the target snapshot. Assumes the hierarchies match.
    /// </summary>
    /// <param name="source">The source transform (animated character)</param>
    /// <param name="target">The target transform (new snapshot)</param>
    void CopyPoseRecursive(Transform source, Transform target)
    {
        target.localPosition = source.localPosition;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;

        for (int i = 0; i < source.childCount; i++)
        {
            if (i < target.childCount)
            {
                CopyPoseRecursive(source.GetChild(i), target.GetChild(i));
            }
        }
    }
}
