using UnityEngine;
using System.Collections;

public class MeshTrail : MonoBehaviour
{
    [Header("Trail Settings")]
    [SerializeField] private float activeTime = 2f;
    [SerializeField] private float meshRefreshRate = 0.1f;
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private Material trailMaterial;
    [SerializeField] private Transform positionToSpawn;
    [SerializeField, Range(0f, 1f)] private float maxOpacity = 1f;
    [SerializeField, Range(0f, 1f)] private float trailsPerFrame = 1f;

    [Header("Debug Settings")]
    [SerializeField] private bool debugMode = false;

    private SkinnedMeshRenderer[] skinnedMeshRenderers;
    private bool isTrailActive = false;
    private static readonly int OpacityProperty = Shader.PropertyToID("_Opacity");

    private void Start()
    {
        if (skinnedMeshRenderers == null)
        {
            skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        }
        
        if (debugMode)
        {
            Debug.Log($"MeshTrail initialized with {skinnedMeshRenderers.Length} mesh renderers");
        }

        // Validate trail material
        if (trailMaterial == null)
        {
            Debug.LogError("Trail material is not assigned!");
        }

        BeginTrail();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) && !isTrailActive)
        {
            if (debugMode) Debug.Log("Trail triggered");
            BeginTrail();
        }
    }

    public void BeginTrail()
    {
        if (skinnedMeshRenderers == null || skinnedMeshRenderers.Length == 0)
        {
            Debug.LogError("No SkinnedMeshRenderers found!");
            return;
        }

        if (trailMaterial == null)
        {
            Debug.LogError("Trail material is not assigned!");
            return;
        }

            isTrailActive = true;
        StartCoroutine(ActivateTrail(activeTime));
        }

    private IEnumerator ActivateTrail(float timeActive)
    {
        int frameCounter = 0;
        int interval = Mathf.Max(1, Mathf.RoundToInt(1f / Mathf.Max(trailsPerFrame, 0.0001f)));
        while (timeActive > 0)
        {
            timeActive -= meshRefreshRate;

            if (trailsPerFrame > 0f && frameCounter % interval == 0)
            {
            for (int i = 0; i < skinnedMeshRenderers.Length; i++)
            {
                    if (skinnedMeshRenderers[i] == null) continue;

                    GameObject trailObject = new GameObject("Trail_" + i);
                    trailObject.transform.SetPositionAndRotation(positionToSpawn.position, positionToSpawn.rotation);

                    MeshRenderer meshRenderer = trailObject.AddComponent<MeshRenderer>();
                    MeshFilter meshFilter = trailObject.AddComponent<MeshFilter>();

                Mesh mesh = new Mesh();
                skinnedMeshRenderers[i].BakeMesh(mesh);
                    meshFilter.mesh = mesh;

                    Material instanceMaterial = new Material(trailMaterial);
                    meshRenderer.material = instanceMaterial;

                    StartCoroutine(FadeTrail(instanceMaterial, trailObject));
                }
            }
            frameCounter++;
            yield return new WaitForSeconds(meshRefreshRate);
        }

        isTrailActive = false;
    }

    private IEnumerator FadeTrail(Material material, GameObject trailObject)
    {
        material.SetFloat(OpacityProperty, maxOpacity);
        float elapsedTime = 0;

        while (elapsedTime < fadeOutDuration)
        {
            float normalizedTime = elapsedTime / fadeOutDuration;
            float curveT = Mathf.Pow(normalizedTime, 2); // quadratic ease-in
            float currentOpacity = Mathf.Lerp(maxOpacity, 0f, curveT);
            material.SetFloat(OpacityProperty, currentOpacity);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(trailObject);
        }
    }

