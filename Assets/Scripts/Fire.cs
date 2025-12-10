using UnityEngine;
using System.Collections.Generic;

// Controls the behavior of a Fire Tile object's health.
// This script should be attached to a Collider child object of the main fire visual parent.
// It has health and ONLY takes damage if the player's currently active ability is Ice.
public class Fire : MonoBehaviour, IDamageable 
{
    [Header("Fire Health")]
    [Tooltip("The initial health of the fire tile.")]
    [SerializeField] private float currentHealth = 10f;

    // Cached reference to the player's GauntletAbilities component
    private GauntletAbilities playerAbilities;
    
    // IMPORTANT: The collider on this object MUST be set to Is Trigger = true in the Inspector.

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
            Debug.LogError("Fire.cs failed to find 'GauntletAbilities' on an object tagged 'Player'. Please check player tag and component presence.");
        }
    }

    // Reduces the fire tile's health and checks for destruction.
    // This method is called by ParticleDamage.cs via the IDamageable interface.
    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0) return; // Already destroyed

        // --- PHYSICAL DAMAGE IGNORE LOGIC ---
        // Ignore the damage if the damage amount is 10f or greater.
        // This is specifically designed to ignore high-damage physical attacks like the player's sword.
        // If the player is casting Ice, this check is bypassed by the elemental check below.
        if (damage >= 10f)
        {
            Debug.Log($"[Fire Tile] Physical damage of {damage} ignored. Requires Ice ability to affect.");
            return;
        }

        // --- ELEMENTAL CHECK ---
        // Only take damage if the player is currently casting Ice.
        if (!IsPlayerCastingIce())
        {
            // Detailed log when damage is rejected
            string activeAbilityName = playerAbilities != null ? playerAbilities.GetCurrentAbility().ToString() : "Player Reference Missing";
            Debug.Log($"[Fire Tile] Damage REJECTED. Active Ability: {activeAbilityName}. Health: {currentHealth}.");
            return;
        }
        // -------------------------

        // Damage is accepted!
        currentHealth -= damage;
        Debug.Log($"[Fire Tile] Hit accepted! Ice damage applied. New Health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Checks if the player's currently active ability is Ice using the GauntletAbilities reference.
    private bool IsPlayerCastingIce()
    {
        if (playerAbilities == null)
        {
            // Try to find the component again as a fallback
            Start();
            if (playerAbilities == null) return false;
        }
        
        // Final check against the current ability
        return playerAbilities.GetCurrentAbility() == AbilityType.Ice;
    }

    // Implements the IDamageable interface. Returns true if health is above zero.
    public bool IsAlive()
    {
        return currentHealth > 0;
    }

    private void Die()
    {
        Debug.Log("Fire tile extinguished! Destroying health manager and its parent.");
        
        // Get the parent object (the fire visual/particle system)
        Transform parentObject = transform.parent;

        // Destroy the parent object, which contains the visual fire effect.
        if (parentObject != null)
            {
            Destroy(parentObject.gameObject);
        }
        
        // Destroy this collider/health script object (if the parent wasn't found or as a fallback)
        Destroy(gameObject);
    }
}