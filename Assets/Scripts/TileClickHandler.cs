using UnityEngine;
using UnityEngine.InputSystem;

public class TileClickHandler : MonoBehaviour
{
    [Header("Tile Settings")]
    [SerializeField] private TileType tileType;

    [Header("References")]
    [SerializeField] private HexGrid hexGrid;
    private BuildModeController buildModeController;

    private Vector2Int tileCoordinate;
    private Camera mainCamera;
    private Mouse mouse;

    public enum TileType
    {
        Hive,
        Flower,
        Connector
    }

    void Start()
    {
        // Find the BuildModeController in the scene
        buildModeController = FindAnyObjectByType<BuildModeController>();

        if (buildModeController == null)
        {
            Debug.LogWarning("BuildModeController not found in scene!");
        }

        // Get references for New Input System
        mainCamera = Camera.main;
        mouse = Mouse.current;

        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
        }
    }

    void Update()
    {
        // Check for mouse click using New Input System
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            DetectClick();
        }
    }

    void DetectClick()
    {
        // Only handle clicks if not in build mode
        if (buildModeController != null && buildModeController.IsBuildModeActive)
        {
            return;
        }

        // Raycast from mouse position to detect if this tile was clicked
        Vector2 mousePosition = mouse.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            // Check if the raycast hit THIS tile
            if (hit.collider.gameObject == gameObject)
            {
                if (tileType == TileType.Hive)
                {
                    OnHiveClicked();
                }
                else if (tileType == TileType.Flower)
                {
                    OnFlowerClicked();
                }
            }
        }
    }

    void OnHiveClicked()
    {
        // Always clickable
        ResourceManager.Instance.AddWax(1);
        PlayClickFeedback();
    }

    void OnFlowerClicked()
    {
        // Only if connected to Hive
        if (IsConnectedToHive())
        {
            ResourceManager.Instance.AddNectar(1);
            PlayClickFeedback();
        }
        else
        {
            Debug.Log("Flower not connected!");
        }
    }

    bool IsConnectedToHive()
    {
        // Use HexGrid's centralized connection checking
        if (hexGrid == null)
        {
            Debug.LogWarning("HexGrid reference not set on TileClickHandler!");
            return false;
        }

        return hexGrid.IsTileConnectedToHive(tileCoordinate);
    }

    void PlayClickFeedback()
    {
        // Simple scale bounce animation
        // TODO: Implement visual feedback in next step
        Debug.Log($"{tileType} clicked!");
    }

    // Public method to set the tile coordinate (called by HexGrid when spawning)
    public void SetTileCoordinate(Vector2Int coord)
    {
        tileCoordinate = coord;
    }

    // Public method to set the HexGrid reference (called by HexGrid when spawning)
    public void SetHexGrid(HexGrid grid)
    {
        hexGrid = grid;
    }
}