using UnityEngine;

public class FixedYPosition : MonoBehaviour
{
    private float m_initialY;

    void Start()
    {
        m_initialY = transform.position.y;
    }

    void LateUpdate()
    {
        Vector3 pos = transform.position;
        pos.y = m_initialY;
        transform.position = pos;
    }
} 