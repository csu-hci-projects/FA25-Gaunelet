using UnityEngine;
using UnityEngine.AI;

// SpiderAI now implements the IDamageable interface
public class SpiderAI : MonoBehaviour, IDamageable
{
    [Header("Wander Settings")]
    [SerializeField] private float wanderRadius = 15f; // Radius around the spawn point
    [SerializeField] private float wanderSpeed = 3.5f;
    [SerializeField] private float minWanderTime = 3f;
    [SerializeField] private float maxWanderTime = 7f;
    
    private Vector3 spawnPoint;
    private float wanderTimer = 0f;

    [Header("Combat Settings")]
    [SerializeField] private Transform player;
    [SerializeField] private float chaseRange = 10f; // Slightly larger chase range
    [SerializeField] private float attackRange = 1.5f; // Slightly smaller attack range
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private float attackDelay = 0.5f; 
    [SerializeField] private float chaseSpeed = 5f; // Faster than patrol

    [Header("Spider Stats")]
    [SerializeField] private float maxHP = 40f; // Lower HP than goblin
    [SerializeField] private float currentHP = 40f;

    [Header("Death Settings")]
    [SerializeField] private float deathDestroyDelay = 2f; 

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

        // Set the spawn point to the spider's starting position
        spawnPoint = transform.position;

        // Get PlayerState component from player (REQUIRED)
        if (player != null)
        {
            playerState = player.GetComponent<PlayerState>();
            if (playerState == null)
            {
                Debug.LogError("PlayerState component not found on player! Spider cannot attack.");
            }
        }
        else
        {
            Debug.LogError("Player Transform not assigned to " + gameObject.name);
        }

        agent.updateRotation = false;
        currentHP = maxHP;

        // Start the timer so the spider chooses its first destination quickly
        wanderTimer = 0f; 
    }

    void Update()
    {
        if (agent == null || isDead) return;

        attackTimer += Time.deltaTime;

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
        
        // If the spider reached its destination, reset the timer to choose a new one
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
            // Spiders often don't need a rotation offset like goblins
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
        Debug.Log($"[Spider] -{damage}HP | Current HP: {currentHP}/{maxHP}");

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
        Debug.Log($"[Spider] {gameObject.name} has died!");

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