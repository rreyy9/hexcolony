using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the hexagonal grid system for the bee colony game.
/// Handles tile spawning, coordinate conversions, and grid layout.
/// </summary>
public class HexGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private float hexSize = 0.9f;       // Size of each hex tile (controls spacing)
    [SerializeField] private int gridRadius = 10;        // Maximum distance from center (0,0)

    [Header("Tile Prefabs")]
    [SerializeField] private GameObject hiveTilePrefab;
    [SerializeField] private GameObject connectorTilePrefab;
    [SerializeField] private GameObject flowerTilePrefab;

    [Header("Flower Settings")]
    [SerializeField] private int numberOfFlowers = 3;    // How many flowers to spawn
    [SerializeField] private int flowerDistance = 2;     // How far from hive (in hex tiles)

    // Dictionary stores all spawned tiles using their grid coordinates as the key
    private Dictionary<Vector2Int, GameObject> spawnedTiles = new Dictionary<Vector2Int, GameObject>();
    
    // Stores the random positions where flowers will spawn this game
    private List<Vector2Int> flowerPositions = new List<Vector2Int>();
    
    // Hive always starts at the center of the grid (0, 0)
    private Vector2Int hivePosition = Vector2Int.zero;

    /// <summary>
    /// Called when the game object is created. Sets up the initial game board.
    /// </summary>
    void Awake()
    {
        GenerateFlowerPositions();  // Randomly decide where flowers will appear
        SpawnInitialTiles();        // Create the hive and flower tiles
    }

    /// <summary>
    /// Randomly selects positions for flowers at a specific distance from the hive.
    /// Uses a shuffle algorithm to pick random spots from all possible positions.
    /// </summary>
    void GenerateFlowerPositions()
    {
        flowerPositions.Clear();

        // Get all hexes that are exactly 'flowerDistance' away from hive
        List<Vector2Int> availablePositions = GetHexesAtDistance(hivePosition, flowerDistance);

        // Fisher-Yates shuffle - randomizes the list of available positions
        for (int i = 0; i < availablePositions.Count; i++)
        {
            Vector2Int temp = availablePositions[i];
            int randomIndex = Random.Range(i, availablePositions.Count);
            availablePositions[i] = availablePositions[randomIndex];
            availablePositions[randomIndex] = temp;
        }

        // Take the first N positions from the shuffled list (where N = numberOfFlowers)
        for (int i = 0; i < numberOfFlowers && i < availablePositions.Count; i++)
        {
            flowerPositions.Add(availablePositions[i]);
        }

        Debug.Log($"Generated {flowerPositions.Count} flower positions at distance {flowerDistance}");
    }

    /// <summary>
    /// Creates the starting tiles: one hive at center and flowers at random positions.
    /// Only spawns the minimum needed tiles to keep performance high.
    /// </summary>
    void SpawnInitialTiles()
    {
        // Spawn Hive at center (0, 0)
        SpawnTile(hivePosition, hiveTilePrefab, "Hive");

        // Spawn all Flowers at their randomly chosen positions
        for (int i = 0; i < flowerPositions.Count; i++)
        {
            SpawnTile(flowerPositions[i], flowerTilePrefab, $"Flower_{i + 1}");
        }

        Debug.Log($"Spawned {spawnedTiles.Count} initial tiles (1 Hive + {flowerPositions.Count} Flowers)");
    }

    /// <summary>
    /// Public method to spawn a connector tile at the specified grid coordinate.
    /// Called when the player clicks to build a new pathway tile.
    /// Includes validation to prevent duplicate tiles or invalid positions.
    /// </summary>
    public void SpawnConnectorTile(Vector2Int coord)
    {
        // Don't allow placing a tile where one already exists
        if (spawnedTiles.ContainsKey(coord))
        {
            Debug.LogWarning($"Tile already exists at {coord}");
            return;
        }

        // Don't allow placing tiles outside the grid boundaries
        if (!IsValidPosition(coord))
        {
            Debug.LogWarning($"Position {coord} is outside grid radius");
            return;
        }

        SpawnTile(coord, connectorTilePrefab, $"Connector_{coord.x}_{coord.y}");
    }

    /// <summary>
    /// Internal helper method that handles the actual spawning of any tile type.
    /// Converts grid coordinates to world position, instantiates the prefab,
    /// and adds it to the spawned tiles dictionary.
    /// </summary>
    void SpawnTile(Vector2Int coord, GameObject prefab, string tileName)
    {
        Vector3 position = AxialToWorld(coord);
        GameObject tile = Instantiate(prefab, position, Quaternion.identity, transform);
        tile.name = tileName;
        spawnedTiles[coord] = tile;

        // Set up TileClickHandler if it exists
        TileClickHandler clickHandler = tile.GetComponent<TileClickHandler>();
        if (clickHandler != null)
        {
            clickHandler.SetTileCoordinate(coord);
            clickHandler.SetHexGrid(this);
        }
    }

    /// <summary>
    /// Checks if a grid coordinate is within the valid playing area.
    /// Uses the axial coordinate constraint: |q + r| <= radius
    /// This creates a circular/hexagonal boundary around the center.
    /// </summary>
    public bool IsValidPosition(Vector2Int coord)
    {
        return Mathf.Abs(coord.x + coord.y) <= gridRadius;
    }

    /// <summary>
    /// Quick lookup to see if a tile has been spawned at a specific coordinate.
    /// Useful for checking before placing new tiles or for pathfinding.
    /// </summary>
    public bool TileExistsAt(Vector2Int coord)
    {
        return spawnedTiles.ContainsKey(coord);
    }

    /// <summary>
    /// Returns all 6 neighboring hex coordinates around a given position.
    /// Hexagons have exactly 6 neighbors in the axial coordinate system.
    /// Only returns neighbors that are within the valid grid boundaries.
    /// </summary>
    public List<Vector2Int> GetNeighbors(Vector2Int coord)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        // The 6 directional offsets for hexagonal neighbors in axial coordinates
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),    // East
            new Vector2Int(1, -1),   // Northeast
            new Vector2Int(0, -1),   // Northwest
            new Vector2Int(-1, 0),   // West
            new Vector2Int(-1, 1),   // Southwest
            new Vector2Int(0, 1)     // Southeast
        };

        // Check each direction and add valid neighbors to the list
        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighbor = coord + dir;
            if (IsValidPosition(neighbor))
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    /// <summary>
    /// Gets all hex coordinates that form a ring at an exact distance from center.
    /// Used to find all possible flower spawn positions at a specific distance.
    /// Returns a list of coordinates forming a hexagonal ring shape.
    /// </summary>
    public List<Vector2Int> GetHexesAtDistance(Vector2Int center, int distance)
    {
        List<Vector2Int> results = new List<Vector2Int>();

        // Special case: distance 0 means just the center itself
        if (distance == 0)
        {
            results.Add(center);
            return results;
        }

        // Same 6 hex directions as GetNeighbors
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(1, -1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(-1, 1),
            new Vector2Int(0, 1)
        };

        // Start at one corner of the ring (distance away in the southwest direction)
        Vector2Int current = center + directions[4] * distance;

        // Walk around the ring: 6 sides, each side has 'distance' hexes
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < distance; j++)
            {
                results.Add(current);
                current += directions[i];  // Move to next hex along this side
            }
        }

        return results;
    }

    /// <summary>
    /// Converts axial hex coordinates (q, r) to 3D world position (x, y, z).
    /// Uses flat-top hexagon math to calculate proper spacing.
    /// The y-value is always 0 since tiles lay flat on the ground.
    /// </summary>
    public Vector3 AxialToWorld(Vector2Int coord)
    {
        float x = hexSize * (3f / 2f * coord.x);
        float z = hexSize * (Mathf.Sqrt(3f) / 2f * coord.x + Mathf.Sqrt(3f) * coord.y);
        return new Vector3(x, 0, z);
    }

    /// <summary>
    /// Converts a 3D world position back to axial hex coordinates.
    /// Useful for determining which hex was clicked by the player.
    /// Uses fractional coordinates then rounds to nearest valid hex.
    /// </summary>
    public Vector2Int WorldToAxial(Vector3 worldPos)
    {
        // Inverse of the AxialToWorld calculation
        float q = (2f / 3f * worldPos.x) / hexSize;
        float r = (-1f / 3f * worldPos.x + Mathf.Sqrt(3f) / 3f * worldPos.z) / hexSize;

        return AxialRound(q, r);  // Round fractional coords to nearest hex
    }

    /// <summary>
    /// Rounds fractional axial coordinates to the nearest valid hex coordinate.
    /// Uses cube coordinates (q, r, s) where q + r + s = 0 for accurate rounding.
    /// This ensures clicks always map to the correct hex, even at edges.
    /// </summary>
    Vector2Int AxialRound(float q, float r)
    {
        float s = -q - r;  // Calculate third cube coordinate

        // Round all three coordinates
        int rq = Mathf.RoundToInt(q);
        int rr = Mathf.RoundToInt(r);
        int rs = Mathf.RoundToInt(s);

        // Calculate how much each coordinate changed during rounding
        float q_diff = Mathf.Abs(rq - q);
        float r_diff = Mathf.Abs(rr - r);
        float s_diff = Mathf.Abs(rs - s);

        // Fix the coordinate that changed the most to maintain q + r + s = 0
        if (q_diff > r_diff && q_diff > s_diff)
            rq = -rr - rs;
        else if (r_diff > s_diff)
            rr = -rq - rs;

        return new Vector2Int(rq, rr);  // Return as axial coordinates (only need q and r)
    }

    /// <summary>
    /// Checks if a tile at the given coordinate is connected to the Hive using BFS.
    /// Public method that can be called by other scripts (TileClickHandler, TileHoverDetector).
    /// </summary>
    public bool IsTileConnectedToHive(Vector2Int coord)
    {
        // The Hive itself is always "connected"
        if (coord == Vector2Int.zero) return true;

        // If there's no tile at this coordinate, it's not connected
        if (!TileExistsAt(coord)) return false;

        // Use BFS (Breadth-First Search) to find path to Hive
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(coord);
        visited.Add(coord);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            // Check all neighbors of current tile
            foreach (Vector2Int neighbor in GetNeighbors(current))
            {
                // Found the Hive! This tile is connected
                if (neighbor == Vector2Int.zero) return true;

                // If neighbor has a tile and we haven't visited it yet
                if (TileExistsAt(neighbor) && !visited.Contains(neighbor))
                {
                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                }
            }
        }

        // Exhausted all paths, no connection to Hive found
        return false;
    }

    /// <summary>
    /// Returns a list of all Flower tile coordinates that are currently connected to the Hive.
    /// </summary>
    public List<Vector2Int> GetConnectedFlowers()
    {
        List<Vector2Int> connectedFlowers = new List<Vector2Int>();

        foreach (Vector2Int flowerPos in flowerPositions)
        {
            if (IsTileConnectedToHive(flowerPos))
            {
                connectedFlowers.Add(flowerPos);
            }
        }

        return connectedFlowers;
    }

    /// <summary>
    /// Public getter for all flower positions (connected or not).
    /// </summary>
    public List<Vector2Int> GetAllFlowerPositions()
    {
        return new List<Vector2Int>(flowerPositions);
    }

    /// <summary>
    /// Public method to spawn a flower tile at the specified grid coordinate.
    /// Used by FlowerExpansionManager to create expansion flowers.
    /// </summary>
    public void SpawnFlowerTile(Vector2Int coord)
    {
        // Don't allow placing a tile where one already exists
        if (spawnedTiles.ContainsKey(coord))
        {
            Debug.LogWarning($"Tile already exists at {coord}");
            return;
        }

        // Don't allow placing tiles outside the grid boundaries
        if (!IsValidPosition(coord))
        {
            Debug.LogWarning($"Position {coord} is outside grid radius");
            return;
        }

        // Use existing flowerPositions count to name the new flower
        SpawnTile(coord, flowerTilePrefab, $"Flower_{flowerPositions.Count + 1}");

        // Add to the flower positions list so we track all flowers
        flowerPositions.Add(coord);
    }

    /// <summary>
    /// Returns a copy of all flower positions spawned in the game.
    /// Used by FlowerExpansionManager to find expansion positions.
    /// </summary>
    public List<Vector2Int> GetFlowerPositions()
    {
        return new List<Vector2Int>(flowerPositions);
    }
}