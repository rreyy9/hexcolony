using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Handles the visual representation of worker bees using a swarm-to-giant-bee system.
/// 1-5 workers per tile: Individual bees in a circular swarm pattern with independent movement
/// 6+ workers per tile: Single giant CUBE that grows infinitely and rotates in place
/// </summary>
public class WorkerVisualizer : MonoBehaviour
{
    [Header("Swarm Settings")]
    [SerializeField] private int swarmThreshold = 5; // Merge happens at 6th worker
    [SerializeField] private float individualBeeScale = 0.3f;
    [SerializeField] private float swarmOrbitRadius = 0.5f; // Circular movement radius
    [SerializeField] private float swarmOrbitSpeed = 30f; // Degrees per second for orbit

    [Header("Giant Bee Settings")]
    [SerializeField] private float giantBeeBaseScale = 0.5f;
    [SerializeField] private float giantBeeGrowthRate = 0.1f;
    [SerializeField] private float giantBeeRotationSpeed = 20f; // Degrees per second

    [Header("Animation Settings")]
    [SerializeField] private float heightAboveTile = 0.5f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobAmount = 0.2f;

    [Header("Colors")]
    [SerializeField] private Color hiveWorkerColor = Color.yellow;
    [SerializeField] private Color flowerWorkerColor = Color.magenta;

    // References
    private HexGrid hexGrid;
    private ResourceManager resourceManager;

    // Tracking dictionaries for per-tile visualization
    private Dictionary<Vector2Int, List<GameObject>> individualBeesByTile = new Dictionary<Vector2Int, List<GameObject>>();
    private Dictionary<Vector2Int, GameObject> giantBeeByTile = new Dictionary<Vector2Int, GameObject>();

    void Start()
    {
        hexGrid = Object.FindFirstObjectByType<HexGrid>();
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
        if (resourceManager == null || hexGrid == null) return;

        // Update visualization based on current worker state
        UpdateWorkerVisualizations();
    }

    /// <summary>
    /// Main update loop: Groups workers by tile and updates visualization accordingly.
    /// </summary>
    void UpdateWorkerVisualizations()
    {
        // Get all active workers
        var workers = resourceManager.GetActiveWorkers();

        // Group workers by their assigned tile coordinate
        var workersByTile = workers.GroupBy(w => w.assignedTileCoordinate)
                                   .ToDictionary(g => g.Key, g => g.ToList());

        // Process each tile
        foreach (var kvp in workersByTile)
        {
            Vector2Int tileCoord = kvp.Key;
            List<WorkerBee> workersOnTile = kvp.Value;
            int workerCount = workersOnTile.Count;

            // Determine visualization mode
            if (workerCount <= swarmThreshold)
            {
                // Individual swarm mode
                UpdateIndividualSwarm(tileCoord, workersOnTile);
            }
            else
            {
                // Giant bee mode (cube)
                UpdateGiantBee(tileCoord, workersOnTile);
            }
        }

        // Clean up tiles with no workers
        CleanupEmptyTiles(workersByTile);
    }

    /// <summary>
    /// Updates individual bee swarm for tiles with ≤5 workers.
    /// Each bee orbits in a circular pattern with out-of-sync bobbing.
    /// </summary>
    void UpdateIndividualSwarm(Vector2Int tileCoord, List<WorkerBee> workers)
    {
        // Ensure we have no giant bee on this tile
        if (giantBeeByTile.ContainsKey(tileCoord))
        {
            Destroy(giantBeeByTile[tileCoord]);
            giantBeeByTile.Remove(tileCoord);
        }

        // Initialize list if needed
        if (!individualBeesByTile.ContainsKey(tileCoord))
        {
            individualBeesByTile[tileCoord] = new List<GameObject>();
        }

        List<GameObject> beeVisuals = individualBeesByTile[tileCoord];

        // Create or update individual bees
        for (int i = 0; i < workers.Count; i++)
        {
            WorkerBee worker = workers[i];

            // Create new bee if needed
            if (i >= beeVisuals.Count)
            {
                GameObject bee = CreateIndividualBee(worker, i);
                beeVisuals.Add(bee);
            }

            // Update position and animation with circular orbit
            AnimateIndividualBeeWithOrbit(beeVisuals[i], tileCoord, i, worker.assignmentType);
        }

        // Remove excess bees if worker count decreased (shouldn't happen in MVP, but safe)
        while (beeVisuals.Count > workers.Count)
        {
            GameObject excessBee = beeVisuals[beeVisuals.Count - 1];
            beeVisuals.RemoveAt(beeVisuals.Count - 1);
            Destroy(excessBee);
        }
    }

