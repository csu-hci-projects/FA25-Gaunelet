using UnityEngine;

public class ParticleDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damagePerParticle = 0.1f; 

    // This function is called by the Unity Particle System component when a particle 
    // collides with a GameObject that has a Collider.
    void OnParticleCollision(GameObject other)
    {
        // Start by assuming the component is on the hit object (since the collider is on the root)
        GoblinAI goblin = other.GetComponent<GoblinAI>();

        // If the particle hit a child object's collider, the component might be on the parent/root.
        // We use GetComponentInParent which searches the hit object and its ancestors.
        if (goblin == null)
        {
            goblin = other.GetComponentInParent<GoblinAI>();
        }

        if (goblin != null)
        {
            // Apply damage only if the component is found
            goblin.TakeDamage(damagePerParticle);
            
            // Optional: Debug confirmation
            Debug.Log($"[ParticleDamage] Applied {damagePerParticle} damage to Goblin.");
        }
        else
        {
            // Debugging: See what the particle is hitting that ISN'T the goblin
            // Debug.Log($"Particle hit non-target: {other.name}");
        }
    }
}