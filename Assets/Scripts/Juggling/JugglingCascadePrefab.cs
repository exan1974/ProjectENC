using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class JugglingCascadePrefab : MonoBehaviour
{
    [Header("Juggler Hands")]
    public Transform leftHand;
    public Transform rightHand;

    [Header("Ball & Club Settings")]
    public bool useClubs = false;
    public GameObject ballPrefab;          // Prefab for balls
    public GameObject[] clubs;             // Assign in inspector if using clubs
    private GameObject[] balls;            // Instantiated balls

    public float arcHeight = 2f;
    public float throwInterval = 0.6f;
    public float throwDuration = 1.2f;
    public float clubRotations = 2f;

    [Header("Excess Juggle Settings")]
    public bool excessJuggle = false;
    public GameObject excessBallPrefab;
    public float excessForce = 10f;
    public float excessStartTime = 3f;
    public Vector3 excessSpawnOffset = new Vector3(0f, 0.2f, 0f);
    public Transform cloudSpawnPoint;
    public GameObject cloudPrefab;
    public Animator animator;

    private class ThrowObject
    {
        public GameObject obj;
        public float throwStartTime;
        public Transform fromHand;
        public Transform toHand;
        public bool inAir;
        public Quaternion initialRotation;
    }

    private List<ThrowObject> activeThrows = new List<ThrowObject>();
    private float globalTime;
    private int objectIndex = 0;
    private bool isLeftThrowing = true;

    void Start()
    {
        // Instantiate 3 balls as children of this GameObject
        if (!useClubs && ballPrefab != null)
        {
            balls = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                // Use overload to parent without preserving world transform
                GameObject go = Instantiate(ballPrefab, transform, false);
                go.name = ballPrefab.name + "_" + i;
                balls[i] = go;
            }
        }

        // Set initial animation index if needed
        if (animator != null)
            animator.SetInteger("AnimIndex", 8);

        // Set up initial juggling state
        GameObject[] set = useClubs ? clubs : balls;
        if (set == null || set.Length < 3)
        {
            Debug.LogError("Need at least 3 objects to juggle.");
            return;
        }

        // One in each hand and one in air
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

    void Update()
    {
        globalTime += Time.deltaTime;
        GameObject[] set = useClubs ? clubs : balls;

        // Throw timing
        if (globalTime >= activeThrows.Count * throwInterval)
        {
            GameObject obj = set[objectIndex % set.Length];
            Transform from = isLeftThrowing ? leftHand : rightHand;
            Transform to = isLeftThrowing ? rightHand : leftHand;
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

            isLeftThrowing = !isLeftThrowing;
            objectIndex++;

            // Excess juggle logic
            if (excessJuggle && globalTime >= excessStartTime && excessBallPrefab != null)
            {
                SpawnExcessBall(from);
                SpawnCloud(cloudSpawnPoint.position, cloudSpawnPoint.rotation);
            }
        }

        // Update throws
        foreach (var t in activeThrows)
        {
            float tNorm = (globalTime - t.throwStartTime) / throwDuration;
            if (tNorm < 1f)
            {
                Vector3 pos = GetParabolicPoint(t.fromHand.position, t.toHand.position, arcHeight, tNorm);
                t.obj.transform.position = pos;
                if (useClubs)
                    t.obj.transform.rotation = t.initialRotation * Quaternion.Euler(360f * clubRotations * tNorm, 0, 0);
            }
            else if (t.inAir)
            {
                t.obj.transform.position = t.toHand.position;
                t.obj.transform.parent = t.toHand;
                if (useClubs)
                    t.obj.transform.rotation = t.initialRotation;
                t.inAir = false;
            }
        }
    }

    private void SpawnExcessBall(Transform hand)
    {
        GameObject extra = Instantiate(excessBallPrefab, hand.position + excessSpawnOffset, hand.rotation);
        extra.transform.localScale = Vector3.one * 0.1f;
        Rigidbody rb = extra.GetComponent<Rigidbody>();
        if (rb != null)
        {
            float elev = Random.value < 0.5f ? Random.Range(75f, 85f) : Random.Range(95f, 105f);
            float az = Random.Range(0f, 360f);
            float er = elev * Mathf.Deg2Rad;
            float ar = az * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(er) * Mathf.Cos(ar), Mathf.Sin(er), Mathf.Cos(er) * Mathf.Sin(ar));
            rb.AddForce(dir.normalized * excessForce, ForceMode.Impulse);
        }
        StartCoroutine(GrowBall(extra));
    }

    IEnumerator GrowBall(GameObject ball)
    {
        yield return new WaitForSeconds(0.3f);
        float dur = 0.2f, el = 0f;
        Vector3 start = Vector3.one * 0.1f, end = Vector3.one * 0.4f;
        while (el < dur)
        {
            ball.transform.localScale = Vector3.Lerp(start, end, el / dur);
            el += Time.deltaTime;
            yield return null;
        }
        ball.transform.localScale = end;
    }

    private void SpawnCloud(Vector3 pos, Quaternion rot)
    {
        if (cloudPrefab == null) return;
        GameObject cloud = Instantiate(cloudPrefab, pos, rot);
        cloud.transform.localScale = Vector3.one * 0.4f;
        Rigidbody rb = cloud.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            float drift = excessForce * 0.5f;
            float up = excessForce * 0.8f;
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            rb.AddForce(new Vector3(dir.x * drift, up, dir.z * drift), ForceMode.Impulse);
        }
    }

    Vector3 GetParabolicPoint(Vector3 start, Vector3 end, float height, float t)
    {
        Vector3 p = Vector3.Lerp(start, end, t);
        p.y += 4 * height * t * (1 - t);
        return p;
    }
}
