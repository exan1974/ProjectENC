using UnityEngine;

public class AnimationSelector : MonoBehaviour
{
    private Animator m_CharAnimator;
    [SerializeField] private SelectedAnimation m_SelectedAnimation;
    
    void Awake()
    {
        m_CharAnimator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Start()
    {
        SetAnimation();
    }

        private void SetAnimation()
    {
        if (m_CharAnimator != null)
        {
            m_CharAnimator.SetInteger("AnimIndex", (int)m_SelectedAnimation);
        }
        else
        {
            Debug.LogError("Animator component is not attached.");
        }
    }
}
