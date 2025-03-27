using UnityEngine;
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
        }

        // Update arc + rotation
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
}
