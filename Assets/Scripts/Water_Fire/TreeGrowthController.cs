using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TreeGrowthController : MonoBehaviour
{
    // Static list to track all instantiated trees.
    public static List<TreeGrowthController> allTrees = new List<TreeGrowthController>();

    [Tooltip("Buffer time (in seconds) after initial growth during which water does nothing.")]
    [SerializeField] private float bufferTime = 1f;
    [Tooltip("Growth increment (scale increase) per water hit.")]
    [SerializeField] private float growthIncrement = 0.05f;
    [Tooltip("Maximum uniform scale of the tree.")]
    [SerializeField] private float maxScale = 1f;
    
    [Tooltip("Initial scale that the tree grows to upon instantiation.")]
    [SerializeField] private float initialScale = 0.1f;
    [Tooltip("Time (in seconds) over which the tree grows from 0 to its initial scale.")]
    [SerializeField] private float instantiationGrowTime = 0.5f;
    [SerializeField] private LayerMask waterLayer;

    // Time recorded after the tree finishes its initial growth.
    private float instantiationTime;

    void Awake()
    {
        allTrees.Add(this);
        // Start at zero scale.
        transform.localScale = Vector3.zero;
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
        // Ensure final scale is set.
        transform.localScale = Vector3.one * initialScale;
        // Record the time when the initial growth is complete.
        instantiationTime = Time.time;
    }

    void OnDestroy()
    {
        allTrees.Remove(this);
    }

    // When water enters the tree's trigger, increase its scale (after the buffer time).
    private void OnTriggerEnter(Collider other)
    {
        if ((waterLayer.value & (1 << other.gameObject.layer)) != 0)        {
            // Only allow growth if the buffer period after initial growth has passed.
            Debug.Log("entrou trigger");
            if (Time.time - instantiationTime >= bufferTime)
            {
                Debug.Log("entrou trigger22222222222");

                float currentScale = transform.localScale.x; // Assume uniform scaling.
                if (currentScale < maxScale)
                {
                    float newScale = Mathf.Min(currentScale + growthIncrement, maxScale);
                    transform.localScale = Vector3.one * newScale;
                }
            }
        }
    }
}
