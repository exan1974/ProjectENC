using UnityEngine;
using System.Collections;

public class MeshTrail : MonoBehaviour
{
    [Header("Trail Timing")]
    [Tooltip("How long (in seconds) the trail remains active once triggered.")]
    [SerializeField] private float activeTime = 100f;

    [Header("Mesh Related")]
    [Tooltip("How frequently (in seconds) the skinned mesh is baked and instantiated.")]
    [SerializeField] private float meshRefreshRate = 0.1f;

    [Tooltip("How long after creation each baked mesh instance is destroyed (in seconds).")]
    [SerializeField] private float meshDestroyDelay = 3f;

    [Tooltip("The transform whose position and rotation are used to spawn the baked mesh.")]
    [SerializeField] private Transform positionToSpawn;

    [Header("Trail Materials")]
    [Tooltip("Material for the glowing fresnel trail. (Press 1 to trigger this trail.)")]
    [SerializeField] private Material fresnelMaterial;

    [Tooltip("Material for the replica trail (matches the character’s look). (Press 2 to trigger this trail.)")]
    [SerializeField] private Material replicaMaterial;

    [Header("Shader Animation Settings")]
    [Tooltip("The reference name of the float property in the shader to animate (e.g., '_Alpha').")]
    [SerializeField] private string shaderVarRef = "_Alpha";

    [Tooltip("How much the shader float property decreases per step (used for fading out the trail).")]
    [SerializeField] private float shaderVarRate = 0.1f;

    [Tooltip("How often (in seconds) the shader float property is updated.")]
    [SerializeField] private float shaderVarRefreshRate = 0.05f;

    private bool isTrailActive;
    private SkinnedMeshRenderer[] skinnedMeshRenderers;

    void Update()
    {
        // Trigger the glowing fresnel trail by pressing "1"
        if (Input.GetKeyDown(KeyCode.Alpha1) && !isTrailActive)
        {
            isTrailActive = true;
            StartCoroutine(ActivateTrail(activeTime, fresnelMaterial));
        }
        // Trigger the replica trail by pressing "2"
        else if (Input.GetKeyDown(KeyCode.Alpha2) && !isTrailActive)
        {
            isTrailActive = true;
            StartCoroutine(ActivateTrail(activeTime, replicaMaterial));
        }
    }

    IEnumerator ActivateTrail(float timeActive, Material usedMaterial)
    {
        while (timeActive > 0)
        {
            timeActive -= meshRefreshRate;

            if (skinnedMeshRenderers == null)
            {
                skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            }

            for (int i = 0; i < skinnedMeshRenderers.Length; i++)
            {
                // Create a new GameObject to hold the baked mesh trail segment
                GameObject gO = new GameObject("MeshTrailSegment");
                gO.transform.SetPositionAndRotation(positionToSpawn.position, positionToSpawn.rotation);

                // Add required mesh components
                MeshRenderer mR = gO.AddComponent<MeshRenderer>();
                MeshFilter mF = gO.AddComponent<MeshFilter>();

                // Bake the skinned mesh into a static mesh
                Mesh mesh = new Mesh();
                skinnedMeshRenderers[i].BakeMesh(mesh);

                // Assign the baked mesh and the chosen material
                mF.mesh = mesh;
                mR.material = usedMaterial;

                // Animate the material's float property (for fading, etc.)
                // If the replica material should not fade out, you can skip this coroutine.
                if(mR.material.HasProperty(shaderVarRef))
                {
                    StartCoroutine(AnimateMaterialFloat(mR.material, 0, shaderVarRate, shaderVarRefreshRate));
                }

                // Clean up the trail segment after a delay
                Destroy(gO, meshDestroyDelay);
            }

            yield return new WaitForSeconds(meshRefreshRate);
        }

        isTrailActive = false;
    }

    IEnumerator AnimateMaterialFloat(Material mat, float goal, float rate, float refreshRate)
    {
        float valueToAnimate = mat.GetFloat(shaderVarRef);

        // Gradually decrease the float property until the goal is reached
        while (valueToAnimate > goal)
        {
            valueToAnimate -= rate;
            mat.SetFloat(shaderVarRef, valueToAnimate);
            yield return new WaitForSeconds(refreshRate);
        }
    }
}
