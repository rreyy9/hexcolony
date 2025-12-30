using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages all UI elements for the game.
/// Displays resources, worker count, and handles spawn worker button.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Resource Display")]
    [SerializeField] private TextMeshProUGUI waxText;
    [SerializeField] private TextMeshProUGUI nectarText;
    [SerializeField] private TextMeshProUGUI workerCountText;

    [Header("Spawn Worker Button")]
    [SerializeField] private Button spawnWorkerButton;
    [SerializeField] private TextMeshProUGUI spawnWorkerButtonText;
    [SerializeField] private TextMeshProUGUI workerCostText;

    private ResourceManager resourceManager;

    void Start()
    {
        resourceManager = ResourceManager.Instance;

        if (resourceManager == null)
        {
            Debug.LogError("ResourceManager not found!");
            return;
        }

        // Hook up button click event
        if (spawnWorkerButton != null)
        {
            spawnWorkerButton.onClick.AddListener(OnSpawnWorkerClicked);
        }

        // Initial UI update
        UpdateUI();
    }

    void Update()
    {
        // Update UI every frame to show real-time changes
        UpdateUI();
    }

    /// <summary>
    /// Updates all UI elements with current game state.
    /// </summary>
    void UpdateUI()
    {
        if (resourceManager == null) return;

        // Update resource displays
        if (waxText != null)
        {
            waxText.text = $"Wax: {resourceManager.CurrentWax}";
        }

        if (nectarText != null)
        {
            nectarText.text = $"Nectar: {resourceManager.CurrentNectar}";
        }

        if (workerCountText != null)
        {
            workerCountText.text = $"Workers: {resourceManager.ActiveWorkerCount} / {resourceManager.MaxWorkers}";
        }

        // Update spawn worker button state
        UpdateSpawnWorkerButton();
    }

    /// <summary>
    /// Updates the spawn worker button's enabled state and appearance.
    /// </summary>
    void UpdateSpawnWorkerButton()
    {
        if (spawnWorkerButton == null) return;

        bool canAfford = resourceManager.CanAffordWorker();
        bool atMaxWorkers = resourceManager.ActiveWorkerCount >= resourceManager.MaxWorkers;

        // Enable/disable button
        spawnWorkerButton.interactable = canAfford;

        // Update button text
        if (spawnWorkerButtonText != null)
        {
            if (atMaxWorkers)
            {
                spawnWorkerButtonText.text = "Max Workers";
            }
            else if (!canAfford)
            {
                spawnWorkerButtonText.text = "Spawn Worker";
            }
            else
            {
                spawnWorkerButtonText.text = "Spawn Worker";
            }
        }

        // Update cost text color based on affordability
        if (workerCostText != null)
        {
            workerCostText.text = "Cost: 30 Nectar";

            if (canAfford)
            {
                workerCostText.color = Color.green;
            }
            else
            {
                workerCostText.color = Color.red;
            }
        }

        // Hide button if at max workers
        if (atMaxWorkers && spawnWorkerButton.gameObject.activeSelf)
        {
            // Optional: Hide the button entirely when at max
            // spawnWorkerButton.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Called when the spawn worker button is clicked.
    /// </summary>
    void OnSpawnWorkerClicked()
    {
        if (resourceManager.SpawnWorker())
        {
            Debug.Log("Worker spawned successfully!");
            // Optional: Play spawn sound effect here
        }
        else
        {
            Debug.Log("Failed to spawn worker - not enough resources or at max workers.");
            // Optional: Play error sound here
        }
    }

    void OnDestroy()
    {
        // Clean up button listener
        if (spawnWorkerButton != null)
        {
            spawnWorkerButton.onClick.RemoveListener(OnSpawnWorkerClicked);
        }
    }
}