using UnityEngine;
using UnityEngine.AI;

public class CultAI : MonoBehaviour, IDamageable
{
    [Header("Wander Settings")]
    [SerializeField] private float wanderRadius = 15f; 
    [SerializeField] private float wanderSpeed = 2.5f; 
    [SerializeField] private float minWanderTime = 4f; 
    [SerializeField] private float maxWanderTime = 8f;
    
    private Vector3 spawnPoint;
    private float wanderTimer = 0f;

    [Header("Combat Settings")]
    [SerializeField] private Transform player;
    [SerializeField] private float chaseRange = 12f;  
    [SerializeField] private float attackRange = 8f;   
    
    // continuous spell firing
    [SerializeField] private float attackWindowDuration = 3.0f; 
    // casting cooldown
    [SerializeField] private float spellCooldownDuration = 1.5f; 
    
    [SerializeField] private float attackDamage = 25f; 
    [SerializeField] private float attackDelay = 0.5f;
    
    [SerializeField] private float chaseSpeed = 3.5f; 

    [Header("Cultist Stats")]
    [SerializeField] private float maxHP = 60f; 
    [SerializeField] private float currentHP = 60f;

    [Header("Ranged Attack Visuals")]
    [SerializeField] private ParticleSystem spellEmitter; 
    
    [Header("Death Settings")]
    [SerializeField] private float deathDestroyDelay = 3f; 

    private NavMeshAgent agent;
    private Animator animator;
    private PlayerState playerState;
    private SpellProjectile spellProjectile; 
    private float attackTimer = 0f; 
    private bool isDead = false;
    private bool isAttackingWindow = false;

