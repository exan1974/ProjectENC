using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;
using System.Linq;
using Debug = UnityEngine.Debug;

public class BVHImporter : EditorWindow
{
    public const string BLENDER_EXEC = @"C:\Program Files\Blender Foundation\Blender 4.3\blender.exe";

    public const string PYTHON_CODE = @"
import bpy
import sys
import os
import traceback

try:
    # Parse arguments safely
    argv = sys.argv
    if '--' in argv:
        bvh_index = argv.index('--') + 1
        if bvh_index < len(argv):
            bvh_in = argv[bvh_index]
        else:
            raise ValueError('No BVH file specified after -- argument.')
    else:
        raise ValueError('Missing -- separator in arguments.')

    fbx_out = os.path.splitext(bvh_in)[0] + '.fbx'
    print('Importing BVH file:', bvh_in)
    print('Exporting to FBX file:', fbx_out)

    # Clear the scene
    bpy.ops.wm.read_factory_settings(use_empty=True)

    # Import BVH
    bpy.ops.import_anim.bvh(
        filepath=bvh_in,
        filter_glob='*.bvh',
        global_scale=1.0,
        frame_start=1,
        use_fps_scale=False,
        use_cyclic=False,
        rotate_mode='NATIVE',
        axis_forward='-Z',
        axis_up='Y'
    )

    print('BVH Import successful.')

    # Rename bones to match Unity Humanoid rig
    rename_map = {
        'pelvis': 'Hips',
        'spine_01': 'Spine',
        'spine_02': 'Chest',
        'spine_03': 'UpperChest',
        'neck_01': 'Neck',
        'head': 'Head',
        'clavicle_l': 'LeftShoulder',
        'upperarm_l': 'LeftUpperArm',
        'lowerarm_l': 'LeftLowerArm',
        'hand_l': 'LeftHand',
        'clavicle_r': 'RightShoulder',
        'upperarm_r': 'RightUpperArm',
        'lowerarm_r': 'RightLowerArm',
        'hand_r': 'RightHand',
        'thigh_l': 'LeftUpperLeg',
        'calf_l': 'LeftLowerLeg',
        'foot_l': 'LeftFoot',
        'thigh_r': 'RightUpperLeg',
        'calf_r': 'RightLowerLeg',
        'foot_r': 'RightFoot'
    }

    # Find armature
    armature = None
    for obj in bpy.data.objects:
        if obj.type == 'ARMATURE':
            armature = obj
            break

    if armature is None:
        print('Error: No armature found.')
    else:
        for bone in armature.data.bones:
            old_name = bone.name
            if old_name in rename_map:
                new_name = rename_map[old_name]
                print(f'Renaming {old_name} -> {new_name}')
                bone.name = new_name

    # Export to FBX
    bpy.ops.export_scene.fbx(
        filepath=fbx_out,
        axis_forward='-Z',
        axis_up='Y',
        use_selection=False,
        apply_unit_scale=True,
        bake_space_transform=True
    )

    print('FBX Export successful.')

except Exception as e:
    print('Error during BVH conversion:', str(e))
    print(traceback.format_exc())
";

    private static string currentPath;
    private static string destination;

    [MenuItem("Tools/Import BVH...")]
    static void ImportBVH()
    {
        // Select BVH file
        string path = EditorUtility.OpenFilePanel("Select BVH file...", "", "bvh");
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("No BVH file selected.");
            return;
        }

        currentPath = path; // Ensure the correct file path is used

        // Select destination FBX file
        string dest = EditorUtility.SaveFilePanel("Select destination...", Application.dataPath, Path.GetFileNameWithoutExtension(path), "fbx");
        if (string.IsNullOrEmpty(dest))
        {
            Debug.LogError("No destination selected.");
            return;
        }

        if (!dest.Contains(Application.dataPath))
        {
            EditorUtility.DisplayDialog("Error", "You must select a folder inside the Unity project.", "Close");
            return;
        }

        destination = "Assets" + dest.Substring(Application.dataPath.Length);

        // Corrected command to pass the BVH file
        string command = $"-b --python-expr \"{PYTHON_CODE}\" -- \"{path}\"";
        var processInfo = new ProcessStartInfo(BLENDER_EXEC, command)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var process = new Process
        {
            StartInfo = processInfo,
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data)) Debug.Log("[Blender] " + e.Data);
        };
        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data)) Debug.LogError("[Blender ERROR] " + e.Data);
        };
        process.Exited += Process_Exited;

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to start Blender process: {ex.Message}");
        }
    }

    private static void Process_Exited(object sender, System.EventArgs e)
    {
        string directory = Path.GetDirectoryName(currentPath);
        Debug.Log("Checking directory: " + directory);

        foreach (var file in Directory.GetFiles(directory))
        {
            Debug.Log("Found file: " + file);
        }

        // Find the latest FBX file
        string[] fbxFiles = Directory.GetFiles(directory, "*.fbx");
        if (fbxFiles.Length > 0)
        {
            string newestFile = fbxFiles.OrderByDescending(f => File.GetLastWriteTime(f)).First();
            Debug.Log($"Latest FBX file detected: {newestFile}");

            try
            {
                File.Copy(newestFile, destination, true);
                Debug.Log("FBX successfully created and moved to Unity project.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error copying FBX: {ex.Message}");
            }
        }
        else
        {
            UnityEngine.Debug.LogError("Unable to create FBX file. No FBX file detected in output directory.");
        }
    }
}
