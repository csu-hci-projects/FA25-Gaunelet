using UnityEngine;
using UnityEngine.AI;

public class GhostAI : MonoBehaviour, IDamageable
{
    [Header("Patrol Settings")]
    [Tooltip("Assign empty Transforms here for the Ghost to follow.")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitTimeAtPoint = 2f;
    [SerializeField] private float reachDistance = 0.5f;
    [SerializeField] private float rotationOffset = 0f;

    [Header("Combat Settings")]
    [SerializeField] private Transform player;
    [SerializeField] private float chaseRange = 12f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackDelay = 0.4f;
    [SerializeField] private float chaseSpeed = 4.5f; 

    [Header("Ghost Stats")]
    [SerializeField] private float maxHP = 50f;
    [SerializeField] private float currentHP = 50f;

    [Header("Immunity Settings")] 
    [Tooltip("Any incoming damage >= this value is considered physical (sword) damage and will be ignored.")]
    [SerializeField] private float physicalDamageImmunityThreshold = 10f;

    [Header("Death Settings")]
    [SerializeField] private float deathDestroyDelay = 2f;

    private NavMeshAgent agent;
    private Animator animator;
    private PlayerState playerState;

    // Patrol variables
    private int currentPatrolIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    
    // Combat variables
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
            Debug.LogWarning("No patrol points assigned to " + gameObject.name + ". Ghost will stand still.");
        }

        if (player != null)
        {
            playerState = player.GetComponent<PlayerState>();
            if (playerState == null)
            {
                Debug.LogError("PlayerState component not found on player! Ghost cannot attack.");
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

    // --- Combat Logic ---

    void TryAttackPlayer()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        Vector3 dir = (player.position - transform.position).normalized;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir) * Quaternion.Euler(0, rotationOffset, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
        }

        if (attackTimer >= attackCooldown)
        {
            attackTimer = 0f;
            animator.SetTrigger("Attack");
            animator.SetBool("IsWalking", false);
            
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
        isWaiting = false;
        agent.isStopped = false;
        agent.speed = chaseSpeed; 
        agent.SetDestination(player.position);
    }

    // --- Patrol Logic ---
    void Patrol()
    {
        agent.speed = patrolSpeed; 
        
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

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
        else
        {
            Debug.LogWarning("NavMeshAgent is not active or placed on the NavMesh. Cannot set destination.");
        }
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

    // --- IDamageable Implementation (Magic Immunity Logic) ---

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        // NEW LOGIC: Block any damage that is greater than or equal to the threshold.
        // This assumes high-damage hits are physical (sword) and low-damage hits are magic.
        if (damage >= physicalDamageImmunityThreshold)
        {
            Debug.Log($"[Ghost] Physical damage ({damage:F2}HP) BLOCKED! Damage >= Threshold ({physicalDamageImmunityThreshold:F2}HP).");
            return; 
        }
        
        // Damage is applied if it's below the threshold (assumed to be magic/particle damage).
        if (damage > 0.0f)
        {
            currentHP -= damage;
            // Debug output to track HP
            Debug.Log($"[Ghost] MAGIC hit! -{damage:F2}HP | Current HP: {currentHP:F2}/{maxHP}");
        }
        else
        {
            // Ignore zero or negative damage
            return; 
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
        Debug.Log($"[Ghost] {gameObject.name} has been banished!");

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
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        
        // Draw lines between patrol points for visual setup aid
        if (patrolPoints != null && patrolPoints.Length > 1)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawSphere(patrolPoints[i].position, 0.2f);
                    if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                    }
                    else if (i == patrolPoints.Length - 1 && patrolPoints[0] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[0].position);
                    }
                }
            }
        }
    }
}