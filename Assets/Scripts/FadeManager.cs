using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class FadeManager : MonoBehaviour
{
    private static FadeManager instance;
    public static FadeManager Instance { get { return instance; } }

    [Header("Fade Settings")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private Color fadeColor = Color.black;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Ensure we have the fade image
            if (fadeImage == null)
            {
                Debug.LogError("Fade Image not assigned to FadeManager!");
            }
            else
            {
                // Set initial state
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadSceneWithFade(int sceneIndex)
    {
        StartCoroutine(FadeAndLoadScene(sceneIndex));
    }

    private IEnumerator FadeAndLoadScene(int sceneIndex)
    {
        // Fade out
        yield return StartCoroutine(Fade(0f, 1f));

        // Load the scene
        SceneManager.LoadScene(sceneIndex);

        // Fade in
        yield return StartCoroutine(Fade(1f, 0f));
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float elapsedTime = 0f;
        Color currentColor = fadeColor;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / fadeDuration);
            currentColor.a = currentAlpha;
            fadeImage.color = currentColor;
            yield return null;
        }

        // Ensure we reach the target alpha
        currentColor.a = endAlpha;
        fadeImage.color = currentColor;
    }
} 