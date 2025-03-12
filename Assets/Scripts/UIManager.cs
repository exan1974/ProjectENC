using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject uiPanel; // Parent UI panel that contains buttons and text elements
    [SerializeField] private Button bestButton;
    [SerializeField] private Button secondBestButton;
    [SerializeField] private TextMeshProUGUI hitsText;
    [SerializeField] private TextMeshProUGUI missesText;

    // New: References for the camera info list
    [SerializeField] private GameObject cameraInfoPrefab; // Prefab with 3 TextMeshProUGUI components (Name, Hits, Misses)
    [SerializeField] private Transform cameraInfoContainer; // Parent container for the instantiated camera info prefabs

    [SerializeField] private MocapCameraPlacement mocapCameraPlacement;

    private MocapCameraPlacement.CameraConfiguration bestConfig;
    private MocapCameraPlacement.CameraConfiguration secondBestConfig;

    private void Start()
    {
        // Hide UI on start.
        if (uiPanel != null)
            uiPanel.SetActive(false);

        if (mocapCameraPlacement != null)
        {
            // Subscribe to the event triggered when configurations are ready.
            mocapCameraPlacement.OnConfigurationsReady += OnConfigurationsReady;
        }

        // Setup button listeners.
        if (bestButton != null)
            bestButton.onClick.AddListener(() => { SetActiveConfiguration(0); });
        if (secondBestButton != null)
            secondBestButton.onClick.AddListener(() => { SetActiveConfiguration(1); });
    }

    private void OnConfigurationsReady(MocapCameraPlacement.CameraConfiguration best, MocapCameraPlacement.CameraConfiguration second)
    {
        bestConfig = best;
        secondBestConfig = second;
        // Show the UI panel now that the configurations are ready.
        if (uiPanel != null)
            uiPanel.SetActive(true);
        // Default to showing the best configuration.
        SetActiveConfiguration(0);
    }

    private void SetActiveConfiguration(int configIndex)
    {
        if (mocapCameraPlacement != null)
        {
            mocapCameraPlacement.SetActiveConfiguration(configIndex);
        }
        if (configIndex == 0 && bestConfig != null)
        {
            int total = bestConfig.totalHits + bestConfig.totalMisses;
            float hitPercent = total > 0 ? (float)bestConfig.totalHits / total * 100f : 0f;
            float missPercent = total > 0 ? (float)bestConfig.totalMisses / total * 100f : 0f;
            if (hitsText != null)
                hitsText.text = bestConfig.totalHits + " (" + hitPercent.ToString("F1") + " %)";
            if (missesText != null)
                missesText.text = bestConfig.totalMisses + " (" + missPercent.ToString("F1") + " %)";
            // Update the camera info list display using bestConfig.
            UpdateCameraInfoList(bestConfig);
        }
        else if (configIndex == 1 && secondBestConfig != null)
        {
            int total = secondBestConfig.totalHits + secondBestConfig.totalMisses;
            float hitPercent = total > 0 ? (float)secondBestConfig.totalHits / total * 100f : 0f;
            float missPercent = total > 0 ? (float)secondBestConfig.totalMisses / total * 100f : 0f;
            if (hitsText != null)
                hitsText.text = secondBestConfig.totalHits + " (" + hitPercent.ToString("F1") + " %)";
            if (missesText != null)
                missesText.text = secondBestConfig.totalMisses + " (" + missPercent.ToString("F1") + " %)";
            // Update the camera info list display using secondBestConfig.
            UpdateCameraInfoList(secondBestConfig);
        }
    }

    private void UpdateCameraInfoList(MocapCameraPlacement.CameraConfiguration config)
    {
        // Clear any existing children in the container.
        foreach (Transform child in cameraInfoContainer)
        {
            Destroy(child.gameObject);
        }

        // Instantiate a new prefab for each camera in the configuration.
        for (int i = 0; i < config.positions.Count; i++)
        {
            GameObject camInfoGO = Instantiate(cameraInfoPrefab, cameraInfoContainer);
            // Assume the prefab has three TextMeshProUGUI components (ordered: Name, Hits, Misses)
            TextMeshProUGUI[] texts = camInfoGO.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 3)
            {
                int total = config.perCameraHits[i] + config.perCameraMisses[i];
                float hitPercent = total > 0 ? (float)config.perCameraHits[i] / total * 100f : 0f;
                float missPercent = total > 0 ? (float)config.perCameraMisses[i] / total * 100f : 0f;
                
                texts[0].text = "Camera " + (i + 1);
                texts[1].text = config.perCameraHits[i].ToString("F0") + " (" + hitPercent.ToString("F1") + " %)";
                texts[2].text = config.perCameraMisses[i].ToString("F0")+ " (" + missPercent.ToString("F1") + " %)";;
            }
        }
    }
}
