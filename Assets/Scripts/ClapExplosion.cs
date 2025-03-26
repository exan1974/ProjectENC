using UnityEngine;

public class CharacterEffects : MonoBehaviour
{
    [Header("Clap Explosion Settings")]
    [Tooltip("Reference to the left hand transform.")]
    [SerializeField] private Transform leftHand;
    [Tooltip("Reference to the right hand transform.")]
    [SerializeField] private Transform rightHand;
    [Tooltip("Explosion particle effect prefab to instantiate on a hand clap.")]
    [SerializeField] private GameObject explosionPrefab;
    [Tooltip("Distance threshold (in units) to detect a hand clap.")]
    [SerializeField] private float clapThreshold = 0.2f;
    [Tooltip("Cooldown time (in seconds) to prevent repeated explosions.")]
    [SerializeField] private float explosionCooldown = 1f;
    private float explosionCooldownTimer = 0f;

    [Header("Arm Extension Effect Settings")]
    [Tooltip("Reference to the left shoulder transform.")]
    [SerializeField] private Transform leftShoulder;
    [Tooltip("Reference to the left elbow transform.")]
    [SerializeField] private Transform leftElbow;
    [Tooltip("Reference to the right shoulder transform.")]
    [SerializeField] private Transform rightShoulder;
    [Tooltip("Reference to the right elbow transform.")]
    [SerializeField] private Transform rightElbow;
    [Tooltip("Particle effect prefab to instantiate when an arm is fully stretched.")]
    [SerializeField] private GameObject armParticlePrefab;
    [Tooltip("Angle threshold (in degrees) for considering an arm as fully stretched (close to 180°).")]
    [SerializeField] private float armExtensionAngleThreshold = 165f;

    [Header("Stomach Reference")]
    [Tooltip("Reference to the stomach transform to check if the hand is above the stomach.")]
    [SerializeField] private Transform stomach;

    // Flags to ensure the arm particle effect is fired only once per extension
    private bool leftArmTriggered = false;
    private bool rightArmTriggered = false;

    void Update()
    {
        HandleClapExplosion();
        HandleArmExtension();
    }

    void HandleClapExplosion()
    {
        if (explosionCooldownTimer > 0f)
        {
            explosionCooldownTimer -= Time.deltaTime;
        }

        if (leftHand == null || rightHand == null)
        {
            Debug.LogWarning("Left or Right hand transforms are not assigned!");
            return;
        }

        float handDistance = Vector3.Distance(leftHand.position, rightHand.position);
        if (handDistance < clapThreshold && explosionCooldownTimer <= 0f)
        {
            // Instantiate the explosion effect at the midpoint between the hands.
            Vector3 explosionPosition = (leftHand.position + rightHand.position) / 2f;
            Instantiate(explosionPrefab, explosionPosition, Quaternion.identity);
            explosionCooldownTimer = explosionCooldown;
        }
    }

    void HandleArmExtension()
    {
        // Process left arm extension.
        if (leftShoulder != null && leftElbow != null && leftHand != null && stomach != null)
        {
            float leftAngle = Vector3.Angle(leftShoulder.position - leftElbow.position, leftHand.position - leftElbow.position);
            bool isLeftHandAboveStomach = leftHand.position.y > stomach.position.y;
            if (leftAngle >= armExtensionAngleThreshold && isLeftHandAboveStomach)
            {
                if (!leftArmTriggered)
                {
                    ShootArmParticle(leftHand, false);
                    leftArmTriggered = true;
                }
            }
            else
            {
                leftArmTriggered = false;
            }
        }
        else
        {
            Debug.LogWarning("Left arm transforms (shoulder, elbow, hand) or stomach are not assigned!");
        }

        // Process right arm extension.
        if (rightShoulder != null && rightElbow != null && rightHand != null && stomach != null)
        {
            float rightAngle = Vector3.Angle(rightShoulder.position - rightElbow.position, rightHand.position - rightElbow.position);
            bool isRightHandAboveStomach = rightHand.position.y > stomach.position.y;
            if (rightAngle >= armExtensionAngleThreshold && isRightHandAboveStomach)
            {
                if (!rightArmTriggered)
                {
                    ShootArmParticle(rightHand, true);
                    rightArmTriggered = true;
                }
            }
            else
            {
                rightArmTriggered = false;
            }
        }
        else
        {
            Debug.LogWarning("Right arm transforms (shoulder, elbow, hand) or stomach are not assigned!");
        }
    }

    /// <summary>
    /// Instantiates the arm particle effect and adjusts its ShapeModule rotation to emit left/right.
    /// </summary>
    void ShootArmParticle(Transform hand, bool isRight)
    {
        if (armParticlePrefab == null)
        {
            Debug.LogWarning("Arm particle prefab is not assigned!");
            return;
        }

        // Instantiate the particle system
        GameObject particleObj = Instantiate(armParticlePrefab, hand.position, Quaternion.identity);
        ParticleSystem particleSystem = particleObj.GetComponent<ParticleSystem>();

        if (particleSystem != null)
        {
            // Adjust the ShapeModule rotation to emit left/right
            var shapeModule = particleSystem.shape;
            shapeModule.rotation = new Vector3(0f, isRight ? 90f : -90f, 0f); // Right: 90°, Left: -90°
        }
        else
        {
            Debug.LogWarning("ParticleSystem component missing on armParticlePrefab!");
        }
    }
}