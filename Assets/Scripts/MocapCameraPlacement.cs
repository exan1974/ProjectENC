using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[ExecuteInEditMode]
public class MocapCameraPlacement : MonoBehaviour
{
    [Header("Camera Placement Settings")]
    [SerializeField] private int numberOfCameras = 3;
    [SerializeField] private float placementRadius = 5f;
    [SerializeField] private float minHeight = 1f;
    [SerializeField] private float maxHeight = 3f;
    [SerializeField] private float fovAngle = 60f;   // Horizontal FOV
    [SerializeField] private float maxViewDistance = 10f; // Maximum viewable range

    [Header("Tracking Points (Joints for Raycasts)")]
    [SerializeField] private List<Transform> trackingPoints = new List<Transform>();

    [Header("Raycast Settings")]
    [SerializeField] private LayerMask raycastLayerMask; // Expose layer mask in the editor
    [SerializeField] private float raycastFPS = 30f; // Frames per second for raycast updates

    [Header("Grid Settings")]
    [SerializeField] private float gridSpacing = 0.3f; // Distance between grid points (30cm)

    [Header("Animation Settings")]
    [SerializeField] private AnimationClip animationClip; // Assign the animation clip here
    private float _animationDuration; // Duration of the animation clip
    private float _elapsedTime; // Time elapsed since the animation started

    private List<Vector3> _allPossiblePositions = new List<Vector3>();
    private List<Quaternion> _allPossibleRotations = new List<Quaternion>();
    private List<float> _accumulatedScores = new List<float>();

    // Definitive best cameras (set at the end of the animation)
    private List<Vector3> _selectedCameraPositions = new List<Vector3>();
    private List<Quaternion> _selectedCameraRotations = new List<Quaternion>();

    // Temporary best cameras (updated at the raycast FPS interval)
    private List<Vector3> _tempCameraPositions = new List<Vector3>();
    private List<Quaternion> _tempCameraRotations = new List<Quaternion>();

    private bool _isAnimationPlaying = false;
    private float _raycastTimer = 0f;

    private void Awake()
    {
        // Get the duration of the animation clip
        if (animationClip != null)
        {
            _animationDuration = animationClip.length / 5;
            Debug.Log($"Animation duration: {_animationDuration} seconds");
        }
        else
        {
            Debug.LogError("Animation clip is not assigned.");
            return;
        }
    }

    private void Start()
    {
        Debug.Log("Start called");

        GenerateAllPossibleCameraPositions();
        ResetAccumulatedScores();
        _isAnimationPlaying = true;
    }

    private void Update()
    {
        if (_isAnimationPlaying)
        {
            _elapsedTime += Time.deltaTime;
            _raycastTimer += Time.deltaTime;

            // Update temporary best cameras at the interval set by raycastFPS
            if (_raycastTimer >= 1f / raycastFPS)
            {
                SelectTemporaryCameras();
                _raycastTimer = 0f;
            }

            // Accumulate scores for each possible camera position every frame.
            CalculateScoresForCurrentFrame();

            // Check if the animation duration has been reached
            if (_elapsedTime >= _animationDuration)
            {
                OnAnimationLoop();
                _elapsedTime = 0f;
                // Stop updating temporary selection now that we have the definitive best cameras.
                _isAnimationPlaying = false;
            }
        }
    }

