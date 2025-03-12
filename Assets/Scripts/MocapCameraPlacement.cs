using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

public enum PlacementMode { Circle, Room }
public enum SurfaceType { Cylinder, Dome }

[ExecuteInEditMode]
public class MocapCameraPlacement : MonoBehaviour
{
    // CAMERA PLACEMENT SETTINGS
    [Header("Camera Placement Settings")]
    [SerializeField] private int numberOfCameras = 3;
    [SerializeField] private float placementRadius = 5f;
    [SerializeField] private float minHeight = 1f;
    [SerializeField] private float maxHeight = 3f;
    [SerializeField] private float fovAngle = 60f;   // Horizontal FOV
    [SerializeField] private float maxViewDistance = 10f; // Maximum viewable range

    // TRACKING POINTS (JOINTS FOR RAYCASTS)
    [Header("Tracking Points (Joints for Raycasts)")]
    [SerializeField] private List<Transform> trackingPoints = new List<Transform>();

    // RAYCAST SETTINGS
    [Header("Raycast Settings")]
    [SerializeField] private LayerMask raycastLayerMask;
    [SerializeField] private float raycastFPS = 30f;

    // GRID SETTINGS
    [Header("Grid Settings")]
    [SerializeField] private float gridSpacing = 0.3f;

    // ANIMATION SETTINGS
    [Header("Animation Settings")]
    [SerializeField] private AnimationClip animationClip;
    private float _animationDuration;
    private float _elapsedTime;

    // PLACEMENT OPTIONS
    [Header("Placement Options")]
    [Tooltip("Full degrees in which cameras may be placed (360° = full circle, 180° = semi circle, etc.)")]
    [SerializeField] private float maxPlacementAngle = 360f;
    [Tooltip("Center angle (in world degrees) of the allowed placement arc")]
    [SerializeField] private float allowedArcCenterAngle = 0f;
    [Tooltip("Choose whether candidates are placed around the character (Circle) or on a room's surfaces (Room)")]
    [SerializeField] private PlacementMode placementMode = PlacementMode.Circle;
    [Tooltip("Choose whether cameras are placed along a cylinder or on a dome (or ceiling in room mode)")]
    [SerializeField] private SurfaceType surfaceType = SurfaceType.Cylinder;

    // ROOM SETTINGS (used if Placement Mode is Room)
    [Header("Room Settings (only used if Placement Mode is Room)")]
    [SerializeField] private float roomWidth = 10f;
    [SerializeField] private float roomDepth = 10f;
    [SerializeField] private float roomHeight = 3f;

    // INTERNAL LISTS
    private List<Vector3> _allPossiblePositions = new List<Vector3>();
    private List<Quaternion> _allPossibleRotations = new List<Quaternion>();
    private List<float> _accumulatedScores = new List<float>();

    // New hit/miss counters (one entry per candidate)
    private List<int> _accumulatedHits = new List<int>();
    private List<int> _accumulatedMisses = new List<int>();

    // TEMPORARY & DEFINITIVE CAMERA CANDIDATES
    private List<Vector3> _selectedCameraPositions = new List<Vector3>();
    private List<Quaternion> _selectedCameraRotations = new List<Quaternion>();

    private List<Vector3> _tempCameraPositions = new List<Vector3>();
    private List<Quaternion> _tempCameraRotations = new List<Quaternion>();

    private bool _isAnimationPlaying = false;
    private float _raycastTimer = 0f;

    // DATA STRUCTURE FOR A FULL CONFIGURATION
// In the CameraConfiguration class, add lists for per‑camera hits and misses:
    public class CameraConfiguration {
        public List<Vector3> positions;
        public List<Quaternion> rotations;
        public List<int> perCameraHits;    // New: per-camera hit counts
        public List<int> perCameraMisses;  // New: per-camera miss counts
        public int totalHits;
        public int totalMisses;
        public float totalScore;
    }
    private CameraConfiguration _bestConfiguration;
    private CameraConfiguration _secondBestConfiguration;
    private List<int> _bestIndices = new List<int>(); // store candidate indices used in best configuration

    // The active configuration that will be drawn (0 = best, 1 = second best)
    private CameraConfiguration _activeConfiguration;

    // Event to notify UIManager when configurations are ready
    public delegate void ConfigurationsReadyHandler(CameraConfiguration best, CameraConfiguration second);
    public event ConfigurationsReadyHandler OnConfigurationsReady;

