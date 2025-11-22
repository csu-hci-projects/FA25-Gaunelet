using UnityEngine;
using UnityEngine.AI;

// BeastAI now implements the IDamageable interface
public class BeastAI : MonoBehaviour, IDamageable 
{
    [Header("Wander Settings")]
    [SerializeField] private float wanderRadius = 15f; 
    [SerializeField] private float wanderSpeed = 3.5f;
    [SerializeField] private float minWanderTime = 3f;
    [SerializeField] private float maxWanderTime = 7f;
    
    private Vector3 spawnPoint;
    private float wanderTimer = 0f;

    [Header("Combat Settings (Increased Aggression/Power)")]
    [SerializeField] private Transform player;
    [SerializeField] private float chaseRange = 15f;    
    [SerializeField] private float attackRange = 1.8f;  
    [SerializeField] private float attackCooldown = 1.5f; 
    [SerializeField] private float attackDamage = 35f;  
    [SerializeField] private float attackDelay = 0.5f; 
    [SerializeField] private float chaseSpeed = 6.5f;   

    [Header("Beast Stats")]
    [SerializeField] private float maxHP = 120f;        
    [SerializeField] private float currentHP = 120f;    

    [Header("Death Settings")]
    [SerializeField] private float deathDestroyDelay = 3f; 

    private NavMeshAgent agent;
    private Animator animator;
    private PlayerState playerState; 

    private float attackTimer = 0f;
    private bool isDead = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component missing on " + gameObject.name);
            return;
        }

        // Set the spawn point to the beast's starting position
        spawnPoint = transform.position;

        // Get PlayerState component from player
        if (player != null)
        {
            playerState = player.GetComponent<PlayerState>();
            if (playerState == null)
            {
                Debug.LogError("PlayerState component not found on player! Beast cannot attack.");
            }
        }
        else
        {
            Debug.LogError("Player Transform not assigned to " + gameObject.name);
        }

        agent.updateRotation = false;
        currentHP = maxHP;

        // Start the timer so the beast chooses its first destination quickly
        wanderTimer = 0f; 
    }

    void Update()
    {
        if (agent == null || isDead) return;

        attackTimer += Time.deltaTime;

        // Use Infinity if player is null to prevent errors
        float distanceToPlayer = player != null ? Vector3.Distance(transform.position, player.position) : Mathf.Infinity;

        if (distanceToPlayer <= attackRange)
        {
            TryAttackPlayer(); 
        }
        else if (distanceToPlayer <= chaseRange)
        {
            ChasePlayer();
        }
        else
        {
            Wander();
        }

        HandleRotation();
        UpdateAnimation();
    }

    // --- Movement Logic: Replaces Patrol ---
    void Wander()
    {
        agent.isStopped = false;
        agent.speed = wanderSpeed;

        wanderTimer -= Time.deltaTime;

        if (wanderTimer <= 0f)
        {
            // Find a new random position within the wander radius of the spawn point
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += spawnPoint;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            
            // Set the timer for the next wander point
            wanderTimer = Random.Range(minWanderTime, maxWanderTime);
        }
        
        // If the beast reached its destination, reset the timer to choose a new one
        if (agent.hasPath && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            wanderTimer = 0f; 
        }
    }

    // --- Combat Logic ---
    void TryAttackPlayer()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // Face the player
        Vector3 dir = (player.position - transform.position).normalized;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
        }

        if (attackTimer >= attackCooldown)
        {
            attackTimer = 0f;
            animator.SetTrigger("Attack");
            animator.SetBool("IsWalking", false);
            
            // DELAY DAMAGE APPLICATION to sync with animation impact
            Invoke(nameof(DealDamageToPlayer), attackDelay); 
        }
    }
    
    void DealDamageToPlayer()
    {
        // Only deal damage if the player is still alive
        if (playerState != null && playerState.IsAlive())
        {
            playerState.TakeDamage(attackDamage);
        }
    }

    void ChasePlayer()
    {
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.SetDestination(player.position);
    }

    void HandleRotation()
    {
        if (agent.velocity.sqrMagnitude > 0.1f && !agent.isStopped)
        {
            Vector3 direction = agent.velocity.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            // Rotates the beast to face the direction of movement
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    void UpdateAnimation()
    {
        if (animator == null) return;

        float speed = agent.velocity.magnitude;
        animator.SetFloat("Speed", speed);
        animator.SetBool("IsWalking", speed > 0.1f && !agent.isStopped);
    }

    // --- IDamageable Implementation ---
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHP -= damage;
        Debug.Log($"[Beast] -{damage}HP | Current HP: {currentHP}/{maxHP}");

        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log($"[Beast] {gameObject.name} has died!");

        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        if (animator != null)
        {
            animator.SetTrigger("Death");
        }

        Destroy(gameObject, deathDestroyDelay);
    }

    public bool IsAlive() => currentHP > 0;
    public float GetCurrentHP() => currentHP;
    public float GetMaxHP() => maxHP;
}