using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[ExecuteInEditMode]
public class MocapCameraPlacement : MonoBehaviour
{
    [Header("Camera Placement Settings")]
    [SerializeField] private int numberOfCameras = 8;
    [SerializeField] private float placementRadius = 5f;
    [SerializeField] private float minHeight = 1f;
    [SerializeField] private float maxHeight = 3f;
    [SerializeField] private float minTiltAngle = 10f;
    [SerializeField] private float maxTiltAngle = 45f;

    [Header("Tracking Points (Joints for Raycasts)")]
    [SerializeField] private List<Transform> trackingPoints = new List<Transform>();

    [Header("Camera Positions (Editable in Inspector)")]
    [SerializeField] private List<Vector3> cameraPositions = new List<Vector3>();

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            GenerateCameraPositions();
        }
    }

    private void GenerateCameraPositions()
    {
        if (cameraPositions == null)
            cameraPositions = new List<Vector3>();

        cameraPositions.Clear();
        float angleStep = 360f / Mathf.Max(1, numberOfCameras);

        for (int i = 0; i < numberOfCameras; i++)
        {
            float angle = i * angleStep;
            float radians = angle * Mathf.Deg2Rad;

            float x = Mathf.Cos(radians) * placementRadius;
            float z = Mathf.Sin(radians) * placementRadius;
            float y = Random.Range(minHeight, maxHeight);

            Vector3 cameraPosition = transform.position + new Vector3(x, y, z);
            cameraPositions.Add(cameraPosition);
        }
    }

    private void OnDrawGizmos()
    {
        if (cameraPositions == null || trackingPoints == null) return;

        // **Draw camera placements**
        Gizmos.color = Color.blue;
        foreach (var camPos in cameraPositions)
        {
            Vector3 groundPos = new Vector3(camPos.x, transform.position.y, camPos.z);
            Gizmos.DrawLine(groundPos, camPos);
            Gizmos.DrawSphere(camPos, 0.2f);
        }

        // **Raycast from cameras to tracking points**
        foreach (var joint in trackingPoints)
        {
            if (joint == null) continue;

            foreach (var camPos in cameraPositions)
            {
                Vector3 direction = (joint.position - camPos).normalized;
                float distance = Vector3.Distance(camPos, joint.position);
                
                // Cast all hits along the ray
                RaycastHit[] hits = Physics.RaycastAll(camPos, direction, distance);

                // Sort hits by distance (important!)
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                bool isVisible = false;
                
                // Check second hit (skip the first hit assuming it's the character)
                if (hits.Length > 1)
                {
                    if (hits[1].transform == joint)
                    {
                        isVisible = true; // Second hit is the tracking point = Visible
                    }
                }
                else if (hits.Length == 1) 
                {
                    // If there's only one hit and it's the tracking point itself
                    if (hits[0].transform == joint)
                    {
                        isVisible = true;
                    }
                }

                // Draw visibility lines
                Gizmos.color = isVisible ? Color.green : Color.red;
                Gizmos.DrawLine(camPos, joint.position);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            GenerateCameraPositions();
        }
    }
#endif
}