    private void Awake()
    {
        if (animationClip != null)
        {
            _animationDuration = animationClip.length / 5f;
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
        ResetAccumulatedData();
        _isAnimationPlaying = true;
    }

    private void Update()
    {
        if (_isAnimationPlaying)
        {
            _elapsedTime += Time.deltaTime;
            _raycastTimer += Time.deltaTime;

            if (_raycastTimer >= 1f / raycastFPS)
            {
                SelectTemporaryCameras();
                _raycastTimer = 0f;
            }

            CalculateScoresForCurrentFrame();

            if (_elapsedTime >= _animationDuration)
            {
                OnAnimationLoop();
                _elapsedTime = 0f;
                _isAnimationPlaying = false;
            }
        }
    }
    
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
            bool tooClose = selectedPositions.Any(selectedPos =>
                Vector3.Distance(selectedPos, _allPossiblePositions[i]) < placementRadius / (numberOfCameras * 2f));
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


    private void GenerateAllPossibleCameraPositions()
    {
        Debug.Log("Generating all possible camera positions...");
        _allPossiblePositions.Clear();
        _allPossibleRotations.Clear();
        _accumulatedScores.Clear();

        if (placementMode == PlacementMode.Circle)
        {
            if (surfaceType == SurfaceType.Cylinder)
            {
                int heightSteps = Mathf.FloorToInt((maxHeight - minHeight) / gridSpacing);
                int angleSteps = Mathf.FloorToInt(maxPlacementAngle / (gridSpacing / placementRadius * Mathf.Rad2Deg));
                float angleIncrement = maxPlacementAngle / angleSteps;
                float startAngle = allowedArcCenterAngle - maxPlacementAngle / 2f;
                for (int h = 0; h < heightSteps; h++)
                {
                    float y = minHeight + h * gridSpacing;
                    for (int a = 0; a < angleSteps; a++)
                    {
                        float angle = startAngle + a * angleIncrement;
                        float rad = angle * Mathf.Deg2Rad;
                        float x = Mathf.Cos(rad) * placementRadius;
                        float z = Mathf.Sin(rad) * placementRadius;
                        Vector3 pos = transform.position + new Vector3(x, y, z);
                        Quaternion rot = Quaternion.LookRotation(transform.position - pos, Vector3.up);
                        _allPossiblePositions.Add(pos);
                        _allPossibleRotations.Add(rot);
                    }
                }
            }
            else if (surfaceType == SurfaceType.Dome)
            {
                int elevSteps = Mathf.Max(2, Mathf.FloorToInt((maxHeight - minHeight) / gridSpacing));
                int azimSteps = Mathf.FloorToInt(maxPlacementAngle / (gridSpacing / placementRadius * Mathf.Rad2Deg));
                float azimIncrement = maxPlacementAngle / azimSteps;
                float startAzim = allowedArcCenterAngle - maxPlacementAngle / 2f;
                for (int e = 0; e < elevSteps; e++)
                {
                    float t = (float)e / (elevSteps - 1);
                    float elevAngle = Mathf.Lerp(0, 90f, t);
                    float yOffset = Mathf.Sin(elevAngle * Mathf.Deg2Rad) * placementRadius;
                    float candidateY = transform.position.y + yOffset;
                    float horizontalRadius = Mathf.Cos(elevAngle * Mathf.Deg2Rad) * placementRadius;
                    for (int a = 0; a < azimSteps; a++)
                    {
                        float azim = startAzim + a * azimIncrement;
                        float radAzim = azim * Mathf.Deg2Rad;
                        float x = Mathf.Cos(radAzim) * horizontalRadius;
                        float z = Mathf.Sin(radAzim) * horizontalRadius;
                        Vector3 pos = new Vector3(transform.position.x + x, candidateY, transform.position.z + z);
                        Quaternion rot = Quaternion.LookRotation(transform.position - pos, Vector3.up);
                        _allPossiblePositions.Add(pos);
                        _allPossibleRotations.Add(rot);
                    }
                }
            }
        }
        else if (placementMode == PlacementMode.Room)
        {
            if (surfaceType == SurfaceType.Cylinder)
            {
                int heightSteps = Mathf.FloorToInt((maxHeight - minHeight) / gridSpacing);
                int frontSteps = Mathf.FloorToInt(roomWidth / gridSpacing);
                int sideSteps = Mathf.FloorToInt(roomDepth / gridSpacing);

                // Front wall (z = roomDepth/2)
                for (int h = 0; h < heightSteps; h++)
                {
                    float y = minHeight + h * gridSpacing;
                    for (int i = 0; i <= frontSteps; i++)
                    {
                        float t = (float)i / frontSteps;
                        float x = Mathf.Lerp(-roomWidth / 2, roomWidth / 2, t);
                        float z = roomDepth / 2;
                        Vector3 pos = transform.position + new Vector3(x, y, z);
                        if (!IsWithinAllowedArc(pos)) continue;
                        Quaternion rot = Quaternion.LookRotation(transform.position - pos, Vector3.up);
                        _allPossiblePositions.Add(pos);
                        _allPossibleRotations.Add(rot);
                    }
                }

                // Back wall (z = -roomDepth/2)
                for (int h = 0; h < heightSteps; h++)
                {
                    float y = minHeight + h * gridSpacing;
                    for (int i = 0; i <= frontSteps; i++)
                    {
                        float t = (float)i / frontSteps;
                        float x = Mathf.Lerp(-roomWidth / 2, roomWidth / 2, t);
                        float z = -roomDepth / 2;
                        Vector3 pos = transform.position + new Vector3(x, y, z);
                        if (!IsWithinAllowedArc(pos)) continue;
                        Quaternion rot = Quaternion.LookRotation(transform.position - pos, Vector3.up);
                        _allPossiblePositions.Add(pos);
                        _allPossibleRotations.Add(rot);
                    }
                }

                // Left wall (x = -roomWidth/2)
                for (int h = 0; h < heightSteps; h++)
                {
                    float y = minHeight + h * gridSpacing;
                    for (int i = 0; i <= sideSteps; i++)
                    {
                        float t = (float)i / sideSteps;
                        float z = Mathf.Lerp(-roomDepth / 2, roomDepth / 2, t);
                        float x = -roomWidth / 2;
                        Vector3 pos = transform.position + new Vector3(x, y, z);
                        if (!IsWithinAllowedArc(pos)) continue;
                        Quaternion rot = Quaternion.LookRotation(transform.position - pos, Vector3.up);
                        _allPossiblePositions.Add(pos);
                        _allPossibleRotations.Add(rot);
                    }
                }

                // Right wall (x = roomWidth/2)
                for (int h = 0; h < heightSteps; h++)
                {
                    float y = minHeight + h * gridSpacing;
                    for (int i = 0; i <= sideSteps; i++)
                    {
                        float t = (float)i / sideSteps;
                        float z = Mathf.Lerp(-roomDepth / 2, roomDepth / 2, t);
                        float x = roomWidth / 2;
                        Vector3 pos = transform.position + new Vector3(x, y, z);
                        if (!IsWithinAllowedArc(pos)) continue;
                        Quaternion rot = Quaternion.LookRotation(transform.position - pos, Vector3.up);
                        _allPossiblePositions.Add(pos);
                        _allPossibleRotations.Add(rot);
                    }
                }
            }
            else if (surfaceType == SurfaceType.Dome)
            {
                // Place candidates on the ceiling.
                int ceilingStepsX = Mathf.FloorToInt(roomWidth / gridSpacing);
                int ceilingStepsZ = Mathf.FloorToInt(roomDepth / gridSpacing);
                for (int i = 0; i <= ceilingStepsX; i++)
                {
                    for (int j = 0; j <= ceilingStepsZ; j++)
                    {
                        float tX = (float)i / ceilingStepsX;
                        float tZ = (float)j / ceilingStepsZ;
                        float x = Mathf.Lerp(-roomWidth / 2, roomWidth / 2, tX);
                        float z = Mathf.Lerp(-roomDepth / 2, roomDepth / 2, tZ);
                        Vector3 pos = transform.position + new Vector3(x, roomHeight, z);
                        if (!IsWithinAllowedArc(pos)) continue;
                        // Look down for ceiling candidates.
                        Quaternion rot = Quaternion.LookRotation(transform.position - pos, Vector3.down);
                        _allPossiblePositions.Add(pos);
                        _allPossibleRotations.Add(rot);
                    }
                }
            }
        }

        int count = _allPossiblePositions.Count;
        _accumulatedScores = new List<float>(new float[count]);
        _accumulatedHits = Enumerable.Repeat(0, count).ToList();
        _accumulatedMisses = Enumerable.Repeat(0, count).ToList();

        Debug.Log($"Generated {count} possible camera positions.");
    }

    private bool IsWithinAllowedArc(Vector3 pos)
    {
        Vector3 dir = pos - transform.position;
        float angle = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        float delta = Mathf.Abs(Mathf.DeltaAngle(angle, allowedArcCenterAngle));
        return delta <= maxPlacementAngle / 2f;
    }

    private void ResetAccumulatedData()
    {
        int count = _allPossiblePositions.Count;
        _accumulatedScores = new List<float>(new float[count]);
        _accumulatedHits = Enumerable.Repeat(0, count).ToList();
        _accumulatedMisses = Enumerable.Repeat(0, count).ToList();
    }

    private void CalculateScoresForCurrentFrame()
    {
        for (int i = 0; i < _allPossiblePositions.Count; i++)
        {
            // Get a score for candidate i
            float score = CalculateCoverageScore(
                new List<Vector3> { _allPossiblePositions[i] },
                new List<Quaternion> { _allPossibleRotations[i] }
            );
            _accumulatedScores[i] += score;

            // For each tracking joint, count a hit if visible, otherwise a miss.
            foreach (var joint in trackingPoints)
            {
                if (joint == null) continue;
                if (IsPointInFOV(_allPossiblePositions[i], _allPossibleRotations[i], joint.position) &&
                    IsJointVisible(_allPossiblePositions[i], joint.position, joint))
                {
                    _accumulatedHits[i]++;
                }
                else
                {
                    _accumulatedMisses[i]++;
                }
            }
        }
    }

    private void OnAnimationLoop()
    {
        Debug.Log("Animation looped. Selecting definitive best cameras...");
        // Select best configuration and store the candidate indices.
        _bestIndices = SelectConfigurationIndices(false, null);
        _selectedCameraPositions.Clear();
        _selectedCameraRotations.Clear();
        foreach (int idx in _bestIndices)
        {
            _selectedCameraPositions.Add(_allPossiblePositions[idx]);
            _selectedCameraRotations.Add(_allPossibleRotations[idx]);
        }
        _bestConfiguration = CreateConfigurationFromIndices(_bestIndices);

        // Select the second best configuration by choosing alternate candidates in each zone.
        List<int> secondIndices = SelectConfigurationIndices(true, _bestIndices);
        _secondBestConfiguration = CreateConfigurationFromIndices(secondIndices);

        // Default the active configuration to the best one.
        _activeConfiguration = _bestConfiguration;

        Debug.Log($"Definitively selected {_selectedCameraPositions.Count} best cameras.");
        // Notify UIManager that configurations are ready.
        OnConfigurationsReady?.Invoke(_bestConfiguration, _secondBestConfiguration);
    }

    // Zone‐based selection. If secondBest is true, candidates already used in bestIndices are skipped (when possible).
    private List<int> SelectConfigurationIndices(bool secondBest, List<int> bestIndices)
    {
        List<int> selectedIndices = new List<int>();
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
                // For second-best, skip candidate used in best configuration if available.
                if (secondBest && bestIndices != null && zone < bestIndices.Count && i == bestIndices[zone])
                    continue;
                bool tooClose = selectedPositions.Any(selectedPos =>
                    Vector3.Distance(selectedPos, _allPossiblePositions[i]) < placementRadius / (numberOfCameras * 2f));
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
                Debug.LogWarning($"No suitable candidate found in zone {zone}. Falling back to best overall candidate.");
                bestCandidateIndex = _accumulatedScores
                    .Select((score, index) => new { Score = score, Index = index })
                    .OrderByDescending(x => x.Score)
                    .FirstOrDefault()?.Index ?? 0;
            }

            selectedIndices.Add(bestCandidateIndex);
            selectedPositions.Add(_allPossiblePositions[bestCandidateIndex]);

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

        return selectedIndices;
    }

