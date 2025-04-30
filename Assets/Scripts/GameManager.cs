using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("UI Panel to Toggle (e.g., Pause Menu)")]
    [SerializeField] private GameObject toggleScreen;

    private bool isVisible = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleScreen();
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
