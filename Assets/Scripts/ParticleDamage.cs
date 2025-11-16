using UnityEngine;

public class ParticleDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damagePerParticle = 0.1f; 

    // Player tag defined for exclusion
    private const string PLAYER_TAG = "Player"; 

    void OnParticleCollision(GameObject other)
    {
        // 1. EXCLUSION CHECK: If the particle hits the player object, skip damage.
        if (other.CompareTag(PLAYER_TAG))
        {
            return; 
        }
        
        // Use IDamageable interface to check for any enemy
        IDamageable damageable = null;

        // 2. Check for IDamageable on the hit object
        damageable = other.GetComponent<IDamageable>();
        
        // 3. Check for IDamageable on the parent object (for child colliders/limbs)
        if (damageable == null)
        {
            damageable = other.GetComponentInParent<IDamageable>();
        }

        // 4. Apply damage if a damageable entity (enemy) was found
        if (damageable != null)
        {
            damageable.TakeDamage(damagePerParticle);
        }
    }
}