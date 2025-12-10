using UnityEngine;
using UnityEngine.AI;

// The Lurker does NOT implement IDamageable, making it impervious to all damage.
public class LurkerAI : MonoBehaviour
{
    [Header("Patrol Settings")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitTimeAtPoint = 2f;
    [SerializeField] private float reachDistance = 0.5f;
    // CRITICAL: This adjusts the model's forward direction to match its movement direction.
    // If the monster is walking backward, change this from 180 to 0 (or vice versa).
    [SerializeField] private float rotationOffset = 180f; 

    [Header("Behavior Settings")]
    [Tooltip("Distance at which the Lurker stops patrolling and starts chasing.")]
    [SerializeField] private float chaseRange = 8f;
    
    [Header("Proximity Damage Settings")]
    [Tooltip("Distance at which the Lurker stops moving and begins dealing continuous damage.")]
    [SerializeField] private float damageRange = 2f;
    [Tooltip("Damage dealt to the player every tick.")]
    [SerializeField] private float proximityDamage = 5f;
    [Tooltip("How often damage is applied (seconds).")]
    [SerializeField] private float damageTickRate = 0.5f;

    [Header("Required References")]
    [SerializeField] private Transform player;

    private NavMeshAgent agent;
    private Animator animator;
    private PlayerState playerState;

    private int currentPatrolIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private float damageTickTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component missing on " + gameObject.name);
            return;
        }

        // Get PlayerState component from player (REQUIRED)
        if (player != null)
        {
            playerState = player.GetComponent<PlayerState>();
            if (playerState == null)
            {
                Debug.LogError("PlayerState component not found on player! Lurker cannot deal damage.");
            }
        }
        else
        {
            Debug.LogError("Player Transform not assigned to " + gameObject.name);
        }

        agent.speed = patrolSpeed;
        agent.updateRotation = false;

        // CRITICAL FIX: Delaying the first SetDestination call slightly (0.1s)
        // to ensure the NavMeshAgent has been properly initialized and placed on the baked NavMesh.
        Invoke(nameof(GoToNextPatrolPoint), 0.1f);
    }

    void Update()
    {
        if (agent == null) return;

        // Ensure damage tick timer counts up
        damageTickTimer += Time.deltaTime;

        float distanceToPlayer = player != null ? Vector3.Distance(transform.position, player.position) : Mathf.Infinity;

        if (distanceToPlayer <= damageRange)
        {
            HandleProximityDamage(); 
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

    // Stops the Lurker, makes it face the player, and deals proximity damage over time.
    void HandleProximityDamage()
    {
        // Stop moving instantly
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // Face the player
        FaceTarget(player.position);

        // Deal damage if the tick is ready
        if (damageTickTimer >= damageTickRate)
        {
            damageTickTimer = 0f;
            
            // The Lurker doesn't have an 'Attack' animation, so we just set walking to false
            animator?.SetBool("IsWalking", false); 
            
            if (playerState != null && playerState.IsAlive())
            {
                playerState.TakeDamage(proximityDamage);
                Debug.Log($"[Lurker] Player is too close! Taking {proximityDamage} damage.");
            }
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
        // Check if agent is active before setting destination (good defensive check)
        if (patrolPoints == null || patrolPoints.Length == 0 || agent == null || !agent.isActiveAndEnabled) return;

        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    void FaceTarget(Vector3 targetPosition)
    {
        Vector3 dir = (targetPosition - transform.position).normalized;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir) * Quaternion.Euler(0, rotationOffset, 0);
            // Slerp for smooth rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
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
        
        // Ensure Lurker doesn't run its attack trigger since it deals damage continuously
        animator.ResetTrigger("Attack");
    }

    void OnDrawGizmosSelected()
    {
        // Draw the damage proximity range (Danger Zone)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRange);
        
        // Draw the chase range (Detection)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}