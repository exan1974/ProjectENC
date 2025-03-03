using UnityEngine;
using UnityEditor;

namespace PatrolsManagement
{
    public class PositionPatrolGizmo : MonoBehaviour
    {
        private Texture2D _positionIcon;

        [SerializeField] private Vector3 _offset = Vector3.up * 5; // Visual offset for gizmo
        [SerializeField] private float _raycastDistance = 50f; // How far to check for the ground
        [SerializeField] private float _groundThreshold = 0.2f; // Allowed height difference from the ground

        private void OnDrawGizmos()
        {
            RaycastHit hit;
            bool isOnGround = Physics.Raycast(transform.position, Vector3.down, out hit, _raycastDistance, LayerMask.GetMask("Ground"));

            float distanceToGround = isOnGround ? Mathf.Abs(transform.position.y - hit.point.y) : float.MaxValue;

            // If within the ground threshold, it's correctly placed
            if (isOnGround && distanceToGround <= _groundThreshold)
            {
                Gizmos.color = Color.green;
                Handles.color = Color.green;
            }
            else
            {
                Gizmos.color = Color.red;
                Handles.color = Color.red;
            }

            Handles.DrawWireDisc(transform.position, Vector3.up, _offset.y);
            Gizmos.DrawSphere(transform.position + _offset, 0.5f);
            Gizmos.DrawLine(transform.position + _offset, transform.position);
        }

        private void Reset()
        {
            _positionIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Gizmos/Target.png");

#if UNITY_EDITOR
            if (_positionIcon != null)
            {
                EditorGUIUtility.SetIconForObject(gameObject, _positionIcon);
            }
#endif
        }
    }
}