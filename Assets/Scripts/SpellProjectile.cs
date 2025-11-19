using UnityEngine;

public class SpellProjectile : MonoBehaviour
{
    // These values will be set by the CultAI script before the particle is launched
    public float damage = 0f;
    public string playerTag = "Player"; 
    
    // NOTE: This relies on the Particle System having Collision enabled 
    // and "Send Collision Messages" checked in the Inspector.

    /// <summary>
    /// Called when the particle collides with another object.
    /// </summary>
    private void OnParticleCollision(GameObject other)
    {
        // Check if the collided object is the Player
        if (other.CompareTag(playerTag))
        {
            // Apply Damage
            PlayerState playerState = other.GetComponent<PlayerState>();
            if (playerState != null)
            {
                // Damage is applied. PlayerState handles invulnerability cooldown.
                playerState.TakeDamage(damage);
                Debug.Log($"[SpellProjectile] Particle hit registered, damage attempted: {damage}.");
            }
        }
    }

    /// <summary>
    /// Called by the CultAI script to initialize damage value.
    /// </summary>
    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }
}