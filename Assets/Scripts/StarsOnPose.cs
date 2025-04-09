using UnityEngine;
using System.Collections.Generic;

public class StarsOnPose : MonoBehaviour
{
    [Header("Star Settings")]
    [Tooltip("List of points on the pose prefab where stars can be instantiated. " +
             "Make sure these transforms are children of this pose prefab.")]
    public Transform[] starPoints;
    
    [Tooltip("List of star prefabs to choose from.")]
    public GameObject[] starPrefabs;
    
    [Header("Star Configuration")]
    [Tooltip("Stores the configuration of spawned stars. " +
             "Each Vector2Int stores: x = index into starPoints, y = index into starPrefabs.")]
    public List<Vector2Int> starConfiguration = new List<Vector2Int>();

    void Start()
    {
        // If there is no stored configuration (e.g., the first time instantiation), generate one.
        if (starConfiguration == null || starConfiguration.Count == 0)
        {
            GenerateRandomStarConfiguration();
        }
        // Instantiate stars using the stored configuration.
        InstantiateStars();
    }

    /// <summary>
    /// Generates a random star configuration.
    /// Randomly selects between 4 and 7 unique star points and assigns a random star prefab index for each.
    /// The pair (star point index, star prefab index) is stored in starConfiguration.
    /// </summary>
    void GenerateRandomStarConfiguration()
    {
        int starCount = Random.Range(4, 8); // Random number between 4 and 7 (upper bound is exclusive)
        
        // Create a list of available indices for the starPoints array.
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < starPoints.Length; i++)
        {
            availableIndices.Add(i);
        }
        // Shuffle available indices using Fisher–Yates.
        for (int i = availableIndices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = availableIndices[i];
            availableIndices[i] = availableIndices[j];
            availableIndices[j] = temp;
        }
        
        // Ensure we don't select more stars than available points.
        starCount = Mathf.Min(starCount, availableIndices.Count);
        starConfiguration.Clear();
        // For each star, record the star point index and a random star prefab index.
        for (int i = 0; i < starCount; i++)
        {
            int starPointIndex = availableIndices[i];
            int starPrefabIndex = Random.Range(0, starPrefabs.Length);
            starConfiguration.Add(new Vector2Int(starPointIndex, starPrefabIndex));
        }
    }

    /// <summary>
    /// Instantiates star prefabs according to the configuration stored in starConfiguration.
    /// Each configuration item gives the index into starPoints and the starPrefabs arrays.
    /// The star is instantiated as a child of the corresponding star point transform.
    /// </summary>
    void InstantiateStars()
    {
        foreach (Vector2Int config in starConfiguration)
        {
            if (config.x < 0 || config.x >= starPoints.Length)
            {
                Debug.LogWarning("Invalid star point index in configuration: " + config.x);
                continue;
            }
            if (config.y < 0 || config.y >= starPrefabs.Length)
            {
                Debug.LogWarning("Invalid star prefab index in configuration: " + config.y);
                continue;
            }
            Transform spawnPoint = starPoints[config.x];
            GameObject starPrefab = starPrefabs[config.y];
            // Instantiate star at the spawn point’s position and rotation as a child so it follows scaling.
            Instantiate(starPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
        }
    }
}
