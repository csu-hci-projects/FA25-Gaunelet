using UnityEngine;
using UnityEngine.UI; 
using TMPro; 
using System.Collections; // Necessary for the Coroutine

public class UIManager : MonoBehaviour
{
    // Singleton pattern for easy static access from other scripts
    public static UIManager Instance { get; private set; }
    
    // Duration for non-interactive messages (like DestroyOnDestroy success)
    private const float NotificationDuration = 3.0f; 

    [Header("UI References")]
    public Slider healthSlider; // Drag your HealthBarSlider here
    public Slider magicSlider;  // Drag your MagicBarSlider here
    
    [Header("Spell UI")]
    [Tooltip("The Text component used to display the currently active Gauntlet spell.")]
    public TextMeshProUGUI activeSpellText; 

    [Header("Pickup Message UI")]
    [Tooltip("The panel containing the message text. Used to toggle visibility.")]
    public GameObject messagePanel; 
    [Tooltip("The Text component (TextMeshProUGUI preferred) used to display the message content.")]
    public TextMeshProUGUI messageText; 
    
    [Header("Player Reference")]
    public PlayerState playerState; // Drag the GameObject with PlayerState here

    private bool isMessageVisible = false;
    private GameObject objectToDestroyOnDismiss; 
    
    // NEW FIX: Flag to prevent immediate dismissal on the same frame the game pauses.
    private bool isDismissalAllowed = false; 
    
    // Cache the abilities script to read the current spell
    private GauntletAbilities playerAbilities;
    // Track the last seen ability to prevent updating text every single frame
    private AbilityType lastKnownAbility = AbilityType.None; 
    
    // Reference to the scaler script for initial UI sync
    private StatMeterScaler scaler;

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
        // --- Reference Checks ---
        if (playerState == null)
        {
            Debug.LogError("PlayerState reference is missing in UIManager!");
            return;
        }
        
        // --- Get the GauntletAbilities component from the player ---
        playerAbilities = playerState.GetComponent<GauntletAbilities>();
        if (playerAbilities == null)
        {
            Debug.LogError("UIManager found PlayerState but could not find GauntletAbilities on the same object.");
        }

        if (healthSlider == null || magicSlider == null)
        {
            Debug.LogError("One or both UI Sliders are missing in UIManager!");
            return;
        }
        
        if (messagePanel == null || messageText == null)
        {
            Debug.LogError("Pickup Message UI references (Panel or Text) are missing in UIManager! Assign them in the Inspector.");
        }
        
