using UnityEngine;
using System.Collections;

public class FireballController : MonoBehaviour
{
    private Rigidbody rb;

    [Tooltip("Target multiplier for gravity applied to the fireball.")]
    [SerializeField] private float gravityMultiplier = 1f;

    [Tooltip("Time (in seconds) over which gravity ramps up from 0 to the target multiplier.")]
    [SerializeField] private float gravityRampUpTime = 1f;

    [Tooltip("Layer mask for the ground. The fireball will disappear when it enters a trigger on this layer.")]
    [SerializeField] private LayerMask groundLayerMask;

    [Tooltip("Layer mask for trees that can be burned.")]
    [SerializeField] private LayerMask treeLayerMask;

    [Tooltip("Prefab for the fire effect to instantiate upon hitting the ground.")]
    [SerializeField] private GameObject groundFirePrefab;

    private float launchTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("FireballController: No Rigidbody found on the fireball prefab.");
        }
        else
        {
            rb.useGravity = false;
        }
        launchTime = Time.time;
    }

    public void Launch(Vector3 direction, float force, float duration)
    {
        Debug.Log($"Launching fireball with force: {force} over duration: {duration}");
        StartCoroutine(ApplyForceOverTime(direction, force, duration));
    }

    private IEnumerator ApplyForceOverTime(Vector3 direction, float totalForce, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (rb != null)
            {
                rb.AddForce(direction * (totalForce / duration) * Time.fixedDeltaTime, ForceMode.Force);
            }
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

    void FixedUpdate()
    {
        float timeSinceLaunch = Time.time - launchTime;
        float currentGravityMultiplier = Mathf.Lerp(0f, gravityMultiplier, Mathf.Clamp01(timeSinceLaunch / gravityRampUpTime));
        if (rb != null)
        {
            rb.AddForce(Physics.gravity * currentGravityMultiplier, ForceMode.Acceleration);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check for ground collision
        if ((groundLayerMask.value & (1 << other.gameObject.layer)) != 0)
        {
            if (groundFirePrefab != null)
            {
                Instantiate(groundFirePrefab, transform.position, Quaternion.identity);
            }
            Destroy(gameObject);
        }
        // Check for tree collision
        else if ((treeLayerMask.value & (1 << other.gameObject.layer)) != 0)
        {
            TreeGrowthController tree = other.GetComponent<TreeGrowthController>();
            if (tree != null)
            {
                tree.IgniteTree();
            }
            Destroy(gameObject);
        }
    }
}