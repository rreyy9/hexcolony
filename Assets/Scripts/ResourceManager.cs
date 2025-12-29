using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    // Singleton instance
    public static ResourceManager Instance { get; private set; }

    [Header("Resources")]
    [SerializeField] private int currentWax = 0;
    [SerializeField] private int currentNectar = 0;

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

    // Public getters for UI
    public int CurrentWax => currentWax;
    public int CurrentNectar => currentNectar;

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
}