using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    private Animator animator;

    [Header("Attack Settings")]
    [SerializeField] private float attackDamage = 50f;
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float attackDelay = 0.3f; // Delay before damage is applied (animation timing)
    [SerializeField] private float attackCooldown = 1f; // Time between attacks
    [SerializeField] private LayerMask enemyLayer; // Specify which layer enemies are on

    [Header("Debug")]
    [SerializeField] private bool showAttackRange = true;

    private float lastAttackTime = -999f;

    void Start()
    {
        // Get the Animator component from the character model
        animator = GetComponentInChildren<Animator>(); 
    }

    void Update()
    {
        // --- INPUT CHECK ---
        // 1. Check for Left Mouse Button (LMB) click
        // 2. Ensure Right Mouse Button (RMB) is NOT held (not in Gauntlet Mode)
        if (Input.GetMouseButtonDown(0) && !Input.GetMouseButton(1))
        {
            // Check if enough time has passed since last attack
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                Attack();
                lastAttackTime = Time.time;
            }
        }
    }

    void Attack()
    {
        // Tell the Animator to fire the 'Attack' Trigger
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // Delay damage application to match animation timing
        Invoke(nameof(DealDamage), attackDelay);
    }

    void DealDamage()
    {
        Collider[] hits;
        
        // Find all colliders in attack range (using layer mask)
        if (enemyLayer.value == 0)
        {
             // Fallback: Check everything if layer mask is not set
            hits = Physics.OverlapSphere(transform.position, attackRange);
        }
        else
        {
            hits = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
        }

        foreach (Collider hit in hits)
        {
            // Try to get the IDamageable interface (decoupled)
            IDamageable damageable = hit.GetComponent<IDamageable>();

            if (damageable != null && damageable.IsAlive())
            {
                // Ensure we don't hit ourselves
                if (hit.transform == transform) continue; 
                
                damageable.TakeDamage(attackDamage);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (showAttackRange)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}