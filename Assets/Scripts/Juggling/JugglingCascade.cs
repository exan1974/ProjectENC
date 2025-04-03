using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class JugglingCascade : MonoBehaviour
{
    [Header("Juggler Hands")]
    public Transform leftHand;
    public Transform rightHand;

    [Header("Ball & Club Settings")]
    public GameObject[] balls;
    public GameObject[] clubs;
    public bool useClubs = false;
    public float arcHeight = 2f;
    public float throwInterval = 0.6f;
    public float throwDuration = 1.2f;
    public float clubRotations = 2f; // Number of full spins per throw

    [Header("Excess Juggle Settings")]
    public bool excessJuggle = false;
    public GameObject excessBallPrefab;  // Ball prefab for excess juggling
    public float excessForce = 10f;        // Force applied when launching extra balls
    public float excessStartTime = 3f;     // Begin extra juggling after 3 seconds
    public Vector3 excessSpawnOffset = new Vector3(0f, 0.2f, 0f); // Slight upward offset
    public Transform cloudSpawnPoint;

    // Cloud prefab for vapor effect (make sure it has a Rigidbody and a collider)
    public GameObject cloudPrefab;

    private class ThrowObject
    {
        public GameObject obj;
        public float throwStartTime;
        public Transform fromHand;
        public Transform toHand;
        public bool inAir;
        public Quaternion initialRotation;
    }

    private List<ThrowObject> activeThrows = new();
    private float globalTime;
    private int objectIndex = 0;
    private bool isLeftThrowing = true;

    void Start()
    {
        GameObject[] set = useClubs ? clubs : balls;

        // Initial positions: 1 object per hand, 1 in air
        set[0].transform.position = leftHand.position;
        set[0].transform.parent = leftHand;

        set[1].transform.position = rightHand.position;
        set[1].transform.parent = rightHand;

        set[2].transform.position = leftHand.position;
        activeThrows.Add(new ThrowObject
        {
            obj = set[2],
            throwStartTime = 0f,
            fromHand = leftHand,
            toHand = rightHand,
            inAir = true,
            initialRotation = set[2].transform.rotation
        });
    }

    void SetInHand(GameObject obj, Transform hand)
    {
        obj.transform.parent = hand;
        obj.transform.position = hand.position;

        if (useClubs)
        {
            // Flip the club so the handle is down (adjust if needed)
            obj.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
        }
    }

    void Update()
    {
        globalTime += Time.deltaTime;
        GameObject[] set = useClubs ? clubs : balls;

        // Handle throw timing
        if (globalTime >= activeThrows.Count * throwInterval)
        {
            GameObject obj = set[objectIndex % set.Length];
            Transform from = isLeftThrowing ? leftHand : rightHand;
            Transform to = isLeftThrowing ? rightHand : leftHand;

            // Detach object to be thrown
            obj.transform.parent = null;

            activeThrows.Add(new ThrowObject
            {
                obj = obj,
                throwStartTime = globalTime,
                fromHand = from,
                toHand = to,
                inAir = true,
                initialRotation = obj.transform.rotation
            });

            // Toggle hand for next throw and update index
            isLeftThrowing = !isLeftThrowing;
            objectIndex++;

            // If excess juggling is enabled and we've passed the start delay, instantiate an extra ball.
            if (excessJuggle && globalTime >= excessStartTime && excessBallPrefab != null)
            {
                // Instantiate the extra ball at the current hand position with a slight upward offset.
                GameObject extraBall = Instantiate(excessBallPrefab, from.position + excessSpawnOffset, from.rotation);
                extraBall.transform.localScale = Vector3.one * 0.1f;

                Rigidbody rb = extraBall.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // Randomly choose one of two angle ranges for elevation.
                    float randomElevation;
                    if (Random.value < 0.5f)
                        randomElevation = Random.Range(75f, 85f);
                    else
                        randomElevation = Random.Range(95f, 105f);
                    
                    // Use a random azimuth for some horizontal variation.
                    float randomAzimuth = Random.Range(0f, 360f);
                    float elevRad = randomElevation * Mathf.Deg2Rad;
                    float azimuthRad = randomAzimuth * Mathf.Deg2Rad;

                    // Convert spherical coordinates to a Cartesian direction vector.
                    Vector3 launchDirection = new Vector3(
                        Mathf.Cos(elevRad) * Mathf.Cos(azimuthRad),
                        Mathf.Sin(elevRad),
                        Mathf.Cos(elevRad) * Mathf.Sin(azimuthRad)
                    );
                    rb.AddForce(launchDirection.normalized * excessForce, ForceMode.Impulse);
                }

                // Start a coroutine to grow the ball from 0.1 to 0.4 after 0.3 seconds.
                StartCoroutine(GrowBall(extraBall));

                // Also, instantiate a cloud that has vapor behavior.
                SpawnCloud(cloudSpawnPoint.position, cloudSpawnPoint.rotation);
            }
        }

        // Update arc and rotation for active throws
        foreach (var throwObj in activeThrows)
        {
            float t = (globalTime - throwObj.throwStartTime) / throwDuration;

            if (t < 1f)
            {
                Vector3 pos = GetParabolicPoint(
                    throwObj.fromHand.position,
                    throwObj.toHand.position,
                    arcHeight,
                    t
                );
                throwObj.obj.transform.position = pos;

                if (useClubs)
                {
                    float angle = 360f * clubRotations * t;
                    throwObj.obj.transform.rotation = throwObj.initialRotation * Quaternion.Euler(angle, 0f, 0f);
                }
            }
            else if (throwObj.inAir)
            {
                throwObj.obj.transform.position = throwObj.toHand.position;
                throwObj.obj.transform.parent = throwObj.toHand;

                if (useClubs)
                    throwObj.obj.transform.rotation = throwObj.initialRotation;

                throwObj.inAir = false;
            }
        }
    }

    Vector3 GetParabolicPoint(Vector3 start, Vector3 end, float height, float t)
    {
        Vector3 pos = Vector3.Lerp(start, end, t);
        float arc = 4 * height * t * (1 - t);
        pos.y += arc;
        return pos;
    }

    IEnumerator GrowBall(GameObject ball)
    {
        // Wait for 0.3 seconds before starting to grow
        yield return new WaitForSeconds(0.3f);
        float duration = 0.2f;
        float elapsed = 0f;
        Vector3 initialScale = Vector3.one * 0.1f;
        Vector3 targetScale = Vector3.one * 0.4f;

        while (elapsed < duration)
        {
            ball.transform.localScale = Vector3.Lerp(initialScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        ball.transform.localScale = targetScale;
    }

    // New function to instantiate a cloud that drifts upward in a vapor-like manner.
    void SpawnCloud(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (cloudPrefab == null)
            return;

        // Instantiate the cloud prefab at the specified position.
        GameObject cloud = Instantiate(cloudPrefab, spawnPosition, spawnRotation);
        cloud.transform.localScale = Vector3.one * 0.4f; // Set initial scale if needed.

        Rigidbody cloudRb = cloud.GetComponent<Rigidbody>();
        if (cloudRb != null)
        {
            // Disable gravity so the cloud is carried by the wind.
            cloudRb.useGravity = false;

            // Create a force vector that is mostly upward with some horizontal drift.
            // Adjust the multipliers as necessary for your desired vapor effect.
            float horizontalDrift = excessForce * 0.5f;
            float upwardForce = excessForce * 0.8f;
            float randomDriftAngle = Random.Range(0f, 360f);
            Vector3 driftDirection = new Vector3(Mathf.Cos(randomDriftAngle * Mathf.Deg2Rad), 0f, Mathf.Sin(randomDriftAngle * Mathf.Deg2Rad));
            Vector3 vaporForce = new Vector3(driftDirection.x * horizontalDrift, upwardForce, driftDirection.z * horizontalDrift);
            cloudRb.AddForce(vaporForce, ForceMode.Impulse);
        }
    }
}
