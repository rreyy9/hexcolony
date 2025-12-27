using UnityEngine;
using UnityEngine.UI;
using TMPro; // Add this for TextMeshPro support

/// <summary>
/// Controls the build/upgrade mode for placing new hex tiles.
/// When active, players can see and place connector tiles adjacent to existing tiles.
/// </summary>
public class BuildModeController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button upgradeTileButton;
    [SerializeField] private TextMeshProUGUI buttonText; // Changed to TextMeshProUGUI

    // Tracks whether build mode is currently active
    private bool isBuildModeActive = false;

    void Start()
    {
        // Make sure button is connected and has a listener
        if (upgradeTileButton != null)
        {
            upgradeTileButton.onClick.AddListener(ToggleBuildMode);
        }
        else
        {
            Debug.LogError("Upgrade Tile Button not assigned in inspector!");
        }

        // Start with build mode off
        UpdateButtonDisplay();
    }

    /// <summary>
    /// Toggles build mode on/off when button is clicked.
    /// </summary>
    void ToggleBuildMode()
    {
        isBuildModeActive = !isBuildModeActive;
        UpdateButtonDisplay();

        Debug.Log($"Build Mode: {(isBuildModeActive ? "ACTIVE" : "INACTIVE")}");
    }

    /// <summary>
    /// Updates the button text to show current mode.
    /// </summary>
    void UpdateButtonDisplay()
    {
        if (buttonText != null)
        {
            buttonText.text = isBuildModeActive ? "Cancel Build" : "Upgrade Tile";
        }
    }

    /// <summary>
    /// Public property so other scripts can check if build mode is active.
    /// </summary>
    public bool IsBuildModeActive
    {
        get { return isBuildModeActive; }
    }

    void OnDestroy()
    {
        // Clean up the button listener when this object is destroyed
        if (upgradeTileButton != null)
        {
            upgradeTileButton.onClick.RemoveListener(ToggleBuildMode);
        }
    }
}