    private CameraConfiguration CreateConfigurationFromIndices(List<int> indices)
    {
        CameraConfiguration config = new CameraConfiguration();
        config.positions = new List<Vector3>();
        config.rotations = new List<Quaternion>();
        config.perCameraHits = new List<int>();    // Initialize new list
        config.perCameraMisses = new List<int>();  // Initialize new list
        config.totalHits = 0;
        config.totalMisses = 0;
        config.totalScore = 0f;
        foreach (int index in indices)
        {
            config.positions.Add(_allPossiblePositions[index]);
            config.rotations.Add(_allPossibleRotations[index]);
            config.perCameraHits.Add(_accumulatedHits[index]);     // Store individual hit count
            config.perCameraMisses.Add(_accumulatedMisses[index]);   // Store individual miss count
            config.totalHits += _accumulatedHits[index];
            config.totalMisses += _accumulatedMisses[index];
            config.totalScore += _accumulatedScores[index];
        }
        return config;
    }


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
        float positionWeight = Mathf.Clamp01(1f - (angleDiff / zoneHalfAngle));

        return coverageScore * positionWeight;
    }

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

    private bool IsJointVisible(Vector3 camPos, Vector3 jointPos, Transform joint)
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


    private bool IsPointInFOV(Vector3 camPos, Quaternion camRot, Vector3 point)
    {
        Vector3 toPoint = (point - camPos).normalized;
        Vector3 forward = camRot * Vector3.forward;
        float halfFOV = fovAngle / 2f;
        float verticalFOV = halfFOV * (9f / 16f);
        if (Vector3.Angle(forward, toPoint) > halfFOV)
            return false;
        if (Vector3.Angle(forward, toPoint) > verticalFOV)
            return false;
        return true;
    }

    // Helper method: draws an edge with segments colored based on whether the midpoint is in the allowed arc.
    private void DrawSegmentedEdge(Vector3 a, Vector3 b, int segments = 50)
    {
        for (int i = 0; i < segments; i++)
        {
            float t0 = (float)i / segments;
            float t1 = (float)(i + 1) / segments;
            Vector3 p0 = Vector3.Lerp(a, b, t0);
            Vector3 p1 = Vector3.Lerp(a, b, t1);
            Vector3 mid = (p0 + p1) * 0.5f;
            Gizmos.color = IsWithinAllowedArc(mid) ? Color.green : Color.red;
            Gizmos.DrawLine(p0, p1);
        }
    }

    private void OnDrawGizmos()
{
    // Draw placement guides (arc or room outline)
    if (placementMode == PlacementMode.Circle)
    {
        DrawPlacementArc();
    }
    else if (placementMode == PlacementMode.Room)
    {
        DrawRoomOutline();
    }

    #if UNITY_EDITOR
    if (_isAnimationPlaying)
    {
        // Draw temporary cameras (during animation)
        for (int i = 0; i < _tempCameraPositions.Count; i++)
        {
            Vector3 camPos = _tempCameraPositions[i];
            Quaternion camRot = _tempCameraRotations[i];

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(camPos, 0.2f);
            DrawCameraFOVPyramid(camPos, camRot);

            // Display only the camera number (offset above the sphere)
            Handles.Label(camPos + Vector3.up * 0.3f, (i + 1).ToString());

            foreach (var joint in trackingPoints)
            {
                if (joint == null) continue;
                if (IsPointInFOV(camPos, camRot, joint.position))
                {
                    Gizmos.color = IsJointVisible(camPos, joint.position, joint) ? Color.green : Color.red;
                    Gizmos.DrawLine(camPos, joint.position);
                }
                else
                {
                    Gizmos.color = Color.gray;
                    Gizmos.DrawLine(camPos, joint.position);
                }
            }
        }
    }
    else if (_activeConfiguration != null)
    {
        // Draw final (selected) configuration
        for (int i = 0; i < _activeConfiguration.positions.Count; i++)
        {
            Vector3 camPos = _activeConfiguration.positions[i];
            Quaternion camRot = _activeConfiguration.rotations[i];

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(camPos, 0.2f);
            DrawCameraFOVPyramid(camPos, camRot);

            // Compute per-camera hit and miss percentages.
            int hits = _activeConfiguration.perCameraHits[i];
            int misses = _activeConfiguration.perCameraMisses[i];
            int total = hits + misses;
            float hitPercent = total > 0 ? (float)hits / total * 100f : 0f;
            float missPercent = total > 0 ? (float)misses / total * 100f : 0f;

            // Display the camera number and percentages above the sphere.
            string label = (i + 1).ToString() + " (H:" + hitPercent.ToString("F0") + "%, M:" + missPercent.ToString("F0") + "%)";
            Handles.Label(camPos + Vector3.up * 0.3f, label);

            foreach (var joint in trackingPoints)
            {
                if (joint == null) continue;
                if (IsPointInFOV(camPos, camRot, joint.position))
                {
                    Gizmos.color = IsJointVisible(camPos, joint.position, joint) ? Color.green : Color.red;
                    Gizmos.DrawLine(camPos, joint.position);
                }
                else
                {
                    Gizmos.color = Color.gray;
                    Gizmos.DrawLine(camPos, joint.position);
                }
            }
        }
    }
    #endif
}

    private void DrawCameraFOVPyramid(Vector3 position, Quaternion rotation)
    {
        Gizmos.color = new Color(0, 0, 1, 0.2f);
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
        Gizmos.DrawLine(position, frontTopLeft);
        Gizmos.DrawLine(position, frontTopRight);
        Gizmos.DrawLine(position, frontBottomLeft);
        Gizmos.DrawLine(position, frontBottomRight);
        Gizmos.DrawLine(frontTopLeft, frontTopRight);
        Gizmos.DrawLine(frontTopLeft, frontBottomLeft);
        Gizmos.DrawLine(frontTopRight, frontBottomRight);
        Gizmos.DrawLine(frontBottomLeft, frontBottomRight);
    }

    private void DrawPlacementArc()
    {
        Gizmos.color = Color.red;
        int segments = 50;
        float startAngle = allowedArcCenterAngle - maxPlacementAngle / 2f;
        float endAngle = allowedArcCenterAngle + maxPlacementAngle / 2f;
        Vector3 prevPoint = transform.position + new Vector3(Mathf.Cos(startAngle * Mathf.Deg2Rad), 0, Mathf.Sin(startAngle * Mathf.Deg2Rad)) * placementRadius;
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = Mathf.Lerp(startAngle, endAngle, t);
            Vector3 newPoint = transform.position + new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad)) * placementRadius;
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
        Vector3 arcStart = transform.position + new Vector3(Mathf.Cos(startAngle * Mathf.Deg2Rad), 0, Mathf.Sin(startAngle * Mathf.Deg2Rad)) * placementRadius;
        Vector3 arcEnd = transform.position + new Vector3(Mathf.Cos(endAngle * Mathf.Deg2Rad), 0, Mathf.Sin(endAngle * Mathf.Deg2Rad)) * placementRadius;
        Gizmos.DrawLine(transform.position, arcStart);
        Gizmos.DrawLine(transform.position, arcEnd);
    }

    private void DrawRoomOutline()
    {
        // Draw the complete room (floor, walls, ceiling) with segmented edges.
        Vector3 center = transform.position;
        Vector3 halfExtents = new Vector3(roomWidth, 0, roomDepth) * 0.5f;
        Vector3[] floorCorners = new Vector3[4];
        floorCorners[0] = center + new Vector3(-halfExtents.x, 0, -halfExtents.z);
        floorCorners[1] = center + new Vector3(-halfExtents.x, 0, halfExtents.z);
        floorCorners[2] = center + new Vector3(halfExtents.x, 0, halfExtents.z);
        floorCorners[3] = center + new Vector3(halfExtents.x, 0, -halfExtents.z);

        // Floor edges.
        for (int i = 0; i < 4; i++)
        {
            DrawSegmentedEdge(floorCorners[i], floorCorners[(i + 1) % 4]);
        }

        // Vertical edges.
        Vector3[] ceilingCorners = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            ceilingCorners[i] = floorCorners[i] + Vector3.up * roomHeight;
            DrawSegmentedEdge(floorCorners[i], ceilingCorners[i]);
        }

        // Ceiling edges.
        for (int i = 0; i < 4; i++)
        {
            DrawSegmentedEdge(ceilingCorners[i], ceilingCorners[(i + 1) % 4]);
        }
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

    // Public method to switch the active configuration (0 = best, 1 = second best)
    public void SetActiveConfiguration(int configIndex)
    {
        if (configIndex == 0)
        {
            _activeConfiguration = _bestConfiguration;
        }
        else if (configIndex == 1)
        {
            _activeConfiguration = _secondBestConfiguration;
        }
    }
}
