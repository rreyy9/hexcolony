using UnityEngine;
using System.Collections.Generic;

public class ResourceManager : MonoBehaviour
{
    // Singleton instance
    public static ResourceManager Instance { get; private set; }

    [Header("Resources")]
    [SerializeField] private int currentWax = 0;
    [SerializeField] private int currentNectar = 0;

    [Header("Worker Settings")]
    [SerializeField] private int workerCost = 30; // Cost in Nectar

    // List of all active workers
    private List<WorkerBee> activeWorkers = new List<WorkerBee>();

    // Accumulators for fractional resource generation
    private float waxAccumulator = 0f;
    private float nectarAccumulator = 0f;

    // Reference to HexGrid for finding tiles
    private HexGrid hexGrid;

    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Find HexGrid in scene
        hexGrid = Object.FindFirstObjectByType<HexGrid>();
        if (hexGrid == null)
        {
            Debug.LogError("HexGrid not found! ResourceManager needs HexGrid reference.");
        }
    }

    void Update()
    {
        // Passive resource generation from workers
        GeneratePassiveResources();
    }

    /// <summary>
    /// Generates resources passively based on active workers.
    /// Uses accumulators to handle fractional amounts properly.
    /// </summary>
    void GeneratePassiveResources()
    {
        if (activeWorkers.Count == 0) return;

        float deltaTime = Time.deltaTime;

        // Accumulate Wax from Hive workers
        foreach (WorkerBee worker in activeWorkers)
        {
            if (worker.assignmentType == WorkerBee.AssignmentType.Hive)
            {
                waxAccumulator += worker.GetGenerationRate() * deltaTime;
            }
            else // Flower
            {
                nectarAccumulator += worker.GetGenerationRate() * deltaTime;
            }
        }

        // Convert accumulated fractional amounts to whole numbers
        if (waxAccumulator >= 1f)
        {
            int wholeWax = Mathf.FloorToInt(waxAccumulator);
            currentWax += wholeWax;
            waxAccumulator -= wholeWax;
        }

        if (nectarAccumulator >= 1f)
        {
            int wholeNectar = Mathf.FloorToInt(nectarAccumulator);
            currentNectar += wholeNectar;
            nectarAccumulator -= wholeNectar;
        }
    }

    // Public getters for UI
    public int CurrentWax => currentWax;
    public int CurrentNectar => currentNectar;
    public int ActiveWorkerCount => activeWorkers.Count;

    // Add resources (from player clicks)
    public void AddWax(int amount)
    {
        currentWax += amount;
        Debug.Log($"Added {amount} Wax. Total: {currentWax}");
    }

    public void AddNectar(int amount)
    {
        currentNectar += amount;
        Debug.Log($"Added {amount} Nectar. Total: {currentNectar}");
    }

    // Check if player has enough Wax for a Connector tile
    public bool CanAffordConnector()
    {
        return currentWax >= 10;
    }

    // Spend Wax, returns true if successful
    public bool SpendWax(int amount)
    {
        if (currentWax >= amount)
        {
            currentWax -= amount;
            Debug.Log($"Spent {amount} Wax. Remaining: {currentWax}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Check if player can afford to spawn a worker.
    /// </summary>
    public bool CanAffordWorker()
    {
        return currentNectar >= workerCost; // Only check if player has enough Nectar
    }

    /// <summary>
    /// Spawns a new worker bee and assigns it automatically.
    /// Returns true if successful, false if can't afford.
    /// </summary>
    public bool SpawnWorker()
    {
        if (!CanAffordWorker())
        {
            Debug.Log($"Cannot spawn worker: Need {workerCost} Nectar.");
            return false;
        }

        // Deduct the cost
        currentNectar -= workerCost;
        Debug.Log($"Spent {workerCost} Nectar. Remaining: {currentNectar}");

        // Auto-assign the worker
        AssignNewWorker();

        return true;
    }

    /// <summary>
    /// Auto-assignment logic: Assigns new worker based on current resource needs.
    /// Priority: 
    /// 1. If Wax < 50 → Assign to Hive
    /// 2. Else if Nectar < 30 → Assign to connected Flower
    /// 3. Else → Distribute evenly
    /// </summary>
    void AssignNewWorker()
    {
        Vector2Int assignedCoord;
        WorkerBee.AssignmentType assignedType;

        // Priority 1: Need Wax?
        if (currentWax < 50)
        {
            assignedCoord = Vector2Int.zero; // Hive is always at (0,0)
            assignedType = WorkerBee.AssignmentType.Hive;
        }
        // Priority 2: Need Nectar?
        else if (currentNectar < 30)
        {
            // Find a connected Flower
            Vector2Int flowerCoord = FindConnectedFlower();
            if (flowerCoord != Vector2Int.zero)
            {
                assignedCoord = flowerCoord;
                assignedType = WorkerBee.AssignmentType.Flower;
            }
            else
            {
                // No connected Flowers, assign to Hive
                assignedCoord = Vector2Int.zero;
                assignedType = WorkerBee.AssignmentType.Hive;
            }
        }
        // Priority 3: Balanced distribution
        else
        {
            // Count current assignments
            int hiveWorkers = 0;
            int flowerWorkers = 0;

            foreach (WorkerBee worker in activeWorkers)
            {
                if (worker.assignmentType == WorkerBee.AssignmentType.Hive)
                    hiveWorkers++;
                else
                    flowerWorkers++;
            }

            // Assign to whichever has fewer workers
            if (hiveWorkers <= flowerWorkers)
            {
                assignedCoord = Vector2Int.zero;
                assignedType = WorkerBee.AssignmentType.Hive;
            }
            else
            {
                Vector2Int flowerCoord = FindConnectedFlower();
                if (flowerCoord != Vector2Int.zero)
                {
                    assignedCoord = flowerCoord;
                    assignedType = WorkerBee.AssignmentType.Flower;
                }
                else
                {
                    assignedCoord = Vector2Int.zero;
                    assignedType = WorkerBee.AssignmentType.Hive;
                }
            }
        }

        // Create the worker
        WorkerBee newWorker = new WorkerBee(assignedCoord, assignedType);
        activeWorkers.Add(newWorker);

        Debug.Log($"Worker #{activeWorkers.Count} spawned! Assigned to {assignedType} at {assignedCoord}");
    }

    /// <summary>
    /// Finds a connected Flower tile to assign a worker to.
    /// Distributes workers evenly across connected Flowers.
    /// Returns Vector2Int.zero if no connected Flowers found.
    /// </summary>
    Vector2Int FindConnectedFlower()
    {
        if (hexGrid == null) return Vector2Int.zero;

        List<Vector2Int> connectedFlowers = hexGrid.GetConnectedFlowers();

        if (connectedFlowers.Count == 0)
        {
            Debug.Log("No connected Flowers found for worker assignment.");
            return Vector2Int.zero;
        }

        // Count how many workers are assigned to each flower
        Dictionary<Vector2Int, int> flowerWorkerCounts = new Dictionary<Vector2Int, int>();

        foreach (Vector2Int flower in connectedFlowers)
        {
            flowerWorkerCounts[flower] = 0;
        }

        foreach (WorkerBee worker in activeWorkers)
        {
            if (worker.assignmentType == WorkerBee.AssignmentType.Flower)
            {
                if (flowerWorkerCounts.ContainsKey(worker.assignedTileCoordinate))
                {
                    flowerWorkerCounts[worker.assignedTileCoordinate]++;
                }
            }
        }

        // Find the flower with the fewest workers
        Vector2Int bestFlower = connectedFlowers[0];
        int minWorkers = int.MaxValue;

        foreach (var kvp in flowerWorkerCounts)
        {
            if (kvp.Value < minWorkers)
            {
                minWorkers = kvp.Value;
                bestFlower = kvp.Key;
            }
        }

        return bestFlower;
    }

    /// <summary>
    /// Returns a read-only list of all active workers.
    /// </summary>
    public List<WorkerBee> GetActiveWorkers()
    {
        return new List<WorkerBee>(activeWorkers);
    }
}