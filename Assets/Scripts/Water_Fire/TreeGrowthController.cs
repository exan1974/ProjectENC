using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TreeGrowthController : MonoBehaviour
{
    public static List<TreeGrowthController> allTrees = new List<TreeGrowthController>();

    [Header("Growth Settings")]
    [SerializeField] private float bufferTime = 1f;
    [SerializeField] private float growthIncrement = 0.05f;
    [SerializeField] private float maxScale = 1f;
    [SerializeField] private float initialScale = 0.1f;
    [SerializeField] private float instantiationGrowTime = 0.5f;
    [SerializeField] private float growthDuration = 1f;
    [SerializeField] private LayerMask waterLayer;

    [Header("Fire Settings")]
    [SerializeField] private ParticleSystem fireParticles;
    [SerializeField] private float burnDuration = 5f;
    [SerializeField] private float burnScaleReductionSpeed = 0.2f;
    [SerializeField] private Material burntMaterial;
    
    private float instantiationTime;
    private bool isGrowing = false;
    private bool waterInContact = false;
    private bool isBurning = false;
    private Material originalMaterial;
    private Renderer treeRenderer;


    void Awake()
    {
        allTrees.Add(this);
        transform.localScale = Vector3.zero;
        
        treeRenderer = GetComponentInChildren<Renderer>();
        if (treeRenderer != null)
        {
            originalMaterial = treeRenderer.material;
        }
        
        if (fireParticles != null)
        {
            fireParticles.Stop();
            fireParticles.gameObject.SetActive(false);
        }
        
        StartCoroutine(GrowFromZero());
    }

    private IEnumerator GrowFromZero()
    {
        float elapsed = 0f;
        while (elapsed < instantiationGrowTime)
        {
            float t = elapsed / instantiationGrowTime;
            transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * initialScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = Vector3.one * initialScale;
        instantiationTime = Time.time;
    }

    public void IgniteTree()
    {
        if (isBurning) return;
        
        isBurning = true;
        
        if (fireParticles != null)
        {
            fireParticles.gameObject.SetActive(true);
            fireParticles.Play();
        }
        
        StartCoroutine(BurnTree());
    }

    private IEnumerator BurnTree()
    {
        Vector3 originalScale = transform.localScale;
        
        while (transform.localScale.x > 0.01f && transform.localScale.z > 0.01f)
        {
            // Calculate burn progress based on time
            float burnProgress = Mathf.Clamp01(Time.deltaTime / burnDuration);
            
            // Reduce scale on X and Z axes
            float newXScale = transform.localScale.x - (originalScale.x * burnScaleReductionSpeed * burnProgress);
            float newZScale = transform.localScale.z - (originalScale.z * burnScaleReductionSpeed * burnProgress);
            
            // Ensure scales don't go below 0
            newXScale = Mathf.Max(0, newXScale);
            newZScale = Mathf.Max(0, newZScale);
            
            // Apply new scale (keep Y scale the same)
            transform.localScale = new Vector3(
                newXScale,
                transform.localScale.y,
                newZScale
            );
            
            // Darken the material
            if (treeRenderer != null && burntMaterial != null)
            {
                float currentBurnProgress = 1f - (newXScale / originalScale.x);
                treeRenderer.material.Lerp(originalMaterial, burntMaterial, currentBurnProgress);
            }
            
            yield return null;
        }
        
        // Ensure complete destruction when X/Z scales reach 0
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        allTrees.Remove(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isBurning) return;
        
        if ((waterLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            if (!waterInContact)
            {
                waterInContact = true;
                TryGrow();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if ((waterLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            waterInContact = false;
        }
    }

    private void TryGrow()
    {
        if (Time.time - instantiationTime < bufferTime) return;
        if (isGrowing || isBurning) return;
        
        float currentScale = transform.localScale.x;
        if (currentScale >= maxScale) return;
        StopAllCoroutines();
        float targetScale = Mathf.Min(currentScale + growthIncrement, maxScale);
        StartCoroutine(GrowOverTime(currentScale, targetScale));
    }

    private IEnumerator GrowOverTime(float startScale, float targetScale)
    {
        isGrowing = true;
        float elapsed = 0f;
        while (elapsed < growthDuration)
        {
            float t = elapsed / growthDuration;
            float newScale = Mathf.Lerp(startScale, targetScale, t);
            transform.localScale = Vector3.one * newScale;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = Vector3.one * targetScale;
        isGrowing = false;
    }
}