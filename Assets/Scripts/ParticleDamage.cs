using UnityEngine;

// This script must be attached to the Particle System emitter (e.g., IceEmitter).
// It listens for particle collision events and applies damage to any hit object
// that implements the IDamageable interface.
public class ParticleDamage : MonoBehaviour
{
    // The amount of damage a single particle hit applies.
    [Header("Damage Settings")]
    [Tooltip("Damage applied per particle collision (this is the base damage).")]
    public float damageAmount = 1f;

    // CRITICAL: Reference to the player's state for reading damage multipliers
    private PlayerState playerState;

    void Start()
    {
        // 1. Find the PlayerState component
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerState = player.GetComponent<PlayerState>();
        }

        if (playerState == null)
        {
            Debug.LogError("ParticleDamage failed to find 'PlayerState' on an object tagged 'Player'. Magic damage multiplier will not apply!");
        }
    }

    // This is a special Unity function called when a particle from this system hits a collider.
    // NOTE: For this to be called, the Particle System's 'Collision' module must be enabled,
    // and 'Send Collision Messages' must be checked.
    void OnParticleCollision(GameObject other)
    {
        // 1. CRITICAL FIX: Ignore collisions with the player (the caster)
        // This prevents the player from hitting themselves with their own projectile's particles.
        if (other.CompareTag("Player"))
        {
            return; 
        }

        // MANDATORY DEBUG LOG: This will confirm if the OnParticleCollision function is firing.
        // Debug.Log($"[ParticleDamage] Collision Registered: {other.name}");

        // 2. Calculate the final damage (Base * Multiplier)
        float finalDamage = damageAmount;
        if (playerState != null)
        {
            float multiplier = playerState.GetMagicDamageMultiplier();
            finalDamage = damageAmount * multiplier;
            // Uncomment this line to see the boost in the console!
            // Debug.Log($"[ParticleDamage] Magic Damage: Base({damageAmount}) * Multiplier({multiplier:F2}) = Final({finalDamage:F2})");
        }
        
        // 3. Check if the hit object implements the IDamageable interface (e.g., Fire.cs)
        IDamageable damageable = other.GetComponent<IDamageable>();

        if (damageable != null && damageable.IsAlive())
        {
            // If it's damageable, apply the calculated final damage.
            damageable.TakeDamage(finalDamage);
            
            // Debug.Log($"[ParticleDamage] Found IDamageable on {other.name}. Calling TakeDamage.");
        }
    }
}