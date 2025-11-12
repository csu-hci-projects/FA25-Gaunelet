using UnityEngine;
using UnityEngine.AI;

public class GoblinAI : MonoBehaviour
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

    private NavMeshAgent agent;
    private Animator animator;

    private int currentPatrolIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private float attackTimer = 0f;
    private bool isChasing = false;

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
            return;
        }

        agent.speed = patrolSpeed;
        agent.updateRotation = false;

        GoToNextPatrolPoint();
    }

    void Update()
    {
        if (agent == null) return;

        attackTimer += Time.deltaTime;

        float distanceToPlayer = player != null ? Vector3.Distance(transform.position, player.position) : Mathf.Infinity;

        if (distanceToPlayer <= attackRange)
        {
            AttackPlayer();
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

    void AttackPlayer()
{
    // Stop moving instantly
    agent.isStopped = true;
    agent.velocity = Vector3.zero; // prevents lingering motion

    // Face the player immediately
    Vector3 dir = (player.position - transform.position).normalized;
    if (dir.sqrMagnitude > 0.001f)
    {
        Quaternion targetRot = Quaternion.LookRotation(dir) * Quaternion.Euler(0, rotationOffset, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
    }

    // Attack instantly when in range
    if (attackTimer >= attackCooldown)
    {
        attackTimer = 0f;
        animator.ResetTrigger("Attack"); // ensure clean trigger
        animator.SetTrigger("Attack");

        // Optional: immediately set walking to false so it snaps out of locomotion
        animator.SetBool("IsWalking", false);
    }
}

    void ChasePlayer()
    {
        // isChasing = true;
        isWaiting = false;
        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    void Patrol()
    {
        // isChasing = false;

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
}
