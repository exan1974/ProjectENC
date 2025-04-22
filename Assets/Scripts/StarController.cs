using System.Collections;
using UnityEngine;

public class StarController : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("Camera to raycast from (leave null to use Camera.main)")]
    public Camera   mainCamera;
    [Tooltip("Layer(s) your juggling balls are on")]
    public LayerMask ballLayerMask;

    [Header("Pulse Settings")]
    [Tooltip("Total time (secs) of the 0→1→0 scale pulse")]
    public float    pulseDuration = 1f;

    bool     _isPulsing;
    Transform _childCollider;  // the actual star’s collider transform

    void Awake()
    {
        // find the first child collider to use as the raycast target
        _childCollider = GetComponentInChildren<Collider>()?.transform;
        if (_childCollider == null)
            Debug.LogWarning($"[{name}] No child Collider found – raycast will use parent position.");

        if (mainCamera == null)
            mainCamera = Camera.main;

        // start hidden
        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        if (_isPulsing || mainCamera == null)
            return;

        Vector3 camPos    = mainCamera.transform.position;
        Vector3 targetPos = (_childCollider != null ? _childCollider.position : transform.position);
        Vector3 dir       = (targetPos - camPos).normalized;
        float   dist      = Vector3.Distance(camPos, targetPos);

        // if the first thing we hit along that ray is a ball, pulse
        if (Physics.Raycast(camPos, dir, out RaycastHit hit, dist, ballLayerMask))
        {
            StartCoroutine(Pulse());
        }
    }

    IEnumerator Pulse()
    {
        _isPulsing = true;
        float half = pulseDuration * 0.5f;
        float t = 0f;

        // Grow 0 → 1
        while (t < half)
        {
            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / half);
            transform.localScale = Vector3.one * f;
            yield return null;
        }

        // Shrink 1 → 0
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / half);
            transform.localScale = Vector3.one * (1f - f);
            yield return null;
        }

        transform.localScale = Vector3.zero;
        _isPulsing = false;
    }
}
