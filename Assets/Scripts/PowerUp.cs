using UnityEngine;
using UnityEngine.SceneManagement; 
using System.Collections; // Required for IEnumerator

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
    
    // --- Reference to the scaler script ---
    private StatMeterScaler scaler;
    
    // Changed to IEnumerator to allow waiting a small duration
    IEnumerator Start()
    {
        // 1. Define the unique key for this pickup
        pickupPersistenceKey = "PowerUp_Collected_" + gameObject.name;
        
        // 2. Find and cache the StatMeterScaler component
        GameObject canvasObject = GameObject.FindWithTag("Canvas");
        if (canvasObject != null)
        {
            // We only need the scaler reference here for applying NEW pickups later in ApplyPowerUp,
            // as the sync logic for previously collected items is now handled by UIManager.
            scaler = canvasObject.GetComponent<StatMeterScaler>(); 
        }
        
        // 3. Normal Persistence Check
        if (PlayerPrefs.GetInt(pickupPersistenceKey, 0) == 1)
        {
            // A. Hide the object immediately so the player doesn't see it flicker.
            if (GetComponent<Collider>()) GetComponent<Collider>().enabled = false;
            
            // Disable all renderers (in case it has children)
            foreach(var r in GetComponentsInChildren<Renderer>())
            {
                r.enabled = false;
            }

            // B. Wait slightly longer (0.1s). 
            // We keep this delay to ensure the object is visually hidden before destruction.
            yield return new WaitForSeconds(0.1f);

            // C. REMOVED: The logic that called scaler.OnHealthUpgradePickedUp() is gone.
            // UIManager.LateUpdate() now handles the sync on scene load.

            // Already collected, destroy the object
            Destroy(gameObject);
        }
        else
        {
            Debug.Log($"[PowerUp] '{gameObject.name}' is available for pickup.");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if key is null (in case Trigger happens before Start - unlikely but possible)
        if (string.IsNullOrEmpty(pickupPersistenceKey)) 
        {
             pickupPersistenceKey = "PowerUp_Collected_" + gameObject.name;
        }

        // Safety check to prevent double pickup
        if (PlayerPrefs.GetInt(pickupPersistenceKey, 0) == 1) return;
        
        // Check if the collider belongs to the Player
        PlayerState playerState = other.GetComponent<PlayerState>();
        if (playerState != null)
        {
            ApplyPowerUp(playerState);
        }
    }

    /// <summary>
    /// Applies the selected bonuses to the player's permanent stats via PlayerState and
    /// triggers the UI message and delayed destruction.
    /// </summary>
    void ApplyPowerUp(PlayerState playerState)
    {
        bool applied = false;

        // Apply Max HP Bonus
        if (grantMaxHP)
        {
            playerState.ApplyPermanentHPIncrease(HP_BONUS);
            
            // Crucial: When a NEW power-up is picked up, we must notify the scaler immediately
            if (scaler != null)
            {
                scaler.OnHealthUpgradePickedUp();
                Debug.Log("[PowerUp] HP Meter scale request sent on NEW pickup.");
            }
            applied = true;
        }

        // Apply Max Magic Bonus
        if (grantMaxMagic)
        {
            playerState.ApplyPermanentMagicIncrease(MAGIC_BONUS);
            
            // Crucial: When a NEW power-up is picked up, we must notify the scaler immediately
            if (scaler != null)
            {
                scaler.OnMagicUpgradePickedUp();
                Debug.Log("[PowerUp] Magic Meter scale request sent on NEW pickup.");
            }
            applied = true;
        }

        // Apply Sword Damage Multiplier
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
            // 1. Mark this specific pickup as collected for persistence immediately
            PlayerPrefs.SetInt(pickupPersistenceKey, 1);
            PlayerPrefs.Save();

            // 2. Restore HP and Magic to 100% of the new max value
            playerState.RestoreHealthToMax();
            playerState.RestoreMagicToMax();
            Debug.Log("[PowerUp] Player HP and Magic restored to 100% max value.");

            Debug.Log($"[PowerUp] Collected '{gameObject.name}'. Bonuses applied and saved. Displaying message.");

            // 3. Display the message (which handles destruction)
            if (UIManager.Instance != null)
            {
                UIManager.Instance.DisplayActionMessage(pickupMessage, gameObject);
            }
            else
            {
                Debug.LogWarning("[PowerUp] UIManager instance is missing. Destroying immediately.");
                Destroy(gameObject);
            }
        }
    }
}