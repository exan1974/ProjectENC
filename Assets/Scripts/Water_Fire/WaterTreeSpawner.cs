using UnityEngine;

public class WaterTreeSpawner : MonoBehaviour
{
    [Tooltip("Prefab for the tree to instantiate (should include TreeGrowthController).")]
    [SerializeField] private GameObject treePrefab;

    [Tooltip("Layer mask for the ground. Water triggers tree spawning only on ground.")]
    [SerializeField] private LayerMask groundLayerMask;

    [Tooltip("Minimum distance required from any existing tree to spawn a new one.")]
    [SerializeField] private float spawnExclusionRadius = 5f;

    // When water (this object) enters a trigger on the ground...
    private void OnTriggerEnter(Collider other)
    {
        // Check if the collided object belongs to the ground (using the layer mask).
        if ((groundLayerMask.value & (1 << other.gameObject.layer)) != 0)
        {
            Vector3 spawnPosition = transform.position;
            bool canSpawn = true;

            // Verify that the spawn position is outside the exclusion radius of every existing tree.
            foreach (TreeGrowthController tree in TreeGrowthController.allTrees)
            {
                if (Vector3.Distance(tree.transform.position, spawnPosition) < spawnExclusionRadius)
                {
                    canSpawn = false;
                    break;
                }
            }

            if (canSpawn)
            {
                // Instantiate the tree at the water's position.
                Instantiate(treePrefab, spawnPosition, Quaternion.identity);
            }

            // Optionally, destroy the water object after it has hit the ground.
            Destroy(gameObject);
        }
    }
}