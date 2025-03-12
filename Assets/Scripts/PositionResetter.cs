using UnityEngine;

public class PositionResetter : MonoBehaviour
{
    [SerializeField] private MocapCameraPlacement _mocapCameraPlacement;
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private float _animDuration;
    private float _timeElapsed = 0;

    void Start()
    {
        // Store the initial position and rotation
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;

        // Get the animation duration from the MocapCameraPlacement script
        _animDuration = _mocapCameraPlacement.GetAnimDuration();
    }

    void Update()
    {
        CheckIfResetTransform();
        _timeElapsed += Time.deltaTime;
    }

    private void CheckIfResetTransform()
    {
        // Check if the elapsed time has reached or exceeded the animation duration
        if (_timeElapsed >= _animDuration)
        {
            // Reset the position and rotation to their initial values
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;

            // Reset the elapsed time
            _timeElapsed = 0;
        }
    }
}