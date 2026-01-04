using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the expansion of flower fields as players connect to them.
/// When the initial 3 flowers are connected, spawns 3 additional flowers in the surrounding area.
/// </summary>
public class FlowerExpansionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HexGrid hexGrid;

    [Header("Expansion Settings")]
    [SerializeField] private int flowersNeededForExpansion = 3;  // How many flowers must be connected to trigger expansion
    [SerializeField] private int newFlowersToSpawn = 3;          // How many new flowers to spawn
    [SerializeField] private int expansionDistance = 1;          // How far from existing flowers (in hex tiles)

    private int lastConnectedCount = 0; // Tracks last known connected flower count
    private int lastTotalFlowerCount = 3; // Tracks total flowers to detect when expansion happens

    void Start()
    {
        // Find HexGrid if not assigned
        if (hexGrid == null)
        {
            hexGrid = FindAnyObjectByType<HexGrid>();
            
            if (hexGrid == null)
            {
                Debug.LogError("FlowerExpansionManager: HexGrid not found in scene!");
            }
        }
    }

    void Update()
    {
        // Only check if we haven't expanded yet
        if (hexGrid != null)
        {
            CheckForExpansion();
        }
    }

    /// <summary>
    /// Checks if enough flowers are connected to trigger expansion.
    /// </summary>
    void CheckForExpansion()
    {
        List<Vector2Int> connectedFlowers = hexGrid.GetConnectedFlowers();
        int connectedCount = connectedFlowers.Count;
        int totalFlowers = hexGrid.GetFlowerPositions().Count;

        // Only log when the count changes (avoid spam)
        if (connectedCount != lastConnectedCount)
        {
            Debug.Log($"[FlowerExpansion] Connected flowers: {connectedCount} / {totalFlowers}");
            lastConnectedCount = connectedCount;
        }

        // Trigger expansion when ALL flowers are connected AND we haven't already spawned for this total
        if (connectedCount >= totalFlowers &&
            connectedCount >= flowersNeededForExpansion &&
            totalFlowers == lastTotalFlowerCount)
        {
            Debug.Log($"[FlowerExpansion] All {totalFlowers} flowers connected! Spawning {newFlowersToSpawn} new flowers.");
            ExpandFlowerFields(connectedFlowers);

            // Update the total flower count so we don't spawn again until the next expansion cycle
            lastTotalFlowerCount = totalFlowers + newFlowersToSpawn;

            // Reset the connected counter
            lastConnectedCount = 0;
        }
    }

    /// <summary>
    /// Spawns new flower fields around existing connected flowers.
    /// </summary>
    void ExpandFlowerFields(List<Vector2Int> existingFlowers)
    {
        Debug.Log($"Expanding flower fields! Spawning {newFlowersToSpawn} new flowers...");

        // Collect all potential spawn positions (positions near existing flowers)
        List<Vector2Int> potentialPositions = new List<Vector2Int>();

        foreach (Vector2Int flowerPos in existingFlowers)
        {
            // Get all positions exactly 'expansionDistance' away from this flower
            List<Vector2Int> positionsAroundFlower = hexGrid.GetHexesAtDistance(flowerPos, expansionDistance);
            
            foreach (Vector2Int pos in positionsAroundFlower)
            {
                // Only add if:
                // 1. Position is valid (within grid bounds)
                // 2. Position doesn't already have a tile
                // 3. Position isn't already in our list
                // Check if position is valid, empty, unique, AND not adjacent to any existing tiles
                if (hexGrid.IsValidPosition(pos) &&
                    !hexGrid.TileExistsAt(pos) &&
                    !potentialPositions.Contains(pos) &&
                    !IsAdjacentToAnyTile(pos))
                {
                    potentialPositions.Add(pos);
                }
            }
        }

        // If we don't have enough valid positions, log a warning
        if (potentialPositions.Count < newFlowersToSpawn)
        {
            Debug.LogWarning($"Only found {potentialPositions.Count} valid positions for {newFlowersToSpawn} flowers!");
        }

        // Shuffle the list to randomize positions (Fisher-Yates shuffle)
        for (int i = 0; i < potentialPositions.Count; i++)
        {
            Vector2Int temp = potentialPositions[i];
            int randomIndex = Random.Range(i, potentialPositions.Count);
            potentialPositions[i] = potentialPositions[randomIndex];
            potentialPositions[randomIndex] = temp;
        }

        // Spawn flowers at the first N positions
        int flowersSpawned = 0;
        for (int i = 0; i < Mathf.Min(newFlowersToSpawn, potentialPositions.Count); i++)
        {
            Vector2Int spawnPos = potentialPositions[i];
            hexGrid.SpawnFlowerTile(spawnPos);
            flowersSpawned++;
            Debug.Log($"Spawned new flower at {spawnPos}");
        }

        Debug.Log($"Flower expansion complete! Spawned {flowersSpawned} new flowers.");
    }

    /// <summary>
    /// Checks if a position is adjacent to ANY existing tile (hive, connector, or flower).
    /// We want new flowers to spawn with a gap, not immediately connected.
    /// </summary>
    private bool IsAdjacentToAnyTile(Vector2Int position)
    {
        List<Vector2Int> neighbors = hexGrid.GetNeighbors(position);

        foreach (Vector2Int neighbor in neighbors)
        {
            if (hexGrid.TileExistsAt(neighbor))
            {
                return true; // Found a tile next to this position
            }
        }

        return false; // No tiles adjacent to this position
    }
}
