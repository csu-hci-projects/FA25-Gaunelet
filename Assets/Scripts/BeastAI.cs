using UnityEngine;
using UnityEngine.AI;


public class BeastAI : MonoBehaviour, IDamageable 
{
    // Animation Parameter Constants
    private const string AnimParam_Speed = "Speed";
    private const string AnimParam_IsWalking = "IsWalking"; 
    private const string AnimParam_Attack = "Attack";
    private const string AnimParam_Death = "Death";
    
    [Header("Wander Settings")]
    [SerializeField] private float wanderRadius = 15f; 
    [SerializeField] private float wanderSpeed = 3.5f;
    [SerializeField] private float minWanderTime = 3f;
    [SerializeField] private float maxWanderTime = 7f;
    
    private Vector3 spawnPoint;
    private float wanderTimer = 0f;

    [Header("Combat Settings (Increased Aggression/Power)")]
    [SerializeField] private Transform player;
    [Tooltip("The initial range at which the beast detects the player and initiates chase.")]
    [SerializeField] private float chaseRange = 15f;    
    [Tooltip("The maximum distance the beast will chase the player once aggro is initiated.")]
    [SerializeField] private float relentlessChaseRange = 40f; // New High Range for Chase Lock
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
    private bool isAggro = false; // State flag: true means the beast is actively pursuing the player

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component missing on " + gameObject.name + ". Cannot move.");
            return;
        }

        if (animator == null)
        {
             Debug.LogWarning("Animator component missing on " + gameObject.name + ". Animations disabled.");
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

        float distanceToPlayer = player != null ? Vector3.Distance(transform.position, player.position) : Mathf.Infinity;

        if (distanceToPlayer <= attackRange)
        {
            // State 1: Attack (Highest Priority)
            isAggro = true; 
            TryAttackPlayer();
        }
        else if (isAggro)
        {
            // State 2: Relentless Chase / Revert
            if (distanceToPlayer <= relentlessChaseRange)
            {
                // Player is still within the high chase limit
                ChasePlayer();
            }
            else
            {
                // Player escaped the high range, revert to patrol
                isAggro = false;
                Wander();
            }
        }
        else if (distanceToPlayer <= chaseRange)
        {
            // State 3: Initial Aggro Detection (Start Chase)
            isAggro = true; 
            ChasePlayer();
        }
        else
        {
            // State 4: Default Wander
            Wander();
        }

        HandleRotation();
        UpdateAnimation();
    }

    // Movement Logic: Replaces Patrol
    void Wander()
    {
        // This is only called if isAggro is false
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

    // Combat Logic
    void TryAttackPlayer()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // Face the player 
        Vector3 dir = (player.position - transform.position).normalized;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            // Use a higher rotation speed for a snappier look while attacking
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f); 
        }

        if (attackTimer >= attackCooldown)
        {
            attackTimer = 0f;
            if (animator != null)
            {
                animator.SetTrigger(AnimParam_Attack);
                animator.SetBool(AnimParam_IsWalking, false); 
            }
            
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
        // Only rotate if the agent is moving
        if (agent.velocity.sqrMagnitude > 0.1f && !agent.isStopped)
        {
            Vector3 direction = agent.velocity.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            // Rotates this GameObject to face the direction of movement
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    void UpdateAnimation()
    {
        if (animator == null) return;

        float speed = agent.velocity.magnitude;
        animator.SetFloat(AnimParam_Speed, speed);
        animator.SetBool(AnimParam_IsWalking, speed > 0.1f && !agent.isStopped); 
    }

    // IDamageable Implementation
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHP -= damage;
        Debug.Log($"[Beast] -{damage}HP | Current HP: {currentHP}/{maxHP}");

        // Any damage taken also causes immediate aggression, regardless of distance
        if (!isAggro)
        {
            isAggro = true;
            Debug.Log("[Beast] Damage taken! Aggro initiated.");
        }


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

        // Collider is on this same GameObject (the root)
        Collider col = GetComponent<Collider>(); 
        if (col != null)
        {
            col.enabled = false;
        }

        if (animator != null)
        {
            animator.SetTrigger(AnimParam_Death);
        }

        // Destroy this GameObject (the root object)
        Destroy(gameObject, deathDestroyDelay);
    }

    public bool IsAlive() => currentHP > 0;
    public float GetCurrentHP() => currentHP;
    public float GetMaxHP() => maxHP;
}