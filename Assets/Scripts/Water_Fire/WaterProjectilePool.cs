using UnityEngine;
using System.Collections.Generic;

public class WaterProjectilePool : MonoBehaviour
{
    [Tooltip("Prefab for the water projectile.")]
    [SerializeField] private GameObject waterProjectilePrefab;
    [Tooltip("Initial number of water projectiles to pre-instantiate.")]
    [SerializeField] private int poolSize = 50;

    private Queue<GameObject> pool = new Queue<GameObject>();

    public static WaterProjectilePool Instance { get; private set; }

    void Awake()
    {
        // Singleton setup.
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Optionally, uncomment the next line if you want the pool to persist across scenes.
        // DontDestroyOnLoad(gameObject);

        // Pre-instantiate pool objects.
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(waterProjectilePrefab, transform);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    /// <summary>
    /// Returns a water projectile from the pool.
    /// If the pool is empty, a new projectile is instantiated.
    /// </summary>
    public GameObject GetProjectile(Vector3 position, Quaternion rotation)
    {
        GameObject projectile;
        if (pool.Count > 0)
        {
            projectile = pool.Dequeue();
            projectile.transform.position = position;
            projectile.transform.rotation = rotation;
            projectile.SetActive(true);
        }
        else
        {
            projectile = Instantiate(waterProjectilePrefab, position, rotation);
        }
        return projectile;
    }

    /// <summary>
    /// Returns a water projectile back to the pool.
    /// </summary>
    public void ReturnProjectile(GameObject projectile)
    {
        projectile.SetActive(false);
        pool.Enqueue(projectile);
    }
}