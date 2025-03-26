using System.Collections;
using UnityEngine;

public class WaterProjectile : MonoBehaviour
{
    private Rigidbody rb;

    [Tooltip("Target multiplier for gravity applied to the water.")]
    [SerializeField] private float gravityMultiplier = 1f;

    [Tooltip("Time (in seconds) over which gravity ramps up from 0 to the target multiplier.")]
    [SerializeField] private float gravityRampUpTime = 1f;

    [Tooltip("Layer mask for the ground. The water will be returned to the pool when it enters a trigger on this layer.")]
    [SerializeField] private LayerMask groundLayerMask;

    // Time when the projectile was launched.
    private float launchTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("WaterProjectile: No Rigidbody found on the water prefab.");
        }
        else
        {
            // Disable built-in gravity to use custom gravity.
            rb.useGravity = false;
        }
        launchTime = Time.time;
    }

    /// <summary>
    /// Launches the water projectile by applying force gradually over the specified duration.
    /// </summary>
    public void Launch(Vector3 direction, float force, float duration)
    {
        Debug.Log($"Launching water with force: {force} over duration: {duration}");
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
        // Ramp up custom gravity over time.
        float timeSinceLaunch = Time.time - launchTime;
        float currentGravityMultiplier = Mathf.Lerp(0f, gravityMultiplier, Mathf.Clamp01(timeSinceLaunch / gravityRampUpTime));
        if (rb != null)
        {
            rb.AddForce(Physics.gravity * currentGravityMultiplier, ForceMode.Acceleration);

            // Align the capsule with its velocity (adjust the Euler offset as needed).
            if (rb.linearVelocity.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(rb.linearVelocity, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 0.2f);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // When hitting an object on the ground layer, return this projectile to the pool.
        if ((groundLayerMask.value & (1 << other.gameObject.layer)) != 0)
        {
            if (WaterProjectilePool.Instance != null)
                WaterProjectilePool.Instance.ReturnProjectile(gameObject);
            else
                Destroy(gameObject);
        }
    }
}