    private bool isAnimatorFrozen = false; 

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component missing on " + gameObject.name);
            return;
        }

        spawnPoint = transform.position;

        if (player != null)
        {
            playerState = player.GetComponent<PlayerState>();
            if (playerState == null)
            {
                Debug.LogError("PlayerState component not found on player! Cultist cannot attack.");
            }
        }
        else
        {
            Debug.LogError("Player Transform not assigned to " + gameObject.name);
        }
        
        if (spellEmitter == null)
        {
            Debug.LogError("Spell Emitter is not assigned to the CultAI script!");
        }
        else
        {
            spellEmitter.Stop(); 
            // Get the SpellProjectile script from the emitter
            spellProjectile = spellEmitter.GetComponent<SpellProjectile>();
            if (spellProjectile == null)
            {
                 Debug.LogError("SpellProjectile script missing on Emitter! Damage will not be applied.");
            }
        }

        agent.updateRotation = false;
        currentHP = maxHP;
        wanderTimer = 0f; 
    }

    void Update()
    {
        if (agent == null || isDead) return;

        // Only tick the attack timer when NOT in the attack window (i.e., when cooling down or idling)
        if (!isAttackingWindow)
        {
            attackTimer += Time.deltaTime;
        }

        float distanceToPlayer = player != null ? Vector3.Distance(transform.position, player.position) : Mathf.Infinity;

        // Continuous rotation when in the attack window or in range to attack.
        if (distanceToPlayer <= attackRange || isAttackingWindow)
        {
            FacePlayer();
        }

        if (!isAttackingWindow) // Only move or try to initiate attack if the window is closed
        {
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
        }
        
        HandleRotation(); 
        UpdateAnimation();
    }

    // --- Movement Logic ---

    void Wander()
    {
        agent.isStopped = false;
        agent.speed = wanderSpeed;

        wanderTimer -= Time.deltaTime;

        if (wanderTimer <= 0f)
        {
            Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * wanderRadius;
            randomDirection += spawnPoint;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            
            wanderTimer = UnityEngine.Random.Range(minWanderTime, maxWanderTime);
        }
        
        if (agent.hasPath && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            wanderTimer = 0f; 
        }
    }

    // --- Combat Logic ---
    
    // Rotates the enemy to face the player using the fast combat speed.
    void FacePlayer()
    {
        if (player == null) return;
        
        // Calculate the direction to the player, ignoring Y-axis
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0; 
        
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            // Use the faster rotation speed (25f) for snappier combat facing/tracking
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 25f); 
        }
    }

    void TryAttackPlayer()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // Check if the spell cooldown duration has passed (1.5 seconds)
        if (attackTimer >= spellCooldownDuration)
        {
            // Reset the timer immediately to start tracking the cooldown after the window ends
            attackTimer = 0f; 
            
            animator.SetTrigger("Attack"); 
            animator.SetBool("IsWalking", false);
            
            // INVOKE START ATTACK WINDOW (after the brief animation delay)
            Invoke(nameof(StartAttackWindow), attackDelay); 
        }
    }
    

    // Called after the attackDelay. Starts the continuous 3.0s attack window.
    void StartAttackWindow()
    {
        // Null check for the necessary components before casting
        if (playerState != null && playerState.IsAlive() && spellEmitter != null && spellProjectile != null)
        {
            isAttackingWindow = true; // Enter the 3.0s attack state
            
            // Explicitly aim the particle emitter at the player's world position
            if (player != null)
            {
                Vector3 directionToPlayer = (player.position - spellEmitter.transform.position).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                spellEmitter.transform.rotation = targetRotation;
                Debug.Log("[Cultist] Spell Emitter rotation locked onto player for launch.");
            }

            // 1. Set the damage for the projectile to use on collision
            spellProjectile.SetDamage(attackDamage);

            // 2. Trigger the spell visual (starts continuous firing for the window)
            spellEmitter.Play(); 
        }
        
        // 3. Freeze the animator immediately after the launch to hold the pose for the duration
        FreezeAnimator();

        // 4. Schedule the end of the attack window
        Invoke(nameof(EndAttackWindow), attackWindowDuration); 
        
        Debug.Log("[Cultist] Attack Window STARTED (3.0s duration).");
    }

    // Called after the attackWindowDuration has passed (3.0s).
    void EndAttackWindow()
    {
        // 1. Stop the continuous attack
        if (spellEmitter != null)
        {
            spellEmitter.Stop();
        }
        
        // 2. Allow movement/action again
        UnfreezeAnimator();
        
        // 3. Exit the attack state. The attackTimer will now start counting the 1.5s cooldown.
        isAttackingWindow = false; 
        
        Debug.Log("[Cultist] Attack Window ENDED. Starting 1.5s cooldown.");
    }

    void ChasePlayer()
    {
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.SetDestination(player.position);
    }

    void HandleRotation()
    {
        // This only handles rotation while moving, based on NavMesh direction
        if (!isAnimatorFrozen && agent.velocity.sqrMagnitude > 0.1f && !agent.isStopped)
        {
            Vector3 direction = agent.velocity.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            // Rotates based on NavMesh direction when moving (15f speed)
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
        }
    }

    void UpdateAnimation()
    {
        if (animator == null) return;

        // Animator updates only when NOT in the frozen attack pose
        if (!isAnimatorFrozen)
        {
            float speed = agent.velocity.magnitude;
            animator.SetFloat("Speed", speed);
            animator.SetBool("IsWalking", speed > 0.1f && !agent.isStopped);
        }
    }

    // --- Animator Control Methods ---

    void FreezeAnimator()
    {
        if (isAnimatorFrozen) return; 

        if (animator != null)
        {
            animator.speed = 0; 
        }
        isAnimatorFrozen = true;
        Debug.Log("[Cultist] Animator Frozen (Holding Attack Pose).");
    }

    void UnfreezeAnimator()
    {
        if (animator != null)
        {
            animator.speed = 1; 
        }
        isAnimatorFrozen = false;
        Debug.Log("[Cultist] Animator Unfrozen (Ready to move/attack).");
    }

    // --- IDamageable Implementation ---

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHP -= damage;
        Debug.Log($"[Cultist] -{damage}HP | Current HP: {currentHP}/{maxHP}");

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
        Debug.Log($"[Cultist] {gameObject.name} has died!");

        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // Cancel all pending Invokes (StartAttackWindow, EndAttackWindow)
        CancelInvoke();
        
        // Ensure emitter is stopped upon death
        if (spellEmitter != null) spellEmitter.Stop();

        UnfreezeAnimator(); 

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        
        if (animator != null) animator.SetTrigger("Death");
        if (agent != null) agent.enabled = false;

        // Clear the reference before destruction to help clean up the Inspector
        spellProjectile = null; 

        Destroy(gameObject, deathDestroyDelay);
    }

    public bool IsAlive() => currentHP > 0;
    public float GetCurrentHP() => currentHP;
    public float GetMaxHP() => maxHP;
}