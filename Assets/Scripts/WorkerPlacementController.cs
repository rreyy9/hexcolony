using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Handles player-controlled worker placement system.
/// When activated, shows valid spawn locations (Hive + connected Flowers)
/// and lets the player choose where to spawn the worker.
/// </summary>
public class WorkerPlacementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject highlightPrefab; // HighlightTile prefab
    [SerializeField] private Material highlightNormalMaterial; // Green semi-transparent
    [SerializeField] private Material highlightHoverMaterial; // Bright white with emission

    [Header("Settings")]
    [SerializeField] private LayerMask tileLayerMask; // Layer for detecting tiles

    private HexGrid hexGrid;
    private ResourceManager resourceManager;
    private Camera mainCamera;

    // Placement mode state
    private bool isPlacementModeActive = false;

    // Highlight tracking
    private Dictionary<Vector2Int, GameObject> spawnLocationHighlights = new Dictionary<Vector2Int, GameObject>();
    private GameObject currentHoveredHighlight = null;

    // Input
    private Mouse mouse;

    void Awake()
    {
        // Get references that don't depend on other scripts
        mainCamera = Camera.main;
        mouse = Mouse.current;
    }

    void Start()
    {
        // Get references to other scripts in Start() to ensure they're initialized
        hexGrid = FindFirstObjectByType<HexGrid>();
        resourceManager = ResourceManager.Instance;

        // Debug check
        if (resourceManager == null)
        {
            Debug.LogError("ResourceManager.Instance is null! Make sure ResourceManager exists in the scene.");
        }

        if (hexGrid == null)
        {
            Debug.LogError("HexGrid not found in scene!");
        }
    }

    void Update()
    {
        if (!isPlacementModeActive || mouse == null) return;

        // Detect hover over spawn locations
        DetectHover();

        // Check for clicks
        if (mouse.leftButton.wasPressedThisFrame)
        {
            HandleLeftClick();
        }

        if (mouse.rightButton.wasPressedThisFrame)
        {
            HandleRightClick();
        }
    }

    /// <summary>
    /// Activates worker placement mode - called by UIManager when spawn button is clicked
    /// </summary>
    public void ActivatePlacementMode()
    {
        if (resourceManager == null || !resourceManager.CanAffordWorker())
        {
            Debug.Log("Cannot afford worker!");
            return;
        }

        isPlacementModeActive = true;
        ShowSpawnLocations();
        Debug.Log("Worker Placement Mode ACTIVATED - Choose a location!");
    }

    /// <summary>
    /// Shows highlight GameObjects on all valid spawn locations
    /// </summary>
    void ShowSpawnLocations()
    {
        // Clear any existing highlights
        ClearHighlights();

        // Always show Hive (0,0) as a valid location
        CreateHighlight(Vector2Int.zero);

        // Show all connected Flowers
        List<Vector2Int> connectedFlowers = hexGrid.GetConnectedFlowers();
        foreach (Vector2Int flowerCoord in connectedFlowers)
        {
            CreateHighlight(flowerCoord);
        }

        Debug.Log($"Showing {spawnLocationHighlights.Count} spawn locations");
    }

    /// <summary>
    /// Creates a highlight GameObject at the specified tile coordinate
    /// </summary>
    void CreateHighlight(Vector2Int tileCoord)
    {
        // Get world position for this tile
        Vector3 worldPos = hexGrid.AxialToWorld(tileCoord);
        worldPos.y += 0.01f; // Slightly above tile to prevent z-fighting

        // Spawn highlight
        GameObject highlight = Instantiate(highlightPrefab, worldPos, Quaternion.identity);

        // Set normal material
        MeshRenderer renderer = highlight.GetComponent<MeshRenderer>();
        if (renderer != null && highlightNormalMaterial != null)
        {
            renderer.material = highlightNormalMaterial;
        }

        // Store reference
        spawnLocationHighlights[tileCoord] = highlight;
    }

    /// <summary>
    /// Detects which spawn location highlight the player is hovering over
    /// </summary>
    void DetectHover()
    {
        if (mouse == null) return;

        Vector2 mousePos = mouse.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        // Reset previous highlight
        if (currentHoveredHighlight != null)
        {
            MeshRenderer renderer = currentHoveredHighlight.GetComponent<MeshRenderer>();
            if (renderer != null && highlightNormalMaterial != null)
            {
                renderer.material = highlightNormalMaterial;
            }
            currentHoveredHighlight = null;
        }

        // Raycast to detect tile
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayerMask))
        {
            // Convert world position to hex coordinate
            Vector2Int hoveredCoord = hexGrid.WorldToAxial(hit.point);

            // Check if this is a valid spawn location
            if (spawnLocationHighlights.ContainsKey(hoveredCoord))
            {
                GameObject highlight = spawnLocationHighlights[hoveredCoord];

                // Apply hover material
                MeshRenderer renderer = highlight.GetComponent<MeshRenderer>();
                if (renderer != null && highlightHoverMaterial != null)
                {
                    renderer.material = highlightHoverMaterial;
                }

                currentHoveredHighlight = highlight;
            }
        }
    }

    /// <summary>
    /// Handles left click during placement mode
    /// </summary>
    void HandleLeftClick()
    {
        if (!isPlacementModeActive) return;

        // Only proceed if hovering over a valid location
        if (currentHoveredHighlight == null) return;

        // Find which coordinate was clicked
        Vector2Int selectedCoord = Vector2Int.zero;
        foreach (var kvp in spawnLocationHighlights)
        {
            if (kvp.Value == currentHoveredHighlight)
            {
                selectedCoord = kvp.Key;
                break;
            }
        }

        // Spawn the worker at this location
        SpawnWorkerAtLocation(selectedCoord);

        // Exit placement mode
        CancelPlacement();
    }

    /// <summary>
    /// Handles right click during placement mode - cancels placement
    /// </summary>
    void HandleRightClick()
    {
        if (isPlacementModeActive)
        {
            CancelPlacement();
            Debug.Log("Worker placement cancelled");
        }
    }

    /// <summary>
    /// Spawns a worker at the specified location
    /// </summary>
    void SpawnWorkerAtLocation(Vector2Int coord)
    {
        // Determine assignment type based on coordinate
        WorkerBee.AssignmentType assignmentType;

        if (coord == Vector2Int.zero)
        {
            assignmentType = WorkerBee.AssignmentType.Hive;
        }
        else
        {
            assignmentType = WorkerBee.AssignmentType.Flower;
        }

        // Use the ResourceManager's method
        bool success = resourceManager.SpawnWorkerAtLocation(coord, assignmentType);

        if (success)
        {
            Debug.Log($"Worker spawned at {coord} ({assignmentType})!");
        }
        else
        {
            Debug.LogWarning("Failed to spawn worker");
        }
    }

    /// <summary>
    /// Exits placement mode and cleans up highlights
    /// </summary>
    public void CancelPlacement()
    {
        isPlacementModeActive = false;
        ClearHighlights();
        currentHoveredHighlight = null;
    }

    /// <summary>
    /// Clears all highlight GameObjects
    /// </summary>
    void ClearHighlights()
    {
        foreach (var highlight in spawnLocationHighlights.Values)
        {
            if (highlight != null)
            {
                Destroy(highlight);
            }
        }

        spawnLocationHighlights.Clear();
    }
}