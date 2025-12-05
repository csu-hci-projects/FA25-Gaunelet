using UnityEngine;

/// <summary>
/// This script must be attached to the Particle System emitter (e.g., IceEmitter).
/// It listens for particle collision events and applies damage to any hit object
/// that implements the IDamageable interface.
/// </summary>
public class ParticleDamage : MonoBehaviour
{
    // The amount of damage a single particle hit applies.
    [Header("Damage Settings")]
    [Tooltip("Damage applied per particle collision.")]
    public float damageAmount = 1f;

    /// <summary>
    /// This is a special Unity function called when a particle from this system hits a collider.
    /// NOTE: For this to be called, the Particle System's 'Collision' module must be enabled,
    /// and 'Send Collision Messages' must be checked.
    /// </summary>
    /// <param name="other">The GameObject whose collider the particle hit.</param>
    void OnParticleCollision(GameObject other)
    {
        // 1. CRITICAL FIX: Ignore collisions with the player (the caster)
        // This prevents the player from hitting themselves with their own projectile's particles.
        if (other.CompareTag("Player"))
        {
            return; 
        }

        // MANDATORY DEBUG LOG: This will confirm if the OnParticleCollision function is firing.
        Debug.Log($"[ParticleDamage] Collision Registered: {other.name}");

        // 2. Check if the hit object implements the IDamageable interface (e.g., Fire.cs)
        IDamageable damageable = other.GetComponent<IDamageable>();

        if (damageable != null && damageable.IsAlive())
        {
            // If it's damageable, apply the damage.
            damageable.TakeDamage(damageAmount);
            
            Debug.Log($"[ParticleDamage] Found IDamageable on {other.name}. Calling TakeDamage.");
        }
    }
}