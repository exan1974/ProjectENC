using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Tooltip("Index of the scene to load (0–8)")]
    [Range(0, 8)]
    public int sceneIndex;

    [Tooltip("Press a key (1–9) to test scene loading via keyboard")]
    public bool enableKeyInput = false;

    void Update()
    {
        if (!enableKeyInput) return;

        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
            {
                LoadSceneByIndex(i);
            }
        }
    }

    public void LoadSceneByIndex(int index)
    {
        if (index < 0 || index >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning($"Scene index {index} is out of range.");
            return;
        }

        SceneManager.LoadScene(index);
    }

    // Optional: For use in a UI Button via Inspector
    public void LoadScene()
    {
        LoadSceneByIndex(sceneIndex);
    }
}
