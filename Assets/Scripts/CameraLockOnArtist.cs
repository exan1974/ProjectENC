using UnityEngine;

public class CameraLockOnArtist : MonoBehaviour
{
    [Header("Lock-On Settings")]
    [Tooltip("The artist to lock onto.")]
    public Transform artist;
    [Tooltip("Distance in front of the artist.")]
    public float distanceFromArtist = 3f;
    [Tooltip("Vertical offset from the artist's position.")]
    public float heightOffset = 1.5f;
    [Tooltip("Duration of the smooth transition (seconds).")]
    public float transitionDuration = 1f;

    private bool m_isLockedOn = false;
    private bool m_isTransitioning = false;
    private float m_transitionTimer = 0f;
    private Vector3 m_startPos;
    private Quaternion m_startRot;

    void Update()
    {
        if (artist == null) return;

        if (!m_isLockedOn && Input.GetKeyDown(KeyCode.Space))
        {
            StartTransition();
        }
    }

    void LateUpdate()
    {
        if (artist == null) return;

        Vector3 targetPos = artist.position + artist.forward * distanceFromArtist + Vector3.up * heightOffset;
        Quaternion targetRot = artist.rotation;

        if (m_isTransitioning)
        {
            m_transitionTimer += Time.deltaTime;
            float t = Mathf.Clamp01(m_transitionTimer / transitionDuration);
            t = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(m_startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(m_startRot, targetRot, t);
            if (t >= 1f)
            {
                m_isTransitioning = false;
                m_isLockedOn = true;
            }
        }
        else if (m_isLockedOn)
        {
            transform.position = targetPos;
            transform.rotation = targetRot;
        }
    }

    private void StartTransition()
    {
        m_isTransitioning = true;
        m_isLockedOn = false;
        m_transitionTimer = 0f;
        m_startPos = transform.position;
        m_startRot = transform.rotation;
    }
} 