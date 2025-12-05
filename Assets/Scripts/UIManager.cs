using UnityEngine;
using UnityEngine.UI; 
using TMPro; // <-- ADD THIS for TextMeshPro

public class UIManager : MonoBehaviour
{
    // Singleton pattern for easy static access from other scripts
    public static UIManager Instance { get; private set; }

    [Header("UI References")]
    public Slider healthSlider; // Drag your HealthBarSlider here
    public Slider magicSlider;  // Drag your MagicBarSlider here
    
    [Header("Pickup Message UI")]
    [Tooltip("The panel containing the message text. Used to toggle visibility.")]
    public GameObject messagePanel; 
    [Tooltip("The Text component (TextMeshProUGUI preferred) used to display the message content.")]
    public TextMeshProUGUI messageText; // <-- CHANGED from 'Text' to 'TextMeshProUGUI'
    
    [Header("Player Reference")]
    public PlayerState playerState; // Drag the GameObject with PlayerState here

    private bool isMessageVisible = false;
    // NEW: Stores the object to be destroyed when the player dismisses the message.
    private GameObject objectToDestroyOnDismiss; 

    void Awake()
    {
        // Implement the Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        if (playerState == null)
        {
            Debug.LogError("PlayerState reference is missing in UIManager!");
            return;
        }
        if (healthSlider == null || magicSlider == null)
        {
            Debug.LogError("One or both UI Sliders are missing in UIManager!");
            return;
        }
        // Note: TMPro components might require a check for null here if you are getting errors.
        if (messagePanel == null || messageText == null)
        {
            Debug.LogError("Pickup Message UI references (Panel or Text) are missing in UIManager! Assign them in the Inspector.");
            // We won't return yet, as the core functionality still needs to initialize
        }
        
        // Ensure the message panel is hidden at the start
        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }

        // --- HEALTH BAR INITIALIZATION ---
        healthSlider.maxValue = playerState.GetMaxHP(); 
        healthSlider.value = playerState.GetCurrentHP(); 
        
        // --- MAGIC BAR INITIALIZATION ---
        magicSlider.maxValue = playerState.GetMaxMagic(); 
        magicSlider.value = playerState.GetCurrentMagic(); 
        
        Debug.Log("UI Manager initialized: HP and Magic sliders set to starting values.");
    }

    void Update()
    {
        // Continuously update the UI based on current player stats
        UpdateHealthBar(playerState.GetCurrentHP());
        UpdateMagicBar(playerState.GetCurrentMagic());
        
        // Check for message dismissal: Space key
        // NOTE: Input.GetKeyDown still works even if Time.timeScale is 0.
        if (isMessageVisible && Input.GetKeyDown(KeyCode.Space))
        {
            HidePickupMessage();
        }
    }

    public void UpdateHealthBar(float currentHP)
    {
        healthSlider.value = currentHP;
    }

    public void UpdateMagicBar(float currentMagic)
    {
        magicSlider.value = currentMagic;
    }
    
    /// <summary>
    /// Displays a message on the UI, pauses the game, and schedules an object for destruction 
    /// upon message dismissal. Called by triggers like DestroyOnTrigger.
    /// </summary>
    public void DisplayActionMessage(string message, GameObject targetToDestroy)
    {
        if (isMessageVisible || messagePanel == null || messageText == null) return;
        
        // Set the object to be destroyed when the message is dismissed
        objectToDestroyOnDismiss = targetToDestroy;

        // Use .text for TextMeshProUGUI
        messageText.text = message; 
        messagePanel.SetActive(true);
        isMessageVisible = true;

        // PAUSE THE GAME
        Time.timeScale = 0f; 
        
        Debug.Log($"[UI] Displaying action message: {message}. Game Paused. Target for delayed destruction: {targetToDestroy?.name ?? "None"}.");
    }
    
    /// <summary>
    /// Displays a message on the UI. Called by AbilityPickup.cs (or old systems).
    /// </summary>
    public void DisplayPickupMessage(string message)
    {
        // Simply call the action message variant with a null destruction target (for pickups)
        DisplayActionMessage(message, null);
    }

    /// <summary>
    /// Hides the message, performs delayed destruction if scheduled, and resumes game flow.
    /// </summary>
    public void HidePickupMessage()
    {
        if (!isMessageVisible || messagePanel == null) return;
        
        // --- DESTRUCTION LOGIC ---
        if (objectToDestroyOnDismiss != null)
        {
            Debug.Log($"[UI] Delayed destruction of {objectToDestroyOnDismiss.name} triggered by message dismissal.");
            Destroy(objectToDestroyOnDismiss);
            objectToDestroyOnDismiss = null; // Clear the reference
        }
        // -------------------------

        messagePanel.SetActive(false);
        isMessageVisible = false;

        // RESUME THE GAME
        Time.timeScale = 1f; 
        
        Debug.Log("[UI] Message dismissed. Game Resumed.");
    }
}