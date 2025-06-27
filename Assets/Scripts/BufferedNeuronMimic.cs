using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Neuron;

public class BufferedNeuronMimic : MonoBehaviour
{
    [Header("Source (Mocap)")]
    public NeuronTransformsInstance sourceInstance;

    [Header("Target (Static Root)")]
    public Transform targetRoot;

    [Header("UI References")]
    public Button freezeButton;
    public Button slowDownButton;
    public Button speedUpButton;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI statusText;

    [Header("Status (Read Only)")]
    [SerializeField, ReadOnly]
    private bool isFrozen;
    [SerializeField, ReadOnly]
    private float currentSpeed = 1f;
    [SerializeField, ReadOnly]
    private int bufferedFrames;
    [SerializeField, ReadOnly]
    private float playbackDelay;

    // Internal timing
    private float playbackHeadTime;
    private int currentPlaybackIndex;

    // Movement frame
    private class MovementFrame
    {
        public float timestamp;
        public Vector3[] positions;
        public Quaternion[] rotations;
        public Vector3 rootPosition;
    }

    private Transform[] srcBones;
    private Transform[] tgtBones;
    private List<MovementFrame> frameBuffer;

    private const float SPEED_INCREMENT = 0.25f;
    private const float MIN_SPEED = 0.25f;
    private const float MAX_SPEED = 2f;
    [SerializeField] private GlitchTimerController glitchTimerController;

    // Central method to check if changes are allowed
    private bool AreChangesAllowed()
    {
        return glitchTimerController.IsTimerRunning;
    }

    void Start()
    {
        if (sourceInstance == null || targetRoot == null)
        {
            Debug.LogError("[BufferedNeuronMimic] Assign both sourceInstance and targetRoot.");
            enabled = false;
            return;
        }

        srcBones = sourceInstance.GetTransforms();
        frameBuffer = new List<MovementFrame>();

        var boneMap = new Dictionary<string, Transform>();
        foreach (var t in targetRoot.GetComponentsInChildren<Transform>())
            boneMap[t.name] = t;

        tgtBones = new Transform[srcBones.Length];
        for (int i = 0; i < srcBones.Length; i++)
            if (srcBones[i] != null && boneMap.TryGetValue(srcBones[i].name, out var found))
                tgtBones[i] = found;

        playbackHeadTime = Time.time;
        currentPlaybackIndex = 0;
        currentSpeed = 1f;
        isFrozen = false;

        freezeButton?.onClick.AddListener(ToggleFreeze);
        slowDownButton?.onClick.AddListener(DecreaseSpeed);
        speedUpButton?.onClick.AddListener(IncreaseSpeed);

        UpdateUI();
    }

    void OnDestroy()
    {
        freezeButton?.onClick.RemoveListener(ToggleFreeze);
        slowDownButton?.onClick.RemoveListener(DecreaseSpeed);
        speedUpButton?.onClick.RemoveListener(IncreaseSpeed);
    }

    public void ToggleFreeze()
    {
        if (!AreChangesAllowed()) return;
        
        isFrozen = !isFrozen;
        if (!isFrozen && frameBuffer.Count > 0)
        {
            // Jump to next buffered frame on unfreeze
            CaptureCurrentFrame(Time.time);
            if (currentPlaybackIndex < frameBuffer.Count - 1)
                currentPlaybackIndex++;
            playbackHeadTime = frameBuffer[currentPlaybackIndex].timestamp;
            ApplyFrame(frameBuffer[currentPlaybackIndex]);
        }
        UpdateUI();
    }

    public void DecreaseSpeed()
    {
        if (!AreChangesAllowed()) return;
        
        currentSpeed = Mathf.Max(MIN_SPEED, currentSpeed - SPEED_INCREMENT);
        UpdateUI();
    }

    public void IncreaseSpeed()
    {
        if (!AreChangesAllowed()) return;
        
        currentSpeed = Mathf.Min(MAX_SPEED, currentSpeed + SPEED_INCREMENT);
        UpdateUI();
    }

    private void UpdateUI()
    {
        speedText.text = $"Speed: {currentSpeed:F2}x";
        statusText.text = isFrozen ? "Status: Frozen" : "Status: Playing";
        
        // Update button interactability based on current state
        bool changesAllowed = AreChangesAllowed();
        freezeButton.interactable = changesAllowed;
        slowDownButton.interactable = changesAllowed && currentSpeed > MIN_SPEED;
        speedUpButton.interactable = changesAllowed && currentSpeed < MAX_SPEED;
    }

    void LateUpdate()
    {
        if (srcBones == null || tgtBones == null) return;

        float now = Time.time;

        // Always capture the artist
        CaptureCurrentFrame(now);

        if (!isFrozen)
        {
            playbackHeadTime += Time.deltaTime * currentSpeed;
            // Clamp to latest
            if (frameBuffer.Count > 0)
            {
                float latest = frameBuffer[frameBuffer.Count - 1].timestamp;
                playbackHeadTime = Mathf.Min(playbackHeadTime, latest);
            }

            PlayFrameAtTime(playbackHeadTime);

            // Auto-reset speed when caught up
            if (currentSpeed > 1f && frameBuffer.Count > 0)
            {
                float latest = frameBuffer[frameBuffer.Count - 1].timestamp;
                if (playbackHeadTime >= latest - 0.001f)
                {
                    currentSpeed = 1f;
                    UpdateUI();
                }
            }
        }

        bufferedFrames = frameBuffer.Count;
        playbackDelay = now - playbackHeadTime;
    }

    private void CaptureCurrentFrame(float time)
    {
        int hips = (int)NeuronBones.Hips;
        Vector3 rootPos = srcBones[hips]?.position ?? Vector3.zero;

        var frame = new MovementFrame
        {
            timestamp = time,
            positions = new Vector3[srcBones.Length],
            rotations = new Quaternion[srcBones.Length],
            rootPosition = rootPos
        };

        for (int i = 0; i < srcBones.Length; i++)
            if (srcBones[i] != null)
            {
                frame.positions[i] = srcBones[i].position;
                frame.rotations[i] = srcBones[i].rotation;
            }

        frameBuffer.Add(frame);
        // Remove old frames >10s
        while (frameBuffer.Count > 1 && frameBuffer[1].timestamp < time - 10f)
        {
            frameBuffer.RemoveAt(0);
            if (currentPlaybackIndex > 0) currentPlaybackIndex--;
        }
    }

    private void PlayFrameAtTime(float t)
    {
        while (currentPlaybackIndex < frameBuffer.Count - 1 &&
               frameBuffer[currentPlaybackIndex + 1].timestamp <= t)
            currentPlaybackIndex++;

        ApplyFrame(frameBuffer[currentPlaybackIndex]);
    }

    private void ApplyFrame(MovementFrame frame)
    {
        for (int i = 0; i < srcBones.Length; i++)
        {
            var bone = tgtBones[i];
            if (bone == null) continue;
            bone.rotation = frame.rotations[i];
            bone.position = targetRoot.position + (frame.positions[i] - frame.rootPosition);
        }
    }

    public void ClearBuffer()
    {
        if (!AreChangesAllowed()) return;
        
        frameBuffer.Clear();
        currentPlaybackIndex = 0;
        playbackHeadTime = Time.time;
        currentSpeed = 1f;
        isFrozen = false;
        UpdateUI();
    }
}

public class ReadOnlyAttribute : PropertyAttribute { }
