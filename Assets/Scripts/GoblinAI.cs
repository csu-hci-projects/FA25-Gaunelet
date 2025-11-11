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
    
    private NavMeshAgent agent;
    private Animator animator;
    private int currentPatrolIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component missing on " + gameObject.name);
            return;
        }
        
        if (animator == null)
        {
            Debug.LogWarning("Animator component missing on " + gameObject.name);
        }
        
        if (patrolPoints.Length == 0)
        {
            Debug.LogWarning("No patrol points assigned to " + gameObject.name);
            return;
        }
        
        agent.speed = patrolSpeed;
        agent.updateRotation = false; // We'll handle rotation manually
        
        GoToNextPatrolPoint();
    }

    void Update()
    {
        if (patrolPoints.Length == 0 || agent == null) return;
        
        Patrol();
        HandleRotation();
        UpdateAnimation();
    }
    
    void UpdateAnimation()
    {
        if (animator == null) return;
        
        // Set speed parameter based on agent velocity
        float speed = agent.velocity.magnitude;
        animator.SetFloat("Speed", speed);
        
        // Also set a bool for more reliable transitions
        bool isMoving = speed > 0.1f && !isWaiting;
        animator.SetBool("IsWalking", isMoving);
        
        // Debug to see what speed values we're getting
        if (Time.frameCount % 30 == 0) // Log every 30 frames
        {
            Debug.Log($"Goblin speed: {speed:F2}, Is waiting: {isWaiting}, IsWalking: {isMoving}");
        }
    }
    
    void HandleRotation()
    {
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            Vector3 direction = agent.velocity.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            targetRotation *= Quaternion.Euler(0, rotationOffset, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    void Patrol()
    {
        if (isWaiting)
        {
            agent.isStopped = true; // Force stop during wait
            waitTimer += Time.deltaTime;
            
            if (waitTimer >= waitTimeAtPoint)
            {
                isWaiting = false;
                waitTimer = 0f;
                agent.isStopped = false; // Resume movement
                GoToNextPatrolPoint();
            }
            return;
        }
        
        // Check if we have a valid path and are close to destination
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

    // Visualization in Scene view
    void OnDrawGizmosSelected()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        
        Gizmos.color = Color.yellow;
        
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] != null)
            {
                Gizmos.DrawWireSphere(patrolPoints[i].position, 0.3f);
                
                int nextIndex = (i + 1) % patrolPoints.Length;
                if (patrolPoints[nextIndex] != null)
                {
                    Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[nextIndex].position);
                }
            }
        }
    }
}