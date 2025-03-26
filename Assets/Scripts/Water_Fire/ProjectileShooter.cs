using UnityEngine;
using System.Collections;

public class ProjectileShooter : MonoBehaviour
{
    public enum ShootingMode { Fire, Water }
    
    [Header("General Mode Selection")]
    [Tooltip("Current shooting mode. Press 1 for fire, 2 for water.")]
    [SerializeField] private ShootingMode currentMode = ShootingMode.Fire;

    [Header("Left Arm")]
    [Tooltip("Left shoulder transform (used as a reference).")]
    [SerializeField] private Transform leftShoulder;
    [Tooltip("Left elbow transform.")]
    [SerializeField] private Transform leftElbow;
    [Tooltip("Left hand (or fingertip) transform where the projectile will spawn.")]
    [SerializeField] private Transform leftHand;

    [Header("Right Arm")]
    [Tooltip("Right shoulder transform (used as a reference).")]
    [SerializeField] private Transform rightShoulder;
    [Tooltip("Right elbow transform.")]
    [SerializeField] private Transform rightElbow;
    [Tooltip("Right hand (or fingertip) transform where the projectile will spawn.")]
    [SerializeField] private Transform rightHand;

    [Header("Fireball Settings")]
    [Tooltip("Prefab for the fireball to instantiate.")]
    [SerializeField] private GameObject fireballPrefab;
    [Tooltip("Minimum force to apply to the fireball.")]
    [SerializeField] private float minFireballForce = 5f;
    [Tooltip("Maximum force to apply to the fireball.")]
    [SerializeField] private float maxFireballForce = 15f;
    [Tooltip("Duration (in seconds) over which the force is applied for a slow shot.")]
    [SerializeField] private float shotDuration = 0.5f;

    [Header("Water Hose Settings")]
    [Tooltip("Prefab for the water particle (capsule) to instantiate.")]
    [SerializeField] private GameObject waterParticlePrefab;
    [Tooltip("Number of water particles spawned per second.")]
    [SerializeField] private float waterParticlesPerSecond = 20f;
    [Tooltip("Duration (in seconds) of the water burst.")]
    [SerializeField] private float waterBurstDuration = 1f;
    [Tooltip("Spread angle (in degrees) for the water particles cone.")]
    [SerializeField] private float waterSpreadAngle = 15f;
    [Tooltip("Force applied to each water particle.")]
    [SerializeField] private float waterParticleForce = 10f;

    [Header("Arm Extension Settings")]
    [Tooltip("Angle threshold (in degrees) for considering an arm as fully extended.")]
    [SerializeField] private float armExtensionAngleThreshold = 165f;

    [Header("Stomach Reference")]
    [Tooltip("Reference to the stomach transform. Projectiles are shot only if the hand is above this point.")]
    [SerializeField] private Transform stomach;

    // Flags to ensure one shot per arm extension event.
    private bool leftShot = false;
    private bool rightShot = false;

    void Update()
    {
        // Mode switching by key press.
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentMode = ShootingMode.Fire;
            Debug.Log("Switched to Fire mode.");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentMode = ShootingMode.Water;
            Debug.Log("Switched to Water mode.");
        }

