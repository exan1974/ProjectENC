using UnityEngine;

public class WaterTreeSpawner : MonoBehaviour
{
    [Tooltip("List of tree prefabs to instantiate (each should include TreeGrowthController).")]
    [SerializeField] private GameObject[] treePrefabs;

    [Tooltip("Layer mask for the ground. Water triggers tree spawning only on ground.")]
    [SerializeField] private LayerMask groundLayerMask;

    [Tooltip("Minimum distance required from any existing tree to spawn a new one.")]
    [SerializeField] private float spawnExclusionRadius = 5f;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the collided object is on the ground.
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

            if (canSpawn && treePrefabs.Length > 0)
            {
                // Randomly select a tree prefab from the array.
                int randomIndex = Random.Range(0, treePrefabs.Length);
                GameObject selectedPrefab = treePrefabs[randomIndex];

                // Instantiate the selected tree prefab at the water's position.
                Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
            }

            // Optionally, destroy the water object after it has hit the ground.
            Destroy(gameObject);
        }
    }
}
