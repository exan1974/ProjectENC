using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstellationEffect : MonoBehaviour
{
    [Header("Prefabs & Skeleton")]
    public GameObject posePrefab;            // your frozen‐pose rig prefab
    public Transform characterRoot;          // live skeleton root
    public Transform characterCentralBone;   // e.g. Hips, for re‐anchoring

    [Header("Placement")]
    public Transform centralPosition;        // where new poses spawn
    public Transform[] storedPositions;      // where old poses end up
    public float scaleReduction = 0.5f;      // how much older poses shrink

    [Header("Fade & Display Timing")]
    [Tooltip("Seconds to fade in/out body and stars")]
    public float fadeDuration = 0.5f;
    [Tooltip("Seconds to stay fully visible at center")]
    public float displayDuration = 2f;

    int currentStoreIndex = 0;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            CapturePose();
    }

    void CapturePose()
    {
        if (currentStoreIndex >= storedPositions.Length)
        {
            Debug.LogWarning("No more stored positions!");
            return;
        }

        // 1) Instantiate & freeze the pose
        GameObject capture = Instantiate(
            posePrefab,
            centralPosition.position,
            centralPosition.rotation
        );
        if (capture.TryGetComponent<Animator>(out var anim))
            anim.enabled = false;
        CopyPoseRecursive(characterRoot, capture.transform);

        // 2) Re‑anchor around the central bone
        if (characterCentralBone != null)
        {
            Transform newBone = capture.transform.Find(characterCentralBone.name);
            if (newBone != null)
            {
                Vector3 offset = capture.transform.position - newBone.position;
                capture.transform.position = centralPosition.position + offset;
            }
        }

        // 3) Gather all SkinnedMeshRenderers for body‑alpha fading
        var bodyRenderers = new List<Renderer>();
        foreach (var sk in capture.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            sk.materials = sk.materials; // instantiate materials
            bodyRenderers.Add(sk);
        }

        // 4) Gather all MeshRenderers for star‑scale tweening
        var stars = new List<StarData>();
        foreach (var mr in capture.GetComponentsInChildren<MeshRenderer>(true))
        {
            // skip any skinned ones (just in case)
            if (mr is SkinnedMeshRenderer) 
                continue;

            stars.Add(new StarData {
                transform     = mr.transform,
                originalScale = mr.transform.localScale
            });
        }

        // 5) Start the fade/hold/move coroutine
        StartCoroutine(PoseLifecycle(capture, bodyRenderers, stars, currentStoreIndex));
        currentStoreIndex++;
    }

    IEnumerator PoseLifecycle(
        GameObject capture,
        List<Renderer> bodyRenderers,
        List<StarData> stars,
        int storeIdx
    )
    {
        // INITIAL: body alpha=0, stars scale=0
        SetBodyAlpha(bodyRenderers, 0f);
        SetStarScale(stars,      0f);

        // FADE IN center (0→1)
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / fadeDuration);
            SetBodyAlpha(bodyRenderers, f);
            SetStarScale(stars, f);
            yield return null;
        }
        SetBodyAlpha(bodyRenderers, 1f);
        SetStarScale(stars,      1f);

        // HOLD fully visible
        yield return new WaitForSeconds(displayDuration);

        // FADE OUT center (1→0)
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / fadeDuration);
            SetBodyAlpha(bodyRenderers, 1f - f);
            SetStarScale(stars,      1f - f);
            yield return null;
        }
        SetBodyAlpha(bodyRenderers, 0f);
        SetStarScale(stars,      0f);

        // MOVE & shrink into stored slot
        var stored = storedPositions[storeIdx];
        capture.transform.position = stored.position;
        capture.transform.rotation = stored.rotation;
        capture.transform.localScale *= scaleReduction;

        // FADE IN stored (0→1)
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / fadeDuration);
            SetBodyAlpha(bodyRenderers, f);
            SetStarScale(stars, f);
            yield return null;
        }
        SetBodyAlpha(bodyRenderers, 1f);
        SetStarScale(stars,      1f);
    }

    // fades all body renderers’ material alpha
    void SetBodyAlpha(List<Renderer> renderers, float alpha)
    {
        foreach (var rend in renderers)
        {
            foreach (var mat in rend.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                }
                else if (mat.HasProperty("_BaseColor"))
                {
                    Color c = mat.GetColor("_BaseColor");
                    c.a = alpha;
                    mat.SetColor("_BaseColor", c);
                }
            }
        }
    }

    // scales each star from 0→originalScale by factor
    void SetStarScale(List<StarData> stars, float factor)
    {
        foreach (var sd in stars)
            sd.transform.localScale = sd.originalScale * factor;
    }

    // copy local transforms so the pose is frozen
    void CopyPoseRecursive(Transform src, Transform dst)
    {
        dst.localPosition = src.localPosition;
        dst.localRotation = src.localRotation;
        dst.localScale    = src.localScale;
        for (int i = 0; i < src.childCount && i < dst.childCount; i++)
            CopyPoseRecursive(src.GetChild(i), dst.GetChild(i));
    }

    class StarData
    {
        public Transform transform;
        public Vector3   originalScale;
    }
}