    /// <summary>
    /// Creates a single individual bee GameObject.
    /// </summary>
    GameObject CreateIndividualBee(WorkerBee worker, int workerIndex)
    {
        GameObject bee = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bee.transform.localScale = Vector3.one * individualBeeScale;

        // Remove collider (not needed)
        Collider collider = bee.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        // Set color based on assignment
        Renderer renderer = bee.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color beeColor = worker.assignmentType == WorkerBee.AssignmentType.Hive
                ? hiveWorkerColor
                : flowerWorkerColor;

            renderer.material.color = beeColor;
        }

        // Parent to this object for organization
        bee.transform.SetParent(transform);
        bee.name = $"IndividualBee_{worker.assignmentType}_{worker.assignedTileCoordinate}_{workerIndex}";

        return bee;
    }

    /// <summary>
    /// Animates an individual bee with circular orbit movement and out-of-sync bobbing.
    /// Creates a natural "swarming" appearance.
    /// </summary>
    void AnimateIndividualBeeWithOrbit(GameObject bee, Vector2Int tileCoord, int workerIndex, WorkerBee.AssignmentType assignmentType)
    {
        // Get tile world position (center of orbit)
        Vector3 tilePosition = hexGrid.AxialToWorld(tileCoord);

        // Calculate unique orbit angle for this bee (deterministic based on index)
        // Each bee starts at a different position on the circle
        float baseAngle = workerIndex * (360f / 5f); // Distribute evenly around circle

        // Add time-based rotation for circular movement
        float currentAngle = baseAngle + (Time.time * swarmOrbitSpeed);

        // Convert angle to radians for circular position
        float angleRad = currentAngle * Mathf.Deg2Rad;

        // Calculate position on circle (XZ plane)
        float xOffset = Mathf.Cos(angleRad) * swarmOrbitRadius;
        float zOffset = Mathf.Sin(angleRad) * swarmOrbitRadius;
        Vector3 orbitOffset = new Vector3(xOffset, 0, zOffset);

        // Calculate out-of-sync bobbing with unique phase offset
        float phaseOffset = workerIndex * 0.5f;
        float bobOffset = Mathf.Sin((Time.time + phaseOffset) * bobSpeed) * bobAmount;

        // Combine all movements: base position + circular orbit + vertical bobbing
        Vector3 finalPosition = tilePosition + orbitOffset + Vector3.up * (heightAboveTile + bobOffset);

        bee.transform.position = finalPosition;
    }

    /// <summary>
    /// Updates giant bee visualization for tiles with >5 workers.
    /// Giant bee is a CUBE that grows with worker count and rotates in place (no bobbing).
    /// </summary>
    void UpdateGiantBee(Vector2Int tileCoord, List<WorkerBee> workers)
    {
        int workerCount = workers.Count;

        // Destroy all individual bees on this tile (transition to giant bee)
        if (individualBeesByTile.ContainsKey(tileCoord))
        {
            foreach (GameObject bee in individualBeesByTile[tileCoord])
            {
                Destroy(bee);
            }
            individualBeesByTile[tileCoord].Clear();
        }

        // Create giant bee if it doesn't exist
        if (!giantBeeByTile.ContainsKey(tileCoord))
        {
            WorkerBee firstWorker = workers[0]; // Use first worker for assignment type
            GameObject giantBee = CreateGiantBeeCube(tileCoord, firstWorker.assignmentType);
            giantBeeByTile[tileCoord] = giantBee;
        }

        // Update giant bee scale and animation
        GameObject giant = giantBeeByTile[tileCoord];
        UpdateGiantBeeScale(giant, workerCount);
        AnimateGiantBeeCube(giant, tileCoord);
    }

    /// <summary>
    /// Creates a giant bee CUBE GameObject with darkened color.
    /// </summary>
    GameObject CreateGiantBeeCube(Vector2Int tileCoord, WorkerBee.AssignmentType assignmentType)
    {
        // Create CUBE instead of sphere
        GameObject giantBee = GameObject.CreatePrimitive(PrimitiveType.Cube);

        // Remove collider
        Collider collider = giantBee.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        // Set darkened color (multiply by 0.7 for darker variant)
        Renderer renderer = giantBee.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color baseColor = assignmentType == WorkerBee.AssignmentType.Hive
                ? hiveWorkerColor
                : flowerWorkerColor;

            Color darkenedColor = baseColor * 0.7f;
            renderer.material.color = darkenedColor;
        }

        // Parent to this object
        giantBee.transform.SetParent(transform);
        giantBee.name = $"GiantBeeCube_{assignmentType}_{tileCoord}";

        return giantBee;
    }

    /// <summary>
    /// Updates giant bee scale based on worker count.
    /// Formula: baseScale + (workerCount - threshold) * growthRate
    /// </summary>
    void UpdateGiantBeeScale(GameObject giantBee, int workerCount)
    {
        float scale = giantBeeBaseScale + (workerCount - swarmThreshold) * giantBeeGrowthRate;
        giantBee.transform.localScale = Vector3.one * scale;
    }

    /// <summary>
    /// Animates giant bee CUBE with rotation only (NO bobbing, NO movement).
    /// Cube stays at fixed height and just rotates on Y axis.
    /// </summary>
    void AnimateGiantBeeCube(GameObject giantBee, Vector2Int tileCoord)
    {
        // Get tile world position
        Vector3 tilePosition = hexGrid.AxialToWorld(tileCoord);

        // Fixed position - NO bobbing animation
        giantBee.transform.position = tilePosition + Vector3.up * heightAboveTile;

        // Rotate around Y axis only
        giantBee.transform.Rotate(Vector3.up, giantBeeRotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Cleans up visualization for tiles that no longer have workers.
    /// </summary>
    void CleanupEmptyTiles(Dictionary<Vector2Int, List<WorkerBee>> activeWorkersByTile)
    {
        // Find tiles with visualizations but no workers
        List<Vector2Int> tilesToCleanup = new List<Vector2Int>();

        // Check individual bees
        foreach (var kvp in individualBeesByTile)
        {
            if (!activeWorkersByTile.ContainsKey(kvp.Key))
            {
                tilesToCleanup.Add(kvp.Key);
            }
        }

        // Check giant bees
        foreach (var kvp in giantBeeByTile)
        {
            if (!activeWorkersByTile.ContainsKey(kvp.Key))
            {
                tilesToCleanup.Add(kvp.Key);
            }
        }

        // Cleanup
        foreach (Vector2Int tileCoord in tilesToCleanup)
        {
            // Destroy individual bees
            if (individualBeesByTile.ContainsKey(tileCoord))
            {
                foreach (GameObject bee in individualBeesByTile[tileCoord])
                {
                    Destroy(bee);
                }
                individualBeesByTile.Remove(tileCoord);
            }

            // Destroy giant bee
            if (giantBeeByTile.ContainsKey(tileCoord))
            {
                Destroy(giantBeeByTile[tileCoord]);
                giantBeeByTile.Remove(tileCoord);
            }
        }
    }
}