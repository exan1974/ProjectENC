using System.Collections.Generic;
using UnityEngine;

public class CameraCoverageCalculator
{
    private LayerMask raycastLayerMask;
    private float fovAngle;
    private float maxViewDistance;

    public CameraCoverageCalculator(LayerMask layerMask, float fovAngle, float maxViewDistance)
    {
        this.raycastLayerMask = layerMask;
        this.fovAngle = fovAngle;
        this.maxViewDistance = maxViewDistance;
    }

    public float CalculateCoverageScore(List<Vector3> positions, List<Quaternion> rotations, List<Transform> trackingPoints)
    {
        if (positions == null || rotations == null || trackingPoints == null)
        {
            Debug.LogWarning("One or more input lists are null.");
            return 0f;
        }

        int visibleJoints = 0;
        foreach (var joint in trackingPoints)
        {
            if (joint == null) continue;
            for (int i = 0; i < positions.Count; i++)
            {
                if (IsPointInFOV(positions[i], rotations[i], joint.position) &&
                    IsJointVisible(positions[i], joint.position, joint))
                {
                    visibleJoints++;
                    break;
                }
            }
        }
        return (float)visibleJoints / trackingPoints.Count;
    }

    public bool IsJointVisible(Vector3 camPos, Vector3 jointPos, Transform joint)
    {
        Vector3 direction = (jointPos - camPos).normalized;
        float distance = Vector3.Distance(camPos, jointPos);
        RaycastHit hit;
        if (Physics.Raycast(camPos, direction, out hit, distance, raycastLayerMask))
        {
            return hit.transform == joint;
        }
        return true;
    }

    public bool IsPointInFOV(Vector3 camPos, Quaternion camRot, Vector3 point)
    {
        Vector3 toPoint = (point - camPos).normalized;
        Vector3 forward = camRot * Vector3.forward;
        float halfFOV = fovAngle / 2f;
        float verticalFOV = halfFOV * (9f / 16f);
        return (Vector3.Angle(forward, toPoint) <= halfFOV &&
                Vector3.Angle(forward, toPoint) <= verticalFOV);
    }

    // Candidate scoring: note the parameter order!
    public float CalculateCandidateScore(
        int cameraIndex,
        List<Vector3> allPositions,
        List<Quaternion> allRotations,
        List<Transform> trackingPoints,
        int[] trackingCoverage,
        float zoneCenter,
        float zoneHalfAngle)
    {
        float coverageScore = 0f;
        for (int i = 0; i < trackingPoints.Count; i++)
        {
            Transform joint = trackingPoints[i];
            if (joint == null) continue;
            if (IsPointInFOV(allPositions[cameraIndex], allRotations[cameraIndex], joint.position) &&
                IsJointVisible(allPositions[cameraIndex], joint.position, joint))
            {
                coverageScore += (trackingCoverage[i] == 0 ? 2f : 1f);
            }
        }
        // Compute candidate angle relative to world origin (adjust as needed)
        float candidateAngle = Mathf.Atan2(allPositions[cameraIndex].z - 0f, allPositions[cameraIndex].x - 0f) * Mathf.Rad2Deg;
        if (candidateAngle < 0) candidateAngle += 360f;
        float angleDiff = Mathf.Abs(candidateAngle - zoneCenter);
        if (angleDiff > 180f) angleDiff = 360f - angleDiff;
        float positionWeight = Mathf.Clamp01(1f - (angleDiff / zoneHalfAngle));
        return coverageScore * positionWeight;
    }
}
