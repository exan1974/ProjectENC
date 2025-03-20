using UnityEngine;
using System.Collections;

public class MeshTrail : MonoBehaviour
{
    [SerializeField] private float activeTime = 2f;

    [Header("Mesh Related")]
    [SerializeField] private float meshRefreshRate = 0.1f;
    [SerializeField] private float meshDestroyDelay = 3f;
    [SerializeField] private Transform positionToSpawn;

    [Header("Shader Related")]
    [SerializeField] private Material mat;
    [SerializeField] private string shaderVarRef;
    [SerializeField] private float shaderVarRate = 0.1f;
    [SerializeField] private float shaderVarRefreshRate = 0.05f;

    private bool isTrailActive;
    private SkinnedMeshRenderer[] skinnedMeshRenderers;

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown (KeyCode.Space) && !isTrailActive)
        {
            isTrailActive = true;
            StartCoroutine(ActivateTrail(activeTime));
        }
    }

    IEnumerator ActivateTrail(float timeActive)
    {
        while (timeActive > 0)
        {
            timeActive -= meshRefreshRate;

            if (skinnedMeshRenderers == null)
            {
                skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            }

            for (int i=0; i<skinnedMeshRenderers.Length; i++)
            {
                GameObject gO = new GameObject();
                gO.transform.SetPositionAndRotation(positionToSpawn.position, positionToSpawn.rotation);

                MeshRenderer mR = gO.AddComponent<MeshRenderer>();
                MeshFilter mF = gO.AddComponent<MeshFilter>();

                Mesh mesh = new Mesh();
                skinnedMeshRenderers[i].BakeMesh(mesh);

                mF.mesh = mesh;
                mR.material = mat;

                StartCoroutine(AnimateMaterialFloat(mR.material, 0, shaderVarRate, shaderVarRefreshRate));

                Destroy(gO, meshDestroyDelay);
            }

            yield return new WaitForSeconds(meshRefreshRate);
        }

        isTrailActive = false;
    }

    IEnumerator AnimateMaterialFloat (Material mat, float goal, float rate, float refreshRate)
    {
        float valueToAnimate = mat.GetFloat(shaderVarRef);

        while (valueToAnimate > goal)
        {
            valueToAnimate -= rate;
            mat.SetFloat(shaderVarRef, valueToAnimate);
            yield return new WaitForSeconds(refreshRate);
        }
    }
}
