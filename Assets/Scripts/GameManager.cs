using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("UI Panel to Toggle (e.g., Pause Menu)")]
    [SerializeField] private GameObject toggleScreen;

    private bool isVisible = false;
    public bool isTitleScreen = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !isTitleScreen)
        {
            ToggleScreen();
        }

        // Scene loading with number keys
        if (Input.GetKeyDown(KeyCode.Alpha1)) LoadScene(1);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) LoadScene(2);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) LoadScene(3);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) LoadScene(4);
        else if (Input.GetKeyDown(KeyCode.Alpha5)) LoadScene(5);
        else if (Input.GetKeyDown(KeyCode.Alpha6)) LoadScene(6);
        else if (Input.GetKeyDown(KeyCode.Alpha7)) LoadScene(7);
        else if (Input.GetKeyDown(KeyCode.Alpha8)) LoadScene(8);
        else if (Input.GetKeyDown(KeyCode.Alpha9)) LoadScene(9);
        else if (Input.GetKeyDown(KeyCode.Alpha0)) LoadScene(10);
        // else if (Input.GetKeyDown(KeyCode.Minus)) LoadScene(11);
        // else if (Input.GetKeyDown(KeyCode.Equals)) LoadScene(12);
    }

    private void LoadScene(int sceneIndex)
    {
        // Hide UI screen before loading new scene
        if (toggleScreen != null)
        {
            toggleScreen.SetActive(false);
            isVisible = false;
        }

        if (sceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            try
            {
                if (FadeManager.Instance != null)
                {
                    FadeManager.Instance.LoadSceneWithFade(sceneIndex);
                }
                else
                {
                    Debug.LogWarning("FadeManager not found in scene. Loading scene without fade effect.");
                    SceneManager.LoadScene(sceneIndex);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load scene {sceneIndex}: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"Scene {sceneIndex} is not in build settings!");
        }
    }

    public void ToggleScreen()
    {
        isVisible = !isVisible;
        if (toggleScreen != null)
            toggleScreen.SetActive(isVisible);
    }

    // Optional method to explicitly close from UI button
    public void HideScreen()
    {
        isVisible = false;
        if (toggleScreen != null)
            toggleScreen.SetActive(false);
    }
}
