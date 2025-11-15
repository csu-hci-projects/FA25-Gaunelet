using UnityEngine;

// Assuming the IDamageable interface is defined in your project
public class ParticleDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damagePerParticle = 0.1f; 

    void OnParticleCollision(GameObject other)
    {
        IDamageable damageable = null;

        // 1. Try to get a direct component (GoblinAI or SpiderAI)
        // Check for the GoblinAI component (on the hit object or its parents)
        if (damageable == null)
        {
            GoblinAI goblin = other.GetComponent<GoblinAI>();
            if (goblin == null)
            {
                goblin = other.GetComponentInParent<GoblinAI>();
            }
            if (goblin != null) damageable = goblin;
        }
        
        // 2. Check for the SpiderAI component (if not already found)
        if (damageable == null)
        {
            SpiderAI spider = other.GetComponent<SpiderAI>();
            if (spider == null)
            {
                spider = other.GetComponentInParent<SpiderAI>();
            }
            if (spider != null) damageable = spider;
        }

        // 3. Apply damage universally if a damageable entity was found
        if (damageable != null)
        {
            damageable.TakeDamage(damagePerParticle);
            
            // Optional: Debug confirmation
            Debug.Log($"[ParticleDamage] Applied {damagePerParticle} damage to {other.name}.");
        }
    }
}