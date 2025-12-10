using UnityEngine;


// Controls the behavior of a Bramble Tile object's health.
// This script should be attached directly to the main Bramble object (which has the Collider and Rigidbody).
// It has health and ONLY takes damage if the player's currently active ability is Fire.

public class Bramble : MonoBehaviour, IDamageable
{
    [Header("Bramble Health")]
    [Tooltip("The initial health of the bramble tile.")]
    [SerializeField] private float currentHealth = 10f;

    [Header("Damage Filtering")]
    [Tooltip("Any incoming damage equal to or above this threshold will be ignored. Use this to block high-damage non-fire attacks (e.g., player sword).")]
    [SerializeField] private float damageIgnoreThreshold = 10.0f; // NEW: Ignore damage >= 10.0f

    // Cached reference to the player's GauntletAbilities component
    private GauntletAbilities playerAbilities;
    
    // IMPORTANT: The object this script is on must have a Collider (Is Trigger = false to block player movement) 
    // and a Rigidbody (Is Kinematic = true recommended) for reliable particle collisions.

    void Start()
    {
        // Find the player tagged 'Player' and get their ability manager
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Assuming GauntletAbilities is on the Player GameObject
            playerAbilities = player.GetComponent<GauntletAbilities>();
        }

        if (playerAbilities == null)
        {
            Debug.LogError("Bramble.cs failed to find 'GauntletAbilities' on an object tagged 'Player'. Elemental damage check will fail.");
        }
    }

 
    // Reduces the bramble tile's health and checks for destruction.
    // This method is called by ParticleDamage.cs via the IDamageable interface.
    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0) return; // Already destroyed

        // NEW DAMAGE FILTER CHECK
        if (damage >= damageIgnoreThreshold)
        {
            // Block high-value damage (e.g., sword swing) regardless of element
            Debug.Log($"[Bramble Tile] Damage of {damage:F2} ignored. It exceeded the threshold of {damageIgnoreThreshold:F2} (likely a physical attack).");
            return;
        }
        
        // ELEMENTAL CHECK
        // Only take damage if the player is currently casting Fire.
        if (!IsPlayerCastingFire())
        {
            // Logging when damage is rejected due to wrong elemental type.
            string activeAbilityName = playerAbilities?.GetCurrentAbility().ToString() ?? "Unknown/None";
            Debug.Log($"[Bramble Tile] Damage received but rejected. Active Ability: {activeAbilityName}. Requires: Fire.");
            return;
        }
        // -------------------------

        // Damage is accepted! (It was low damage AND it was Fire)
        currentHealth -= damage;
        Debug.Log($"[Bramble Tile] Hit accepted! Fire damage applied: -{damage:F2}. New Health: {currentHealth:F2}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }


    // Checks if the player's currently active ability is Fire using the GauntletAbilities reference.
    private bool IsPlayerCastingFire()
    {
        if (playerAbilities == null)
        {
            // Try to find the component again in case the player was spawned late
            Start();
            if (playerAbilities == null) return false;
        }
        
        // This check ensures damage is only processed when the Gauntlet is set to Fire.
        // Assuming AbilityType.Fire is correctly defined elsewhere.
        return playerAbilities.GetCurrentAbility() == AbilityType.Fire;
    }


    // Implements the IDamageable interface. Returns true if health is above zero.
    public bool IsAlive()
    {
        return currentHealth > 0;
    }

    private void Die()
    {
        Debug.Log("Bramble tile destroyed! Destroying the current game object.");
        
        // DESTROYS THE OBJECT THIS SCRIPT IS ATTACHED TO.
        Destroy(gameObject);
    }
}