    private void GenerateAllPossibleCameraPositions()
    {
        Debug.Log("Generating all possible camera positions...");

        _allPossiblePositions.Clear();
        _allPossibleRotations.Clear();
        _accumulatedScores.Clear();

        // Calculate the number of grid points in height and angle.
        int heightSteps = Mathf.FloorToInt((maxHeight - minHeight) / gridSpacing);
        int angleSteps = Mathf.FloorToInt(360f / (gridSpacing / placementRadius * Mathf.Rad2Deg));

        for (int h = 0; h < heightSteps; h++)
        {
            float y = minHeight + h * gridSpacing;

            for (int a = 0; a < angleSteps; a++)
            {
                float angle = a * (360f / angleSteps) * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * placementRadius;
                float z = Mathf.Sin(angle) * placementRadius;

                Vector3 position = transform.position + new Vector3(x, y, z);
                Quaternion rotation = Quaternion.LookRotation(transform.position - position, Vector3.up);

                _allPossiblePositions.Add(position);
                _allPossibleRotations.Add(rotation);
            }
        }

        // Initialize accumulated scores for each possible position
        _accumulatedScores = new List<float>(new float[_allPossiblePositions.Count]);

        Debug.Log($"Generated {_allPossiblePositions.Count} possible camera positions.");
    }

    private void ResetAccumulatedScores()
    {
        _accumulatedScores = new List<float>(new float[_allPossiblePositions.Count]);
    }

    private void CalculateScoresForCurrentFrame()
    {
        for (int i = 0; i < _allPossiblePositions.Count; i++)
        {
            float score = CalculateCoverageScore(new List<Vector3> { _allPossiblePositions[i] }, new List<Quaternion> { _allPossibleRotations[i] });
            _accumulatedScores[i] += score;
        }
    }

    /// <summary>
    /// Called when the animation ends. Computes the definitive best cameras and resets scores.
    /// </summary>
    private void OnAnimationLoop()
    {
        Debug.Log("Animation looped. Selecting definitive best cameras...");

        SelectDefinitiveCameras();

        // Reset accumulated scores for next animation loop if desired.
        ResetAccumulatedScores();
    }

    /// <summary>
    /// Selects the definitive best cameras based on accumulated scores.
    /// </summary>
    private void SelectDefinitiveCameras()
    {
        _selectedCameraPositions.Clear();
        _selectedCameraRotations.Clear();

        // Create a list of indices sorted by accumulated score (descending) for fallback.
        var sortedIndices = _accumulatedScores
            .Select((score, index) => new { Score = score, Index = index })
            .OrderByDescending(x => x.Score)
            .Select(x => x.Index)
            .ToList();

        // Local array to track coverage counts for each tracking point.
        int[] trackingCoverage = new int[trackingPoints.Count];
        List<Vector3> selectedPositions = new List<Vector3>();

        // Divide the circle into zones.
        float angleStep = 360f / numberOfCameras;
        float halfZoneAngle = angleStep / 2f;

        for (int zone = 0; zone < Mathf.Min(numberOfCameras, _allPossiblePositions.Count); zone++)
        {
            int bestCandidateIndex = -1;
            float bestCandidateScore = -Mathf.Infinity;
            float zoneStartAngle = zone * angleStep;
            float zoneEndAngle = (zone + 1) * angleStep;
            float zoneCenter = zoneStartAngle + halfZoneAngle;

            // Find the best candidate in this zone.
            for (int i = 0; i < _allPossiblePositions.Count; i++)
            {
                Vector3 posRelative = _allPossiblePositions[i] - transform.position;
                float candidateAngle = Mathf.Atan2(posRelative.z, posRelative.x) * Mathf.Rad2Deg;
                if (candidateAngle < 0) candidateAngle += 360f;
                if (candidateAngle < zoneStartAngle || candidateAngle >= zoneEndAngle)
                    continue;
                // Ensure spatial distribution: candidate must be far enough from already selected positions.
                bool isTooClose = selectedPositions.Any(selectedPos => Vector3.Distance(selectedPos, _allPossiblePositions[i]) < placementRadius / (numberOfCameras * 2f));
                if (isTooClose)
                    continue;

                float candidateScore = CalculateCandidateScore(i, trackingCoverage, zoneCenter, halfZoneAngle);
                if (candidateScore > bestCandidateScore)
                {
                    bestCandidateScore = candidateScore;
                    bestCandidateIndex = i;
                }
            }

            // If no candidate was found in this zone, fallback to the best overall candidate.
            if (bestCandidateIndex == -1)
            {
                Debug.LogWarning($"No suitable candidate found in zone {zone}. Falling back to best overall candidate.");
                bestCandidateIndex = sortedIndices[0];
            }

            if (bestCandidateIndex != -1)
            {
                _selectedCameraPositions.Add(_allPossiblePositions[bestCandidateIndex]);
                _selectedCameraRotations.Add(_allPossibleRotations[bestCandidateIndex]);
                selectedPositions.Add(_allPossiblePositions[bestCandidateIndex]);

                // Update tracking coverage.
                for (int t = 0; t < trackingPoints.Count; t++)
                {
                    if (trackingPoints[t] == null) continue;
                    if (IsPointInFOV(_allPossiblePositions[bestCandidateIndex], _allPossibleRotations[bestCandidateIndex], trackingPoints[t].position) &&
                        IsJointVisible(_allPossiblePositions[bestCandidateIndex], trackingPoints[t].position, trackingPoints[t]))
                    {
                        trackingCoverage[t]++;
                    }
                }

                Debug.Log($"Definitive selected Camera {bestCandidateIndex} at Position: {_allPossiblePositions[bestCandidateIndex]} in zone {zone}");
            }
        }

        Debug.Log($"Definitively selected {_selectedCameraPositions.Count} best cameras.");
    }

