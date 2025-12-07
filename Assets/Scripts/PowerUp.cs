using UnityEngine;
using UnityEngine.SceneManagement; 

/// <summary>
/// Component to be attached to a collectible item that grants permanent,
/// persistent stat bonuses to the player.
/// </summary>
[RequireComponent(typeof(Collider))] // Requires a Collider component
public class PowerUp : MonoBehaviour
{
    [Header("Bonuses to Grant (Check all that apply)")]
    
    [Tooltip("Enables a permanent +50 Max HP increase.")]
    public bool grantMaxHP = false;
    
    [Tooltip("Enables a permanent +50 Max Magic increase.")]
    public bool grantMaxMagic = false;
    
    [Tooltip("Enables a permanent 2x multiplier for sword damage.")]
    public bool grantDoubleSwordDamage = false;
    
    [Tooltip("Enables a permanent 25% increase (x1.25) for magic damage.")]
    public bool grantMagicDamageIncrease = false;

    [Header("UI Feedback")]
    [Tooltip("The message displayed on screen when this item is picked up.")]
    [TextArea(3, 5)]
    public string pickupMessage = "Permanent Power-Up Gained! Check your stats. Press [SPACE] to continue.";

    // --- Constants for Values ---
    private const float HP_BONUS = 50f;
    private const float MAGIC_BONUS = 50f;
    private const float SWORD_MULTIPLIER = 2.0f; // Double damage
    private const float MAGIC_MULTIPLIER = 1.25f; // 25% increase
    
    // --- Persistence Key for This Specific Pickup ---
    private string pickupPersistenceKey; 
    
    // --- NEW: Reference to the scaler script ---
    private StatMeterScaler scaler;
    
    void Start()
    {
        // 1. Define the unique key for this pickup
        pickupPersistenceKey = "PowerUp_Collected_" + gameObject.name;
        
        // 2. Find and cache the StatMeterScaler component
        // Assuming the persistent Canvas is tagged "Canvas" or can be found by name/reference
        GameObject canvasObject = GameObject.FindWithTag("Canvas");
        if (canvasObject != null)
        {
            scaler = canvasObject.GetComponent<StatMeterScaler>();
        }
        
        if (scaler == null)
        {
            Debug.LogError($"[PowerUp] Could not find StatMeterScaler component. Ensure the persistent Canvas object is tagged 'Canvas' and has the component attached.");
        }

        // 3. Normal Persistence Check
        if (PlayerPrefs.GetInt(pickupPersistenceKey, 0) == 1)
        {
            // Already collected, destroy the object immediately so it doesn't appear
            Debug.Log($"[PowerUp] '{gameObject.name}' already collected. Destroying instance.");
            Destroy(gameObject);
        }
        else
        {
            Debug.Log($"[PowerUp] '{gameObject.name}' is available for pickup.");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Safety check to prevent double pickup
        if (PlayerPrefs.GetInt(pickupPersistenceKey, 0) == 1) return;
        
        // Check if the collider belongs to the Player (who should have the PlayerState component)
        PlayerState playerState = other.GetComponent<PlayerState>();
        if (playerState != null)
        {
            ApplyPowerUp(playerState);
        }
    }

    /// <summary>
    /// Applies the selected bonuses to the player's permanent stats via PlayerState and
    /// triggers the UI message and delayed destruction, including meter scaling if applicable.
    /// </summary>
    void ApplyPowerUp(PlayerState playerState)
    {
        bool applied = false;

        // Apply Max HP Bonus
        if (grantMaxHP)
        {
            playerState.ApplyPermanentHPIncrease(HP_BONUS);
            // --- CRITICAL: CALL SCALER ---
            if (scaler != null)
            {
                scaler.OnHealthUpgradePickedUp();
                Debug.Log("[PowerUp] HP Meter scale request sent.");
            }
            applied = true;
        }

        // Apply Max Magic Bonus
        if (grantMaxMagic)
        {
            playerState.ApplyPermanentMagicIncrease(MAGIC_BONUS);
            // --- CRITICAL: CALL SCALER ---
            if (scaler != null)
            {
                scaler.OnMagicUpgradePickedUp();
                Debug.Log("[PowerUp] Magic Meter scale request sent.");
            }
            applied = true;
        }

        // Apply Sword Damage Multiplier (Note: Multipliers stack multiplicatively)
        if (grantDoubleSwordDamage)
        {
            playerState.ApplySwordDamageMultiplier(SWORD_MULTIPLIER);
            applied = true;
        }

        // Apply Magic Damage Multiplier
        if (grantMagicDamageIncrease)
        {
            playerState.ApplyMagicDamageMultiplier(MAGIC_MULTIPLIER);
            applied = true;
        }

        if (applied)
        {
            // 1. Restore HP and Magic to 100% of the new (or old) max value
            playerState.RestoreHealthToMax();
            playerState.RestoreMagicToMax();
            Debug.Log("[PowerUp] Player HP and Magic restored to 100% max value.");

            // 2. Mark this specific pickup as collected for persistence
            PlayerPrefs.SetInt(pickupPersistenceKey, 1);
            PlayerPrefs.Save();
            
            Debug.Log($"[PowerUp] Collected '{gameObject.name}'. Bonuses applied and saved. Displaying message.");

            // 3. Display the message, pause the game, and schedule this GameObject for destruction
            if (UIManager.Instance != null)
            {
                UIManager.Instance.DisplayActionMessage(pickupMessage, gameObject);
            }
            else
            {
                Debug.LogWarning("[PowerUp] UIManager instance is missing. Destroying immediately without dialogue.");
                Destroy(gameObject);
            }

            // IMPORTANT: We DO NOT call Destroy(gameObject) here. UIManager handles it on message dismissal.
        }
    }
}