using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor.Animations;

public class BVHToMecanim : MonoBehaviour
{
    public string bvhFilePath = "path/to/your/file.bvh"; // Set in inspector
    public Animator animator; // Drag your Humanoid Animator here
    public float frameRate = 30f; // Frames per second
    public bool autoPlay = true; // Play animation automatically
    private List<int> rotationChannels = new List<int>(); // Tracks rotation channels per joint


    private List<string> jointNames = new List<string>();
    private List<Quaternion[]> jointRotations;
    private int frameCount;
    private float frameTime;
    private AnimationClip animationClip;

    void Start()
    {
        LoadBVH(bvhFilePath);
        if (autoPlay) ApplyAnimation();
    }

    void LoadBVH(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("BVH file not found: " + filePath);
            return;
        }

        string[] lines = File.ReadAllLines(filePath);
        ParseBVH(lines);
    }

void ParseBVH(string[] lines)
{
    bool inMotionSection = false;
    List<Quaternion[]> motionFrames = new List<Quaternion[]>();

    foreach (string line in lines)
    {
        string[] tokens = line.Trim().Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) continue; // Skip empty lines

        if (tokens[0] == "HIERARCHY") continue;
        if (tokens[0] == "MOTION") { inMotionSection = true; continue; }

        if (!inMotionSection)
        {
            if (tokens[0] == "ROOT" || tokens[0] == "JOINT")
            {
               // Debug.Log($"BVH Bone Found: {tokens[1]}");

                jointNames.Add(tokens[1]);
                rotationChannels.Add(0);
            }
            else if (tokens[0] == "CHANNELS")
            {
                if (tokens.Length > 1 && int.TryParse(tokens[1], out int channelCount))
                {
                    rotationChannels[jointNames.Count - 1] = channelCount;
                }
                else
                {
                    Debug.LogWarning($"Invalid CHANNELS line: {line}");
                }
            }
        }
        else
        {
            if (tokens[0] == "Frames:")
            {
                if (tokens.Length > 1 && int.TryParse(tokens[1], out frameCount))
                {
                    Debug.Log($"Total Frames: {frameCount}");
                }
                else
                {
                    Debug.LogError($"Invalid frame count: {line}");
                }
                continue;
            }

            if (tokens[0] == "Frame" && tokens[1] == "Time:")
            {
                if (tokens.Length > 2 && float.TryParse(tokens[2], NumberStyles.Float, CultureInfo.InvariantCulture, out frameTime))
                {
                    Debug.Log($"Frame Time: {frameTime}");
                }
                else
                {
                    Debug.LogError($"Invalid frame time: {line}");
                }
                continue;
            }

            // Handling motion data
            if (tokens.Length < jointNames.Count * 3)
            {
                Debug.LogWarning($"Skipping malformed motion data line: {line}");
                continue;
            }

            Quaternion[] frameRotations = new Quaternion[jointNames.Count];
            int dataIndex = 0;

            for (int i = 0; i < jointNames.Count; i++)
            {
                if (rotationChannels[i] == 3) // Rotation channels detected
                {
                    if (float.TryParse(tokens[dataIndex++], NumberStyles.Float, CultureInfo.InvariantCulture, out float xRot) &&
                        float.TryParse(tokens[dataIndex++], NumberStyles.Float, CultureInfo.InvariantCulture, out float yRot) &&
                        float.TryParse(tokens[dataIndex++], NumberStyles.Float, CultureInfo.InvariantCulture, out float zRot))
                    {
                        frameRotations[i] = Quaternion.Euler(xRot, yRot, zRot);
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid rotation values at frame {motionFrames.Count}: {line}");
                        frameRotations[i] = Quaternion.identity;
                    }
                }
                else
                {
                    frameRotations[i] = Quaternion.identity;
                }
            }

            motionFrames.Add(frameRotations);
        }
    }

    jointRotations = motionFrames;
    Debug.Log("BVH Loaded Successfully: Frames = " + frameCount);
}


void ApplyAnimation()
{
    if (animator == null || !animator.isHuman)
    {
        Debug.LogError("Animator is not assigned or not set to Humanoid.");
        return;
    }

    Debug.Log("Applying BVH animation to Mecanim...");

    animationClip = new AnimationClip();
    animationClip.frameRate = frameRate;
    float frameDuration = 1f / frameRate;

    for (int i = 0; i < jointNames.Count; i++)
    {
        string humanBone = MapBVHJointToHumanBone(jointNames[i]);
        if (string.IsNullOrEmpty(humanBone))
        {
            Debug.LogWarning($"Skipping unmapped bone: {jointNames[i]}");
            continue;
        }

        AnimationCurve xCurve = new AnimationCurve();
        AnimationCurve yCurve = new AnimationCurve();
        AnimationCurve zCurve = new AnimationCurve();

        for (int frame = 0; frame < frameCount; frame++)
        {
            float time = frame * frameDuration;
            Quaternion rotation = jointRotations[frame][i];

            xCurve.AddKey(new Keyframe(time, rotation.eulerAngles.x));
            yCurve.AddKey(new Keyframe(time, rotation.eulerAngles.y));
            zCurve.AddKey(new Keyframe(time, rotation.eulerAngles.z));

            Debug.Log($"Frame {frame}: {humanBone} Rotation => X:{rotation.eulerAngles.x} Y:{rotation.eulerAngles.y} Z:{rotation.eulerAngles.z}");
        }

        // FIX: Use correct Mecanim Humanoid paths
        animationClip.SetCurve($"HumanBodyBones/{humanBone}", typeof(Animator), "m_LocalRotation.x", xCurve);
        animationClip.SetCurve($"HumanBodyBones/{humanBone}", typeof(Animator), "m_LocalRotation.y", yCurve);
        animationClip.SetCurve($"HumanBodyBones/{humanBone}", typeof(Animator), "m_LocalRotation.z", zCurve);
    }

    AssetDatabase.CreateAsset(animationClip, "Assets/BVH_Animation.anim");
    AssetDatabase.SaveAssets();

    Debug.Log("BVH animation applied and saved as AnimationClip.");
}




string MapBVHJointToHumanBone(string bvhJointName)
{
    Dictionary<string, string> mapping = new Dictionary<string, string>
    {
        { "Hips", "Hips" },
        { "Spine", "Spine" },
        { "Spine1", "Chest" },
        { "Neck", "Neck" },
        { "Head", "Head" },

        { "LeftShoulder", "Left Shoulder" },
        { "LeftArm", "Left UpperArm" },
        { "LeftForeArm", "Left LowerArm" },
        { "LeftHand", "Left Hand" },

        { "RightShoulder", "Right Shoulder" },
        { "RightArm", "Right UpperArm" },
        { "RightForeArm", "Right LowerArm" },
        { "RightHand", "Right Hand" },

        { "LeftUpLeg", "Left UpperLeg" },
        { "LeftLeg", "Left LowerLeg" },
        { "LeftFoot", "Left Foot" },
        { "LeftToeBase", "Left Toes" },

        { "RightUpLeg", "Right UpperLeg" },
        { "RightLeg", "Right LowerLeg" },
        { "RightFoot", "Right Foot" },
        { "RightToeBase", "Right Toes" }
    };

    if (mapping.ContainsKey(bvhJointName))
        return mapping[bvhJointName];

    Debug.LogWarning($"Bone '{bvhJointName}' not found in mapping. Skipping.");
    return null;
}


}