    /// <summary>
    /// Selects temporary best cameras (used as a preview) based on the current accumulated scores.
    /// </summary>
    private void SelectTemporaryCameras()
    {
        _tempCameraPositions.Clear();
        _tempCameraRotations.Clear();

        int[] trackingCoverage = new int[trackingPoints.Count];
        List<Vector3> selectedPositions = new List<Vector3>();

        float angleStep = 360f / numberOfCameras;
        float halfZoneAngle = angleStep / 2f;

        for (int zone = 0; zone < Mathf.Min(numberOfCameras, _allPossiblePositions.Count); zone++)
        {
            int bestCandidateIndex = -1;
            float bestCandidateScore = -Mathf.Infinity;
            float zoneStartAngle = zone * angleStep;
            float zoneEndAngle = (zone + 1) * angleStep;
            float zoneCenter = zoneStartAngle + halfZoneAngle;

            for (int i = 0; i < _allPossiblePositions.Count; i++)
            {
                Vector3 posRelative = _allPossiblePositions[i] - transform.position;
                float candidateAngle = Mathf.Atan2(posRelative.z, posRelative.x) * Mathf.Rad2Deg;
                if (candidateAngle < 0) candidateAngle += 360f;
                if (candidateAngle < zoneStartAngle || candidateAngle >= zoneEndAngle)
                    continue;
                bool tooClose = selectedPositions.Any(selectedPos => Vector3.Distance(selectedPos, _allPossiblePositions[i]) < placementRadius / (numberOfCameras * 2f));
                if (tooClose)
                    continue;

                float candidateScore = CalculateCandidateScore(i, trackingCoverage, zoneCenter, halfZoneAngle);
                if (candidateScore > bestCandidateScore)
                {
                    bestCandidateScore = candidateScore;
                    bestCandidateIndex = i;
                }
            }

            if (bestCandidateIndex == -1)
            {
                // Fallback to best overall candidate.
                bestCandidateIndex = _accumulatedScores
                    .Select((score, index) => new { Score = score, Index = index })
                    .OrderByDescending(x => x.Score)
                    .FirstOrDefault()?.Index ?? 0;
            }

            if (bestCandidateIndex != -1)
            {
                _tempCameraPositions.Add(_allPossiblePositions[bestCandidateIndex]);
                _tempCameraRotations.Add(_allPossibleRotations[bestCandidateIndex]);
                selectedPositions.Add(_allPossiblePositions[bestCandidateIndex]);

                // Update tracking coverage for temporary selection.
                for (int t = 0; t < trackingPoints.Count; t++)
                {
                    if (trackingPoints[t] == null) continue;
                    if (IsPointInFOV(_allPossiblePositions[bestCandidateIndex], _allPossibleRotations[bestCandidateIndex], trackingPoints[t].position) &&
                        IsJointVisible(_allPossiblePositions[bestCandidateIndex], trackingPoints[t].position, trackingPoints[t]))
                    {
                        trackingCoverage[t]++;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Calculates a score for a candidate camera at index `cameraIndex` given the current tracking coverage.
    /// For each tracking point seen by the candidate:
    ///   - If not yet covered (blind spot), add a bonus of 2.
    ///   - If already covered, add a bonus of 1.
    /// Then multiplies by a positional weight (highest at the center of the zone).
    /// </summary>
    private float CalculateCandidateScore(int cameraIndex, int[] trackingCoverage, float zoneCenter, float zoneHalfAngle)
    {
        float coverageScore = 0f;

        for (int i = 0; i < trackingPoints.Count; i++)
        {
            Transform joint = trackingPoints[i];
            if (joint == null) continue;
            if (IsPointInFOV(_allPossiblePositions[cameraIndex], _allPossibleRotations[cameraIndex], joint.position) &&
                IsJointVisible(_allPossiblePositions[cameraIndex], joint.position, joint))
            {
                coverageScore += (trackingCoverage[i] == 0 ? 2f : 1f);
            }
        }

        Vector3 posRelative = _allPossiblePositions[cameraIndex] - transform.position;
        float candidateAngle = Mathf.Atan2(posRelative.z, posRelative.x) * Mathf.Rad2Deg;
        if (candidateAngle < 0) candidateAngle += 360f;

        float angleDiff = Mathf.Abs(candidateAngle - zoneCenter);
        if (angleDiff > 180f)
            angleDiff = 360f - angleDiff;

        // Positional weight is 1.0 at the center and decreases linearly to 0 at the zone edge.
        float positionWeight = Mathf.Clamp01(1f - (angleDiff / zoneHalfAngle));

        return coverageScore * positionWeight;
    }

    /// <summary>
    /// Original per-frame coverage score calculation.
    /// </summary>
    private float CalculateCoverageScore(List<Vector3> positions, List<Quaternion> rotations)
    {
        if (positions == null || rotations == null || trackingPoints == null)
        {
            Debug.LogWarning("Positions, rotations, or tracking points list is null.");
            return 0f;
        }

        int visibleJoints = 0;

        foreach (var joint in trackingPoints)
        {
            if (joint == null) continue;
            foreach (int i in Enumerable.Range(0, positions.Count))
            {
                Vector3 camPos = positions[i];
                Quaternion camRot = rotations[i];

                if (IsPointInFOV(camPos, camRot, joint.position) && IsJointVisible(camPos, joint.position, joint))
                {
                    visibleJoints++;
                    break;
                }
            }
        }

        return (float)visibleJoints / trackingPoints.Count;
    }

    /// <summary>
    /// Determines if a joint is visible from a given camera position (using a raycast).
    /// </summary>
    private bool IsJointVisible(Vector3 camPos, Vector3 jointPos, Transform joint)
    {
        Vector3 toJoint = (jointPos - camPos).normalized;
        float distance = Vector3.Distance(camPos, jointPos);
        RaycastHit[] hits = new RaycastHit[10]; // Adjust size as needed
        int hitCount = Physics.RaycastNonAlloc(camPos, toJoint, hits, distance, raycastLayerMask);
        System.Array.Sort(hits, 0, hitCount, new RaycastHitComparer());

        for (int i = 0; i < hitCount; i++)
        {
            if (hits[i].transform == joint)
            {
                for (int j = 0; j < i; j++)
                {
                    if (hits[j].transform != joint)
                    {
                        return false; // Occluded
                    }
                }
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if a given point is within the camera's field of view.
    /// </summary>
    private bool IsPointInFOV(Vector3 camPos, Quaternion camRot, Vector3 point)
    {
        Vector3 toPoint = (point - camPos).normalized;
        Vector3 forward = camRot * Vector3.forward;

        float halfFOV = fovAngle / 2f;
        float verticalFOV = halfFOV * (9f / 16f);  // Approximation for 16:9 aspect

        float horizontalAngle = Vector3.Angle(forward, toPoint);
        if (horizontalAngle > halfFOV)
            return false;

        float verticalAngle = Vector3.Angle(forward, toPoint);
        if (verticalAngle > verticalFOV)
            return false;

        return true;
    }

    private void OnDrawGizmos()
    {
        // While animation is playing, draw temporary cameras (yellow) and their raycasts.
        if (_isAnimationPlaying)
        {
            for (int i = 0; i < _tempCameraPositions.Count; i++)
            {
                Vector3 camPos = _tempCameraPositions[i];
                Quaternion camRot = _tempCameraRotations[i];

                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(camPos, 0.2f);
                DrawCameraFOVPyramid(camPos, camRot);

                // Draw raycasts from this temporary camera to each visible tracking point.
                foreach (var joint in trackingPoints)
                {
                    if (joint == null) continue;
                    if (IsPointInFOV(camPos, camRot, joint.position) && IsJointVisible(camPos, joint.position, joint))
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(camPos, joint.position);
                    }
                }
            }
        }
        else // After the animation ends, draw definitive cameras (blue) and their raycasts.
        {
            for (int i = 0; i < _selectedCameraPositions.Count; i++)
            {
                Vector3 camPos = _selectedCameraPositions[i];
                Quaternion camRot = _selectedCameraRotations[i];

                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(camPos, 0.2f);
                DrawCameraFOVPyramid(camPos, camRot);

                // Draw raycasts from definitive cameras.
                foreach (var joint in trackingPoints)
                {
                    if (joint == null) continue;
                    if (IsPointInFOV(camPos, camRot, joint.position) && IsJointVisible(camPos, joint.position, joint))
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(camPos, joint.position);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Draws a pyramid representing the camera's field of view.
    /// </summary>
    private void DrawCameraFOVPyramid(Vector3 position, Quaternion rotation)
    {
        Gizmos.color = new Color(0, 0, 1, 0.2f); // Transparent blue

        Vector3 forward = rotation * Vector3.forward * maxViewDistance;
        Vector3 right = Quaternion.Euler(0, fovAngle / 2f, 0) * forward;
        Vector3 left = Quaternion.Euler(0, -fovAngle / 2f, 0) * forward;
        Vector3 top = Quaternion.Euler(-fovAngle / 3f, 0, 0) * forward;
        Vector3 bottom = Quaternion.Euler(fovAngle / 3f, 0, 0) * forward;

        Vector3 frontCenter = position + forward;
        Vector3 frontTopLeft = frontCenter + (left - forward) + (top - forward);
        Vector3 frontTopRight = frontCenter + (right - forward) + (top - forward);
        Vector3 frontBottomLeft = frontCenter + (left - forward) + (bottom - forward);
        Vector3 frontBottomRight = frontCenter + (right - forward) + (bottom - forward);

        // Connect camera to FOV edges
        Gizmos.DrawLine(position, frontTopLeft);
        Gizmos.DrawLine(position, frontTopRight);
        Gizmos.DrawLine(position, frontBottomLeft);
        Gizmos.DrawLine(position, frontBottomRight);

        // Connect front face of the FOV pyramid
        Gizmos.DrawLine(frontTopLeft, frontTopRight);
        Gizmos.DrawLine(frontTopLeft, frontBottomLeft);
        Gizmos.DrawLine(frontTopRight, frontBottomRight);
        Gizmos.DrawLine(frontBottomLeft, frontBottomRight);
    }

    private class RaycastHitComparer : IComparer<RaycastHit>
    {
        public int Compare(RaycastHit a, RaycastHit b)
        {
            return a.distance.CompareTo(b.distance);
        }
    }

    public float GetAnimDuration()
    {
        return _animationDuration;
    }
}
