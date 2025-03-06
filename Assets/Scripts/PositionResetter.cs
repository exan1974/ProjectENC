using UnityEngine;

public class PositionResetter : MonoBehaviour
{
    [SerializeField] MocapCameraPlacement _mocapCameraPlacement;
    private Vector3 _position;
    private float _animDuration;
    private float _timeElapsed = 0;

    void Start()
    {
        _position = transform.position;
        _animDuration = _mocapCameraPlacement.GetAnimDuration();
    }
    
    void Update()
    {
        CheckIfResetTransform();
        _timeElapsed += Time.deltaTime;
    }

    private void CheckIfResetTransform()
    {
        if (_timeElapsed >= _animDuration)
        {
            transform.position = _position;
            _timeElapsed = 0;
        }
    }
}