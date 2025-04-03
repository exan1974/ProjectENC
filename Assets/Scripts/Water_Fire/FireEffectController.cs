using UnityEngine;
using System.Collections;

public class FireEffectController : MonoBehaviour
{
    [Tooltip("Time (in seconds) for the fire to scale from 0.1 to 1.")]
    [SerializeField] private float growTime = 1f;

    [Tooltip("Time (in seconds) to hold at full scale (1).")]
    [SerializeField] private float holdTime = 3f;

    [Tooltip("Time (in seconds) for the fire to shrink from 1 to 0.")]
    [SerializeField] private float shrinkTime = 1f;
        private float fireScale = 0.0f;

    void Start()
    {
        fireScale = transform.localScale.x;
        // Start with a small scale.
        transform.localScale = Vector3.one * 0.1f;
        StartCoroutine(ScaleFire());
    }

    private IEnumerator ScaleFire()
    {
        // Grow from 0.1 to 1.
        float elapsed = 0f;
        while (elapsed < growTime)
        {
            float t = elapsed / growTime;
            transform.localScale = Vector3.Lerp(Vector3.one * 0.1f, Vector3.one * fireScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = Vector3.one * fireScale; // Ensure full scale.

        // Hold full scale for holdTime seconds.
        yield return new WaitForSeconds(holdTime);

        // Shrink from 1 to 0.
        elapsed = 0f;
        Vector3 initialScale = transform.localScale;
        while (elapsed < shrinkTime)
        {
            float t = elapsed / shrinkTime;
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        // Destroy the fire effect after shrinking.
        Destroy(gameObject);
    }
}