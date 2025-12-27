using UnityEngine;
using System.Collections.Generic;

public class HexGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private float hexSize = 0.9f;
    [SerializeField] private int gridRadius = 10;

    [Header("Tile Prefabs")]
    [SerializeField] private GameObject hiveTilePrefab;
    [SerializeField] private GameObject connectorTilePrefab;
    [SerializeField] private GameObject flowerTilePrefab;

    [Header("Flower Settings")]
    [SerializeField] private int numberOfFlowers = 3;
    [SerializeField] private int flowerDistance = 2;

    private Dictionary<Vector2Int, GameObject> spawnedTiles = new Dictionary<Vector2Int, GameObject>();
    private List<Vector2Int> flowerPositions = new List<Vector2Int>();
    private Vector2Int hivePosition = Vector2Int.zero;

    void Awake()
    {
        GenerateFlowerPositions();
        SpawnInitialTiles();
    }

    void GenerateFlowerPositions()
    {
        flowerPositions.Clear();

        List<Vector2Int> availablePositions = GetHexesAtDistance(hivePosition, flowerDistance);

        // Shuffle
        for (int i = 0; i < availablePositions.Count; i++)
        {
            Vector2Int temp = availablePositions[i];
            int randomIndex = Random.Range(i, availablePositions.Count);
            availablePositions[i] = availablePositions[randomIndex];
            availablePositions[randomIndex] = temp;
        }

        // Take first 3
        for (int i = 0; i < numberOfFlowers && i < availablePositions.Count; i++)
        {
            flowerPositions.Add(availablePositions[i]);
        }

        Debug.Log($"Generated {flowerPositions.Count} flower positions at distance {flowerDistance}");
    }

    void SpawnInitialTiles()
    {
        // Spawn Hive at center
        SpawnTile(hivePosition, hiveTilePrefab, "Hive");

        // Spawn all Flowers
        for (int i = 0; i < flowerPositions.Count; i++)
        {
            SpawnTile(flowerPositions[i], flowerTilePrefab, $"Flower_{i + 1}");
        }

        Debug.Log($"Spawned {spawnedTiles.Count} initial tiles (1 Hive + {flowerPositions.Count} Flowers)");
    }

    // Spawn a connector tile at a specific position (called when player builds)
    public void SpawnConnectorTile(Vector2Int coord)
    {
        // Don't spawn if already exists
        if (spawnedTiles.ContainsKey(coord))
        {
            Debug.LogWarning($"Tile already exists at {coord}");
            return;
        }

        // Check if position is valid (within grid radius)
        if (!IsValidPosition(coord))
        {
            Debug.LogWarning($"Position {coord} is outside grid radius");
            return;
        }

        SpawnTile(coord, connectorTilePrefab, $"Connector_{coord.x}_{coord.y}");
    }

    // Internal method to spawn any tile
    void SpawnTile(Vector2Int coord, GameObject prefab, string tileName)
    {
        Vector3 position = AxialToWorld(coord);
        GameObject tile = Instantiate(prefab, position, Quaternion.identity, transform);
        tile.name = tileName;
        spawnedTiles[coord] = tile;
    }

    // Check if a position is within the valid grid
    public bool IsValidPosition(Vector2Int coord)
    {
        return Mathf.Abs(coord.x + coord.y) <= gridRadius;
    }

    // Check if a tile exists at this position
    public bool TileExistsAt(Vector2Int coord)
    {
        return spawnedTiles.ContainsKey(coord);
    }

    // Get neighbors of a hex (useful for placement rules)
    public List<Vector2Int> GetNeighbors(Vector2Int coord)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(1, -1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(-1, 1),
            new Vector2Int(0, 1)
        };

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

    List<Vector2Int> GetHexesAtDistance(Vector2Int center, int distance)
    {
        List<Vector2Int> results = new List<Vector2Int>();

        if (distance == 0)
        {
            results.Add(center);
            return results;
        }

        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(1, -1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(-1, 1),
            new Vector2Int(0, 1)
        };

        Vector2Int current = center + directions[4] * distance;

        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < distance; j++)
            {
                results.Add(current);
                current += directions[i];
            }
        }

        return results;
    }

    Vector3 AxialToWorld(Vector2Int coord)
    {
        float x = hexSize * (3f / 2f * coord.x);
        float z = hexSize * (Mathf.Sqrt(3f) / 2f * coord.x + Mathf.Sqrt(3f) * coord.y);
        return new Vector3(x, 0, z);
    }

    // Convert world position to hex coordinate (useful for clicking)
    public Vector2Int WorldToAxial(Vector3 worldPos)
    {
        float q = (2f / 3f * worldPos.x) / hexSize;
        float r = (-1f / 3f * worldPos.x + Mathf.Sqrt(3f) / 3f * worldPos.z) / hexSize;

        return AxialRound(q, r);
    }

    Vector2Int AxialRound(float q, float r)
    {
        float s = -q - r;

        int rq = Mathf.RoundToInt(q);
        int rr = Mathf.RoundToInt(r);
        int rs = Mathf.RoundToInt(s);

        float q_diff = Mathf.Abs(rq - q);
        float r_diff = Mathf.Abs(rr - r);
        float s_diff = Mathf.Abs(rs - s);

        if (q_diff > r_diff && q_diff > s_diff)
            rq = -rr - rs;
        else if (r_diff > s_diff)
            rr = -rq - rs;

        return new Vector2Int(rq, rr);
    }
}