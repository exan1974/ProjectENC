using UnityEngine;

public class JugglingSimulator : MonoBehaviour
{
    [Header("Juggler Bones")]
    public Transform leftHand;
    public Transform rightHand;

    [Header("Ball Settings")]
    public GameObject ballPrefab;
    public float throwForce = 5f;
    public float throwHeight = 2.5f;
    public float interval = 0.7f;

    private float timer;
    private bool isLeftNext = true;

    void Start()
    {
        timer = interval;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            ThrowBall(isLeftNext ? leftHand : rightHand, isLeftNext ? rightHand.position : leftHand.position);
            isLeftNext = !isLeftNext;
            timer = interval;
        }
    }

    void ThrowBall(Transform fromHand, Vector3 targetPosition)
    {
        GameObject ball = Instantiate(ballPrefab, fromHand.position, Quaternion.identity);
        Rigidbody rb = ball.GetComponent<Rigidbody>();

        Vector3 velocity = CalculateThrowVelocity(fromHand.position, targetPosition, throwHeight);
        rb.linearVelocity = velocity;
    }

    Vector3 CalculateThrowVelocity(Vector3 start, Vector3 end, float height)
    {
        float gravity = Physics.gravity.y;
        Vector3 displacementXZ = new Vector3(end.x - start.x, 0f, end.z - start.z);

        float timeUp = Mathf.Sqrt(-2 * height / gravity);
        float timeDown = Mathf.Sqrt(2 * (end.y - (start.y + height)) / gravity);
        float totalTime = timeUp + timeDown;

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * height);
        Vector3 velocityXZ = displacementXZ / totalTime;

        return velocityXZ + velocityY;
    }
}
