using UnityEngine;

/// <summary>
/// Handles the visual representation of worker bees.
/// Creates simple sprites that bob up and down above their assigned tiles.
/// </summary>
public class WorkerVisualizer : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private GameObject workerPrefab;
    [SerializeField] private float heightAboveTile = 0.5f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobAmount = 0.2f;

    [Header("Colors")]
    [SerializeField] private Color hiveWorkerColor = Color.yellow;
    [SerializeField] private Color flowerWorkerColor = Color.magenta;

    private HexGrid hexGrid;
    private ResourceManager resourceManager;

    // Track which workers we've already visualized
    private int lastVisualizedCount = 0;  // FIXED: Removed space in variable name

    void Start()
    {
        hexGrid = FindObjectOfType<HexGrid>();
        resourceManager = ResourceManager.Instance;

        if (hexGrid == null)
        {
            Debug.LogError("HexGrid not found!");
        }

        if (resourceManager == null)
        {
            Debug.LogError("ResourceManager not found!");
        }
    }

    void Update()
    {
        // Check if new workers have been spawned
        if (resourceManager != null && resourceManager.ActiveWorkerCount > lastVisualizedCount)
        {
            UpdateWorkerVisuals();
            lastVisualizedCount = resourceManager.ActiveWorkerCount;
        }

        // Animate existing worker visuals
        AnimateWorkers();
    }

    /// <summary>
    /// Creates or updates visual representations for all active workers.
    /// </summary>
    void UpdateWorkerVisuals()
    {
        if (resourceManager == null || hexGrid == null) return;

        // Get all active workers from ResourceManager
        var workers = resourceManager.GetActiveWorkers();

        foreach (WorkerBee worker in workers)
        {
            // Skip if this worker already has a visual
            if (worker.visualObject != null) continue;

            // Create visual for this worker
            CreateWorkerVisual(worker);
        }
    }

    /// <summary>
    /// Creates a visual GameObject for a worker bee.
    /// </summary>
    void CreateWorkerVisual(WorkerBee worker)
    {
        if (workerPrefab == null)
        {
            // Create a simple sphere if no prefab is assigned
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.transform.localScale = Vector3.one * 0.3f;

            // Remove collider (we don't need it)
            Collider collider = visual.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            worker.visualObject = visual;
        }
        else
        {
            worker.visualObject = Instantiate(workerPrefab);
        }

        // Position the worker above its assigned tile
        Vector3 tilePosition = hexGrid.AxialToWorld(worker.assignedTileCoordinate);
        worker.visualObject.transform.position = tilePosition + Vector3.up * heightAboveTile;

        // Set color based on assignment type
        Renderer renderer = worker.visualObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color workerColor = worker.assignmentType == WorkerBee.AssignmentType.Hive
                ? hiveWorkerColor
                : flowerWorkerColor;

            renderer.material.color = workerColor;
        }

        // Parent to this object for organization
        worker.visualObject.transform.SetParent(transform);
        worker.visualObject.name = $"Worker_{worker.assignmentType}_{worker.assignedTileCoordinate}";

        Debug.Log($"Created visual for worker at {worker.assignedTileCoordinate}");
    }

    /// <summary>
    /// Animates all worker visuals with a gentle bobbing motion.
    /// </summary>
    void AnimateWorkers()
    {
        if (resourceManager == null) return;

        var workers = resourceManager.GetActiveWorkers();

        foreach (WorkerBee worker in workers)
        {
            if (worker.visualObject == null) continue;

            // Calculate bobbing offset
            float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmount;

            // Update position
            Vector3 tilePosition = hexGrid.AxialToWorld(worker.assignedTileCoordinate);
            worker.visualObject.transform.position = tilePosition + Vector3.up * (heightAboveTile + bobOffset);
        }
    }
}