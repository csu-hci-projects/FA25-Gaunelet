using UnityEngine;

// Requires a Collider component set to Is Trigger = true
[RequireComponent(typeof(Collider))]
public class AbilityPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    [Tooltip("The ability type that will be enabled when the player touches this pickup.")]
    public AbilityType abilityToEnable;
    
    [Header("UI Feedback")]
    [Tooltip("The message displayed on screen when this item is picked up.")]
    [TextArea(3, 5)]
    public string pickupMessage = "Ability Unlocked! Press [SPACE] to continue.";

    void OnTriggerEnter(Collider other)
    {
        // Check if the colliding object is the player
        if (other.CompareTag("Player"))
        {
            // Try to get the GauntletAbilities component from the player
            GauntletAbilities gauntlet = other.GetComponent<GauntletAbilities>();
            
            if (gauntlet != null)
            {
                // Check if the ability was already enabled to avoid re-triggering the message
                
                // 1. Enable the ability on the GauntletAbilities script
                gauntlet.EnableAbility(abilityToEnable);

                // 2. Trigger the message display via the UIManager instance
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.DisplayPickupMessage(pickupMessage);
                } else {
                    Debug.LogWarning("[Pickup] UIManager instance is missing. Cannot display pickup message.");
                }
                
                // 3. Log success and destroy the pickup item
                Debug.Log($"[Pickup] Enabled ability: {abilityToEnable} for the player. Displaying message.");
                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning("[Pickup] Player hit the pickup, but GauntletAbilities component was not found!");
            }
        }
    }
    
    // Ensure the collider is set up as a trigger
    void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }
}