        HandleLeftArm();
        HandleRightArm();
    }

    void HandleLeftArm()
    {
        if (leftShoulder == null || leftElbow == null || leftHand == null || stomach == null)
        {
            Debug.LogWarning("Left arm or stomach transforms are not assigned.");
            return;
        }

        // Calculate the angle at the left elbow.
        float leftAngle = Vector3.Angle(leftShoulder.position - leftElbow.position, leftHand.position - leftElbow.position);
        bool isHandAboveStomach = leftHand.position.y > stomach.position.y;

        if (leftAngle >= armExtensionAngleThreshold && isHandAboveStomach)
        {
            if (!leftShot)
            {
                if (currentMode == ShootingMode.Fire)
                {
                    ShootFireball(leftHand, leftShoulder, false);
                }
                else if (currentMode == ShootingMode.Water)
                {
                    StartCoroutine(ShootWaterStream(leftHand, leftShoulder, false));
                }
                leftShot = true;
            }
        }
        else
        {
            leftShot = false;
        }
    }

    void HandleRightArm()
    {
        if (rightShoulder == null || rightElbow == null || rightHand == null || stomach == null)
        {
            Debug.LogWarning("Right arm or stomach transforms are not assigned.");
            return;
        }

        float rightAngle = Vector3.Angle(rightShoulder.position - rightElbow.position, rightHand.position - rightElbow.position);
        bool isHandAboveStomach = rightHand.position.y > stomach.position.y;

        if (rightAngle >= armExtensionAngleThreshold && isHandAboveStomach)
        {
            if (!rightShot)
            {
                if (currentMode == ShootingMode.Fire)
                {
                    ShootFireball(rightHand, rightShoulder, true);
                }
                else if (currentMode == ShootingMode.Water)
                {
                    StartCoroutine(ShootWaterStream(rightHand, rightShoulder, true));
                }
                rightShot = true;
            }
        }
        else
        {
            rightShot = false;
        }
    }

    /// <summary>
    /// Instantiates the fireball at the hand's position and launches it.
    /// The firing direction is calculated from the vector pointing from the shoulder to the hand.
    /// </summary>
    /// <param name="hand">The hand (or fingertip) transform.</param>
    /// <param name="shoulder">The shoulder transform (used as a reference).</param>
    /// <param name="isRight">True for right arm; false for left arm.</param>
    void ShootFireball(Transform hand, Transform shoulder, bool isRight)
    {
        // Compute the direction from the shoulder to the hand.
        Vector3 direction = (hand.position - shoulder.position).normalized;
        
        // Instantiate the fireball at the hand's position.
        GameObject fireball = Instantiate(fireballPrefab, hand.position, Quaternion.identity);

        // Determine a random force between minFireballForce and maxFireballForce.
        float appliedForce = Random.Range(minFireballForce, maxFireballForce);
        Debug.Log($"Fireball force applied: {appliedForce}");

        // Get the FireballController component and launch the fireball.
        FireballController controller = fireball.GetComponent<FireballController>();
        if (controller != null)
        {
            controller.Launch(direction, appliedForce, shotDuration);
        }
        else
        {
            // Fallback: if no controller exists, apply the force directly.
            Rigidbody rb = fireball.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(direction * appliedForce, ForceMode.Impulse);
            }
        }
    }

    /// <summary>
    /// Coroutine that spawns water particles continuously from the hand for a burst duration.
    /// The water particles are launched in a small randomized cone and receive physics (gravity, force)
    /// similar to the fireball.
    /// The base firing direction is computed from the vector from the shoulder to the hand.
    /// </summary>
    /// <param name="hand">The hand (or fingertip) transform.</param>
    /// <param name="shoulder">The shoulder transform (used as a reference for orientation).</param>
    /// <param name="isRight">True for right arm; false for left arm.</param>
    IEnumerator ShootWaterStream(Transform hand, Transform shoulder, bool isRight)
    {
        float endTime = Time.time + waterBurstDuration;
        // Base direction computed from shoulder to hand.
        Vector3 baseDirection = (hand.position - shoulder.position).normalized;

        while (Time.time < endTime)
        {
            // Calculate a random direction within a cone of waterSpreadAngle degrees around baseDirection.
            Vector3 randomDirection = Vector3.RotateTowards(baseDirection, Random.onUnitSphere, waterSpreadAngle * Mathf.Deg2Rad, 0f);
            randomDirection.Normalize();

            // Instantiate a water particle at the hand's position.
            GameObject waterParticle = Instantiate(waterParticlePrefab, hand.position, Quaternion.identity);
            
            // Apply force to the water particle.
            Rigidbody rb = waterParticle.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(randomDirection * waterParticleForce, ForceMode.Impulse);
            }
            
            // The water particle should have its own collision logic to return to pool or self-destruct.
            
            // Wait before spawning the next particle.
            yield return new WaitForSeconds(1f / waterParticlesPerSecond);
        }
    }
}
