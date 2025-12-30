using UnityEngine;

/// <summary>
/// Represents a single worker bee that generates resources passively.
/// Workers can be assigned to either the Hive (generates Wax) or Flowers (generates Nectar).
/// </summary>
public class WorkerBee
{
    // Which tile this worker is assigned to (hex coordinate)
    public Vector2Int assignedTileCoordinate;

    // What type of tile the worker is assigned to
    public enum AssignmentType
    {
        Hive,
        Flower
    }

    public AssignmentType assignmentType;

    // How much resource this worker generates per second
    public float generationRate;

    // Visual representation of the worker (will be added in Phase 6)
    public GameObject visualObject;

    /// <summary>
    /// Constructor: Creates a new worker bee and assigns it to a tile.
    /// </summary>
    public WorkerBee(Vector2Int tileCoord, AssignmentType type)
    {
        assignedTileCoordinate = tileCoord;
        assignmentType = type;

        // Set generation rate based on assignment type
        if (type == AssignmentType.Hive)
        {
            generationRate = 1.0f; // 1 Wax per second
        }
        else // Flower
        {
            generationRate = 0.5f; // 0.5 Nectar per second
        }

        Debug.Log($"Worker created: {type} at {tileCoord}, generates {generationRate}/sec");
    }

    /// <summary>
    /// Returns how much resource this worker generates per second.
    /// </summary>
    public float GetGenerationRate()
    {
        return generationRate;
    }
}