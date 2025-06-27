using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class BackgroundSetup : MonoBehaviour
{
    [Header("Background Settings")]
    [SerializeField] private Material backgroundMaterial;
    [SerializeField] private float zPosition = 0f;
    [SerializeField] private Vector2 scale = new Vector2(20f, 20f);

    private void Awake()
    {
        // Set layer
        gameObject.layer = LayerMask.NameToLayer("Background");
        
        // Set position and scale
        transform.position = new Vector3(0f, 0f, zPosition);
        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(scale.x, scale.y, 1f);

        // Set up renderer
        var renderer = GetComponent<MeshRenderer>();
        if (backgroundMaterial != null)
        {
            renderer.material = backgroundMaterial;
        }
        
        // Set sorting layer
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = 0;
    }
} 