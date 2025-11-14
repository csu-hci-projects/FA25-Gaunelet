using UnityEngine;
using UnityEngine.AI;

// GoblinAI now implements the IDamageable interface
public class GoblinAI : MonoBehaviour, IDamageable
{
    [Header("Patrol Settings")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitTimeAtPoint = 2f;
    [SerializeField] private float reachDistance = 0.5f;
    [SerializeField] private float rotationOffset = 180f;

    [Header("Combat Settings")]
    [SerializeField] private Transform player;
    [SerializeField] private float chaseRange = 8f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackDelay = 0.4f; // Time delay to sync damage with animation

    [Header("Goblin Stats")]
    [SerializeField] private float maxHP = 50f;
    [SerializeField] private float currentHP = 50f;

    [Header("Death Settings")]
    [SerializeField] private float deathDestroyDelay = 2f; // Time before destroying after death

    private NavMeshAgent agent;
    private Animator animator;
    private PlayerState playerState;

    private int currentPatrolIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;
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

        if (patrolPoints.Length == 0)
        {
            Debug.LogWarning("No patrol points assigned to " + gameObject.name);
        }

        // Get PlayerState component from player (REQUIRED)
        if (player != null)
        {
            playerState = player.GetComponent<PlayerState>();
            if (playerState == null)
            {
                Debug.LogError("PlayerState component not found on player! Goblin cannot attack.");
            }
        }
        else
        {
            Debug.LogError("Player Transform not assigned to " + gameObject.name);
        }

        agent.speed = patrolSpeed;
        agent.updateRotation = false;

        currentHP = maxHP;

        GoToNextPatrolPoint();
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
            Patrol();
        }

        HandleRotation();
        UpdateAnimation();
    }

    // Function that checks cooldown and triggers attack animation
    void TryAttackPlayer()
    {
        // Stop moving instantly
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // Face the player
        Vector3 dir = (player.position - transform.position).normalized;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir) * Quaternion.Euler(0, rotationOffset, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
        }

        // Attack when cooldown is ready
        if (attackTimer >= attackCooldown)
        {
            attackTimer = 0f;
            animator.SetTrigger("Attack");
            animator.SetBool("IsWalking", false);
            
            // DELAY DAMAGE APPLICATION to sync with animation impact
            Invoke(nameof(DealDamageToPlayer), attackDelay); 
        }
    }
    
    // Function that deals damage (called by Invoke)
    void DealDamageToPlayer()
    {
        // Check if the player is still alive before dealing damage
        if (playerState != null && playerState.IsAlive())
        {
            playerState.TakeDamage(attackDamage);
        }
    }

    void ChasePlayer()
    {
        isWaiting = false;
        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    void Patrol()
    {
        if (isWaiting)
        {
            agent.isStopped = true;
            waitTimer += Time.deltaTime;

            if (waitTimer >= waitTimeAtPoint)
            {
                isWaiting = false;
                waitTimer = 0f;
                agent.isStopped = false;
                GoToNextPatrolPoint();
            }
            return;
        }

        if (agent.hasPath && !agent.pathPending && agent.remainingDistance <= reachDistance)
        {
            isWaiting = true;
        }
    }

    void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;

        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    void HandleRotation()
    {
        if (agent.velocity.sqrMagnitude > 0.1f && !agent.isStopped)
        {
            Vector3 direction = agent.velocity.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            targetRotation *= Quaternion.Euler(0, rotationOffset, 0);
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }

    // --- IDamageable Implementation ---

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHP -= damage;
        Debug.Log($"[Goblin] -{damage}HP | Current HP: {currentHP}/{maxHP}");

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
        Debug.Log($"[Goblin] {gameObject.name} has died!");

        // Stop all AI behavior
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // Disable collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Trigger death animation
        if (animator != null)
        {
            animator.SetTrigger("Death");
        }

        // Destroy after delay to allow animation to play
        Destroy(gameObject, deathDestroyDelay);
    }

    // Public getters required by IDamageable
    public bool IsAlive() => currentHP > 0;
    public float GetCurrentHP() => currentHP;
    public float GetMaxHP() => maxHP;
}