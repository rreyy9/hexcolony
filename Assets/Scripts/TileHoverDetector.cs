using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Detects which hex tile the player's mouse is hovering over.
/// Shows buildable positions when hovering, locks them when clicking.
/// NEW: Holding Shift AFTER clicking enables rapid-build mode for quick connector placement.
/// </summary>
public class TileHoverDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HexGrid hexGrid;
    [SerializeField] private BuildModeController buildModeController;
    [SerializeField] private Camera mainCamera;

    [Header("Raycast Settings")]
    [SerializeField] private LayerMask tileLayer;
    [SerializeField] private float raycastDistance = 100f;

    [Header("Highlight Settings")]
    [SerializeField] private GameObject highlightPrefab;
    [SerializeField] private float highlightYOffset = 0.1f;

    [Header("Outline Settings")]
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private Material normalHighlightMaterial;

    // Currently hovered tile coordinate
    private Vector2Int? currentHoveredCoord = null;
    private GameObject currentHoveredTile = null;

    // Dictionary of currently active highlights (coord -> GameObject)
    private Dictionary<Vector2Int, GameObject> activeHighlights = new Dictionary<Vector2Int, GameObject>();

    // Track which highlight is currently being hovered over
    private GameObject currentHoveredHighlight = null;
    private Vector2Int? currentHoveredHighlightCoord = null;

    // Track if highlights are currently locked (after clicking a tile)
    private bool highlightsLocked = false;

    // NEW: Track the coordinate of the locked tile (for rapid-build mode)
    private Vector2Int? lockedTileCoord = null;

    // NEW: Track if we're in rapid-build mode (Shift held AFTER placing a connector)
    private bool rapidBuildModeActive = false;

    // Reference to mouse and keyboard for New Input System
    private Mouse mouse;
    private Keyboard keyboard;

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        mouse = Mouse.current;
        keyboard = Keyboard.current;
    }

    void Update()
    {
        if (buildModeController == null || !buildModeController.IsBuildModeActive)
        {
            ClearAll();
            return;
        }

        if (mouse == null || keyboard == null)
        {
            return;
        }

        // Check if Shift key is being held
        bool isShiftHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;

        // NEW: If rapid-build mode is active but Shift was released, exit rapid-build mode
        if (rapidBuildModeActive && !isShiftHeld)
        {
            UnlockHighlights();
            rapidBuildModeActive = false;
            Debug.Log("Rapid-build mode ended (Shift released)");
            return;
        }

        // Always detect what we're hovering over
        if (highlightsLocked)
        {
            // Highlights are locked - only detect hover over highlights
            DetectHighlightHover();
        }
        else
        {
            // Normal mode - detect tile hover and show preview highlights
            DetectTileHover();
        }

        // Check for click
        if (mouse.leftButton.wasPressedThisFrame)
        {
            HandleClick(isShiftHeld);
        }
    }

    /// <summary>
    /// Detects which tile the player is hovering over and shows preview highlights.
    /// </summary>
    void DetectTileHover()
    {
        Vector2 mousePosition = mouse.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, raycastDistance, tileLayer))
        {
            GameObject hitTile = hit.collider.gameObject;
            Vector2Int hoveredCoord = hexGrid.WorldToAxial(hit.point);

            if (!IsTileConnectedToHive(hoveredCoord))
            {
                ClearPreviewHighlights();
                return;
            }

            // If we're hovering over a new tile, update highlights
            if (currentHoveredTile != hitTile)
            {
                currentHoveredTile = hitTile;
                currentHoveredCoord = hoveredCoord;

                // Show preview highlights around this tile
                ShowPreviewHighlights(hoveredCoord);

                Debug.Log($"Hovering over tile at {hoveredCoord}");
            }
        }
        else
        {
            // Not hovering over any tile - clear preview
            ClearPreviewHighlights();
        }
    }

    /// <summary>
    /// Shows preview highlights (when hovering before clicking).
    /// </summary>
    void ShowPreviewHighlights(Vector2Int centerCoord)
    {
        // Clear existing highlights
        ClearHighlights();

        // Show buildable positions
        ShowBuildablePositions(centerCoord);
    }

    /// <summary>
    /// Clears preview highlights when not hovering over a tile.
    /// </summary>
    void ClearPreviewHighlights()
    {
        currentHoveredTile = null;
        currentHoveredCoord = null;

        // Only clear highlights if they're not locked
        if (!highlightsLocked)
        {
            ClearHighlights();
        }
    }

    /// <summary>
    /// Detects which highlight position the player is hovering over (when highlights are locked).
    /// Raycasts to find world position, then checks if that position has a highlight.
    /// </summary>
    void DetectHighlightHover()
    {
        Vector2 mousePosition = mouse.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        // Create a plane at y=0 (ground level) to raycast against
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float rayDistance;

        // Raycast against the ground plane
        if (groundPlane.Raycast(ray, out rayDistance))
        {
            // Get the world position where the ray hit the ground
            Vector3 hitPoint = ray.GetPoint(rayDistance);

            // Convert world position to hex coordinate
            Vector2Int hoveredCoord = hexGrid.WorldToAxial(hitPoint);

            // Check if this coordinate has an active highlight
            if (activeHighlights.ContainsKey(hoveredCoord))
            {
                if (currentHoveredHighlightCoord != hoveredCoord)
                {
                    // Clear previous outline
                    ClearHighlightOutline();

                    // Set new hovered highlight
                    currentHoveredHighlight = activeHighlights[hoveredCoord];
                    currentHoveredHighlightCoord = hoveredCoord;

                    // Apply outline
                    ApplyHighlightOutline(currentHoveredHighlight);

                    Debug.Log($"Hovering over buildable position at {hoveredCoord}");
                }
            }
            else
            {
                ClearHighlightOutline();
            }
        }
        else
        {
            ClearHighlightOutline();
        }
    }

    /// <summary>
    /// Handles mouse click - either locks highlights or selects a highlight.
    /// NEW: Pass isShiftHeld to control rapid-build behavior.
    /// </summary>
    void HandleClick(bool isShiftHeld)
    {
        if (highlightsLocked)
        {
            // Highlights are locked - check if clicking on a highlight
            if (currentHoveredHighlightCoord.HasValue)
            {
                // Player clicked on a highlight - build there
                OnHighlightSelected(currentHoveredHighlightCoord.Value, isShiftHeld);
            }
            else
            {
                // Player clicked somewhere else - cancel/unlock
                UnlockHighlights();
                rapidBuildModeActive = false;
            }
        }
        else
        {
            // Highlights not locked - check if clicking on a tile to lock highlights
            if (currentHoveredCoord.HasValue)
            {
                LockHighlights(currentHoveredCoord.Value);
            }
        }
    }

    /// <summary>
    /// Locks the currently visible highlights in place.
    /// NEW: Store the locked tile coordinate for rapid-build mode.
    /// </summary>
    void LockHighlights(Vector2Int tileCoord)
    {
        highlightsLocked = true;
        lockedTileCoord = tileCoord;
        rapidBuildModeActive = false; // Not in rapid-build mode yet
        Debug.Log($"Highlights locked at {tileCoord} - {activeHighlights.Count} buildable positions");
    }

    /// <summary>
    /// Unlocks highlights (clears them).
    /// </summary>
    void UnlockHighlights()
    {
        ClearHighlights();
        ClearHighlightOutline();
        highlightsLocked = false;
        lockedTileCoord = null;
        currentHoveredTile = null;
        currentHoveredCoord = null;
        Debug.Log("Highlights unlocked");
    }

    /// <summary>
    /// Called when player selects a specific highlight to build on.
    /// NEW: If Shift is held, immediately re-lock highlights on the newly placed connector.
    /// </summary>
    void OnHighlightSelected(Vector2Int coord, bool isShiftHeld)
    {
        Debug.Log($"Selected position {coord} for building!" + (isShiftHeld ? " [SHIFT - RAPID BUILD]" : ""));

        // Check if player can afford the Connector
        if (!ResourceManager.Instance.CanAffordConnector())
        {
            Debug.Log("Not enough Wax! Need 10 Wax to build.");
            UnlockHighlights();
            rapidBuildModeActive = false;
            return;
        }

        // Deduct the Wax cost
        if (!ResourceManager.Instance.SpendWax(10))
        {
            Debug.LogWarning("Failed to spend Wax!");
            UnlockHighlights();
            rapidBuildModeActive = false;
            return;
        }

        // Spawn the connector tile at this position
        hexGrid.SpawnConnectorTile(coord);

        // NEW: Rapid-build mode behavior
        if (isShiftHeld)
        {
            // Clear old highlights and outline
            ClearHighlights();
            ClearHighlightOutline();

            // The newly placed connector becomes the new locked tile
            lockedTileCoord = coord;

            // Show highlights around the new connector
            ShowBuildablePositions(coord);

            // Keep highlights locked AND activate rapid-build mode
            highlightsLocked = true;
            rapidBuildModeActive = true;

            Debug.Log($"Rapid-build mode activated: New connector at {coord} is now locked tile");
        }
        else
        {
            // Normal mode: Clear all highlights and unlock
            UnlockHighlights();
            rapidBuildModeActive = false;
        }
    }

    /// <summary>
    /// Shows highlight indicators on all empty adjacent hexes.
    /// </summary>
    void ShowBuildablePositions(Vector2Int centerCoord)
    {
        List<Vector2Int> neighbors = hexGrid.GetNeighbors(centerCoord);

        foreach (Vector2Int neighborCoord in neighbors)
        {
            if (!hexGrid.TileExistsAt(neighborCoord))
            {
                SpawnHighlight(neighborCoord);
            }
        }
    }

    /// <summary>
    /// Spawns a highlight object at the given hex coordinate.
    /// </summary>
    void SpawnHighlight(Vector2Int coord)
    {
        if (highlightPrefab == null)
        {
            Debug.LogWarning("Highlight prefab not assigned!");
            return;
        }

        Vector3 worldPos = hexGrid.AxialToWorld(coord);
        worldPos.y += highlightYOffset;

        GameObject highlight = Instantiate(highlightPrefab, worldPos, Quaternion.identity, transform);
        highlight.name = $"Highlight_{coord.x}_{coord.y}";

        // Apply normal highlight material
        if (normalHighlightMaterial != null)
        {
            Renderer renderer = highlight.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = normalHighlightMaterial;
            }
        }

        activeHighlights[coord] = highlight;
    }

    /// <summary>
    /// Applies outline material to a highlight when hovering over it.
    /// </summary>
    void ApplyHighlightOutline(GameObject highlight)
    {
        if (highlight == null || outlineMaterial == null) return;

        Renderer renderer = highlight.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = outlineMaterial;
        }
    }

    /// <summary>
    /// Clears the outline from the currently hovered highlight.
    /// </summary>
    void ClearHighlightOutline()
    {
        if (currentHoveredHighlight != null && normalHighlightMaterial != null)
        {
            Renderer renderer = currentHoveredHighlight.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = normalHighlightMaterial;
            }
        }

        currentHoveredHighlight = null;
        currentHoveredHighlightCoord = null;
    }

    /// <summary>
    /// Removes all active highlight objects.
    /// </summary>
    void ClearHighlights()
    {
        foreach (var kvp in activeHighlights)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }

        activeHighlights.Clear();
    }

    /// <summary>
    /// Clears everything.
    /// </summary>
    void ClearAll()
    {
        ClearHighlights();
        ClearHighlightOutline();
        highlightsLocked = false;
        lockedTileCoord = null;
        rapidBuildModeActive = false;
        currentHoveredTile = null;
        currentHoveredCoord = null;
    }

    /// <summary>
    /// Checks if a tile is connected to the Hive using BFS.
    /// </summary>
    bool IsTileConnectedToHive(Vector2Int coord)
    {
        // Use HexGrid's centralized connection checking
        return hexGrid.IsTileConnectedToHive(coord);
    }

    void OnDisable()
    {
        ClearAll();
    }
}