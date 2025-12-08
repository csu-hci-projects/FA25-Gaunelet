using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement; // REQUIRED for loading scenes
using System.Collections; // REQUIRED for Coroutines

/// <summary>
/// AI component for the final boss, which remains idle until the player enters its
/// engagement range, then attacks with devastating, block-dependent damage.
/// </summary>
public class FinalBossAI : MonoBehaviour, IDamageable
{
    // --- Combat Damage Constants ---
    private const float NO_BLOCK_DAMAGE = 150f; // Instantly kills player if not blocking
    private const float BLOCK_DAMAGE = 25f;    // Damage if player is blocking


    [Header("Boss Configuration")]
    [Tooltip("The maximum health of the Final Boss.")]
    [SerializeField] private float maxHP = 250f; // Made configurable
    [Tooltip("The victory message displayed on the UI when the boss is defeated.")]
    [SerializeField] 
    [TextArea(3, 5)] 
    private string endGameMessage = "VICTORY! The Final Boss has been defeated, and the realm is saved! You have won the game! Press [SPACE] to continue.";

    [Header("Combat Settings")]
    [SerializeField] private Transform player;
    [SerializeField] private float chaseRange = 10f; // Boss will start chasing when player is within this distance
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float attackCooldown = 2.0f;
    [SerializeField] private float attackDelay = 0.5f; // Time delay to sync damage with animation
    [SerializeField] private float rotationOffset = 180f; // This is the 180-degree offset for backward-facing models

    [Header("Boss Stats")]
    [SerializeField] private float currentHP; // Initialized to maxHP in Start()

    [Header("Death Settings")]
    [Tooltip("The duration of the death animation. The boss object will be destroyed after this delay.")]
    [SerializeField] private float deathAnimationDuration = 5.0f; // Delay before object is destroyed
    
    [Tooltip("The index of the scene to load after the game ends (e.g., 0 for Title Screen).")]
    [SerializeField] private int titleSceneIndex = 0; // The scene index to load
    
    [Tooltip("The delay to wait after the message appears (i.e., after the death animation finishes) before loading the scene.")]
    [SerializeField] private float sceneLoadDelay = 10.0f; // UPDATED to 10s default

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

        // Get PlayerState component from player (REQUIRED)
        if (player != null)
        {
            playerState = player.GetComponent<PlayerState>();
            if (playerState == null)
            {
                Debug.LogError("PlayerState component not found on player! Final Boss cannot attack.");
            }
        }
        else
        {
            Debug.LogError("Player Transform not assigned to " + gameObject.name);
        }

        agent.updateRotation = false; // We handle rotation manually
        
        // --- HP Initialization ---
        currentHP = maxHP; // Now uses the serialized maxHP
        // -------------------------

        // Ensure boss starts idle and stopped
        agent.isStopped = true;
        Debug.Log($"[FinalBossAI] Initialized with {currentHP} HP. Awaiting player engagement.");
    }

    void Update()
    {
        if (agent == null || isDead || player == null) return;

        attackTimer += Time.deltaTime;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

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
            StandIdle(); // Wait for the player to approach
        }

        HandleRotation();
        UpdateAnimation();
    }

    /// <summary>
    /// Boss stands still and waits.
    /// </summary>
    void StandIdle()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        animator.SetBool("IsWalking", false);
    }

    // Function that checks cooldown and triggers attack animation
    void TryAttackPlayer()
    {
        // Stop movement and face the player instantly
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        FaceTarget(player.position);

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
    
    /// <summary>
    /// Calculates and applies damage to the player based on whether they are blocking.
    /// Called by Invoke to sync with the attack animation.
    /// </summary>
    void DealDamageToPlayer()
    {
        if (playerState != null && playerState.IsAlive())
        {
            float damageToDeal = NO_BLOCK_DAMAGE; // Default: 150 (instant kill)

            if (playerState.IsBlocking())
            {
                damageToDeal = BLOCK_DAMAGE; // Reduced damage: 25
                Debug.Log("[FinalBossAI] Player is blocking! Damage reduced to 25.");
            }
            else
            {
                Debug.Log("[FinalBossAI] Player is NOT blocking! Dealing massive damage: 150.");
            }
            
            playerState.TakeDamage(damageToDeal);
        }
    }

    void ChasePlayer()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    void FaceTarget(Vector3 targetPosition)
    {
        Vector3 dir = (targetPosition - transform.position).normalized;
        if (dir.sqrMagnitude > 0.001f)
        {
            // Apply the rotation offset here to correct backward movement
            Quaternion targetRot = Quaternion.LookRotation(dir) * Quaternion.Euler(0, rotationOffset, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
        }
    }

    void HandleRotation()
    {
        if (agent.velocity.sqrMagnitude > 0.1f && !agent.isStopped)
        {
            // Only rotate based on movement when chasing
            FaceTarget(transform.position + agent.velocity);
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
        Debug.Log($"[FinalBoss] -{damage}HP | Current HP: {currentHP}/{maxHP}");

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
        Debug.Log($"[FinalBoss] {gameObject.name} has died! Will be destroyed in {deathAnimationDuration} seconds, triggering the end game message.");

        // Stop all AI behavior and animation
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // Trigger death animation
        if (animator != null)
        {
            animator.SetTrigger("Death");
        }
        
        // Disable collider immediately
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Destroy after delay to allow animation to play. OnDestroy() will handle the message and scene load.
        Destroy(gameObject, deathAnimationDuration);
    }
    
    /// <summary>
    /// Displays the victory message and schedules the title scene load.
    /// CRITICAL FIX: We start a Coroutine on the UIManager to handle the delay, 
    /// because this FinalBoss object is about to be destroyed and cannot run Invokes.
    /// </summary>
    private void OnDestroy()
    {
        // Only show the message if the boss died naturally.
        if (!isDead) return;

        Debug.Log("[FinalBossAI] Object destroyed. Displaying end game message and scheduling Title Scene load via UIManager.");
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.DisplayActionMessage(endGameMessage, null);
            
            // Piggyback on the UIManager to run the timer, since this object is dying right now.
            UIManager.Instance.StartCoroutine(LoadSceneAfterDelay(titleSceneIndex, sceneLoadDelay));
        }
        else
        {
             Debug.LogWarning("[FinalBossAI] UIManager instance missing. Cannot display end game message or load scene.");
        }
    }

    /// <summary>
    /// Coroutine that waits for the specified delay and then loads the scene.
    /// </summary>
    private IEnumerator LoadSceneAfterDelay(int sceneIndex, float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log($"[FinalBossAI] Timer finished. Loading Title Scene (Index {sceneIndex}).");
        SceneManager.LoadScene(sceneIndex);
    }

    // Public getters required by IDamageable
    public bool IsAlive() => currentHP > 0;
    public float GetCurrentHP() => currentHP;
    public float GetMaxHP() => maxHP; 
}