        // Ensure the message panel is hidden at the start
        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }

        // 1. --- FIND AND CACHE SCALER FOR SYNC (Using the reliable direct GetComponent method) ---
        scaler = GetComponent<StatMeterScaler>(); 
        
        if (scaler != null)
        {
             Debug.Log("[UIManager] Successfully retrieved StatMeterScaler from this GameObject.");
        }
        else
        {
             Debug.LogError("[UIManager] StatMeterScaler component is missing from the Canvas!");
        }


        // 2. --- INITIALIZATION FOR LOGGING ONLY ---
        float maxHP = playerState.GetMaxHP();
        float currentHP = playerState.GetCurrentHP();
        
        // Initial value is still necessary for the first frame before Update runs.
        healthSlider.value = currentHP; 
        
        // The log that confirms the data is correctly read
        Debug.Log($"[UIManager] Health Init: Max={maxHP} (Initial PlayerState), Current={healthSlider.value}."); 
        
        // 3. --- MAGIC BAR INITIALIZATION (Initial value set here, max value set per frame in UpdateMagicBar) ---
        float maxMagic = playerState.GetMaxMagic();
        float currentMagic = playerState.GetCurrentMagic();
        
        // Initial value is still necessary for the first frame before Update runs.
        magicSlider.value = currentMagic; 
        
        Debug.Log($"[UIManager] Magic Init: Max={maxMagic} (Initial PlayerState), Current={magicSlider.value}."); 

        // 4. --- DELAYED VISUAL SYNC (Keep Coroutine for visual bar resizing) ---
        if (scaler != null)
        {
            StartCoroutine(SyncMeterVisualsDelayed(maxHP, maxMagic));
        }
        
        // Force initial UI update for the spell text
        UpdateActiveSpellUI(true);

        Debug.Log("UI Manager initialized: HP and Magic sliders set to starting values.");
    }
    
    // Delays the StatMeterScaler sync by one frame to ensure the visual resize happens 
    // AFTER the UI layout system has completed its initial frame update, resolving the timing conflict.
    private IEnumerator SyncMeterVisualsDelayed(float maxHP, float maxMagic)
    {
        // Wait one frame. This is the most reliable way to ensure the UI is stable.
        yield return null; 

        if (scaler != null)
        {
            // Check for Max HP Upgrade
            if (maxHP > 100f) // Assuming 100f is the base HP value
            {
                // This call should physically resize the health bar visual element
                scaler.OnHealthUpgradePickedUp();
                Debug.Log("[UIManager] DELAYED SYNC: Health Meter size updated via scaler.");
            }

            // Check for Max Magic Upgrade
            if (maxMagic > 100f) // Assuming 100f is the base Magic value
            {
                // This call should physically resize the magic bar visual element
                scaler.OnMagicUpgradePickedUp();
                Debug.Log("[UIManager] DELAYED SYNC: Magic Meter size updated via scaler.");
            }
        }
    }

    // Allows external systems (like MessageLoader) to forcibly reset the internal
    // message visibility status, preventing conflicts when multiple systems use the same panel.
    public void ResetMessageStatus()
    {
        isMessageVisible = false;
        objectToDestroyOnDismiss = null; // Clear destruction target just in case
        isDismissalAllowed = false; // Reset input safety flag
        Debug.Log("[UIManager] External message status reset.");
        // Note: We do NOT resume time here, as MessageLoader handles the unpause.
    }
    
    void Update()
    {
        // Continuously update the UI based on current player stats
        UpdateHealthBar(playerState.GetCurrentHP());
        UpdateMagicBar(playerState.GetCurrentMagic());
        
        // --- Update Spell Text ---
        UpdateActiveSpellUI();
        
        // --- Dismissal Logic for Interactive Messages ---
        // We only check for dismissal if the message is visible AND the game is paused AND
        // the one-frame safety delay has passed.
        if (isMessageVisible && Time.timeScale == 0f)
        {
            // The very first frame Time.timeScale is 0, we set the flag.
            if (!isDismissalAllowed)
            {
                isDismissalAllowed = true;
            }
            // The second frame (and later), we check for dismissal.
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                HidePickupMessage();
            }
        }
    }

    // This prevents any external script from resetting maxValue back to the Inspector default (100).
    public void UpdateHealthBar(float currentHP)
    {
        // 1. Force the maxValue to the true maximum from PlayerState every frame.
        healthSlider.maxValue = playerState.GetMaxHP(); 

        // 2. Set the current value.
        healthSlider.value = currentHP;
    }

    // This prevents any external script from resetting maxValue back to the Inspector default (100).
    public void UpdateMagicBar(float currentMagic)
    {
        // 1. Force the maxValue to the true maximum from PlayerState every frame.
        magicSlider.maxValue = playerState.GetMaxMagic();

        // 2. Set the current value.
        magicSlider.value = currentMagic;
    }
    
    // Checks the player's current ability and updates the UI text if it has changed.
    private void UpdateActiveSpellUI(bool forceUpdate = false)
    {
        if (playerAbilities == null || activeSpellText == null) return;

        // Get the current ability from the script
        AbilityType current = playerAbilities.GetCurrentAbility();

        // Only update the text if the ability has changed since the last frame
        if (current != lastKnownAbility || forceUpdate)
        {
            string spellName = current.ToString();
            
            // Handle the "None" case specifically
            if (current == AbilityType.None || !playerAbilities.IsAbilityEnabled(current))
            {
                // This covers the scenario where GauntletAbilities defaults to 'None' or 
                // if the currently selected ability is somehow disabled.
                spellName = "None";
            }
            
            // Set the final text string
            activeSpellText.text = $"Current Spell: {spellName}";
            lastKnownAbility = current;
        }
    }
    
    // Displays a message on the UI for a short duration without pausing the game.
    // Used for simple notifications like when a barrier is destroyed.
    public void ShowNotificationMessage(string message)
    {
        if (messagePanel == null || messageText == null) return;
        
        // Ensure we stop any previous notification coroutine to prevent overlap
        StopCoroutine(nameof(HideMessageAfterDelay)); 
        
        messageText.text = message; 
        messagePanel.SetActive(true);
        isMessageVisible = true; 
        
        // Start the timer to hide the message
        StartCoroutine(HideMessageAfterDelay(NotificationDuration));
        
        Debug.Log($"[UI] Displaying notification: {message}. Game running.");
    }
    
    // Coroutine to wait for a delay and then hide the message panel.
    // Uses WaitForSecondsRealtime so it functions correctly even if Time.timeScale is modified elsewhere.
    private IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        
        // Only hide if the message wasn't replaced by an interactive message
        if (isMessageVisible && Time.timeScale != 0f) 
        {
            messagePanel.SetActive(false);
            isMessageVisible = false;
            Debug.Log("[UI] Notification message hidden.");
        }
    }

    // Displays a message on the UI, pauses the game, and schedules an object for destruction 
    // upon message dismissal. Called by triggers like DestroyOnTrigger.
    public void DisplayActionMessage(string message, GameObject targetToDestroy)
    {
        if (messagePanel == null || messageText == null)
        {
             Debug.LogError("[UIManager] Pickup Message UI references (Panel or Text) are missing in UIManager! Cannot display message.");
             return;
        }

        // Stop any running notification coroutine, as this is an interactive message
        StopCoroutine(nameof(HideMessageAfterDelay)); 
        
        // Set the object to be destroyed when the message is dismissed
        objectToDestroyOnDismiss = targetToDestroy;

        // Use .text for TextMeshProUGUI
        messageText.text = message; 
        
        // CRITICAL STEP: Ensure the panel is visible before pausing
        messagePanel.SetActive(true);
        isMessageVisible = true; // Mark as visible immediately
        isDismissalAllowed = false; // RESET the safety flag

        // PAUSE THE GAME
        Time.timeScale = 0f; 
        
        Debug.Log($"[UI] Displaying action message: {message}. Game Paused. Target for delayed destruction: {targetToDestroy?.name ?? "None"}.");
    }
    
    // Displays a message on the UI. Called by AbilityPickup.cs (or old systems).
    // This uses the Action Message system, pausing the game until dismissed by Space bar.
    public void DisplayPickupMessage(string message)
    {
        // Simply call the action message variant with a null destruction target (for pickups)
        DisplayActionMessage(message, null);
    }

    // Hides the message, performs delayed destruction if scheduled, and resumes game flow.
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

        // CRITICAL STEP: Hide the panel first
        messagePanel.SetActive(false);
        isMessageVisible = false; // Reset the flag
        isDismissalAllowed = false; // Reset the safety flag

        // RESUME THE GAME
        Time.timeScale = 1f; 
        
        Debug.Log("[UI] Message dismissed. Game Resumed.");
    }
}