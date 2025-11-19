using UnityEngine;

public class PlayerState : MonoBehaviour, IDamageable
{
    [Header("Health & Magic Stats")]
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float currentHP = 100f;
    [SerializeField] private float maxMagic = 100f;
    [SerializeField] private float currentMagic = 100f;

    [Header("Regeneration Settings")]
    [SerializeField] private float magicRegenRate = 5f; 
    [SerializeField] private float blockDamageReduction = 0.5f; // New: 50% damage reduction when blocking

    [Header("Temporary Invulnerability Settings")]
    // The duration the player is immune to damage after being hit
    [SerializeField] private float damageInvulnerabilityDuration = 0.5f; 
    private float invulnerabilityTimer = 0f;
    private bool isDamagedInvulnerable = false; // Tracks the temporary cooldown

    private bool isInvincible = false; // Tracks permanent invincibility (e.g., cheat or power-up)
    private bool isBlocking = false; 

    void Start()
    {
        currentHP = maxHP;
        currentMagic = maxMagic;
        Debug.Log($"[PlayerState] Initialized. HP: {currentHP}/{maxHP} | Magic: {currentMagic}/{maxMagic}");
    }

    void Update()
    {
        RegenerateMagic();
        HandleInvulnerabilityTimer(); // NEW: Handle the damage cooldown timer
    }

    // NEW: Handles the temporary invulnerability after a hit
    void HandleInvulnerabilityTimer()
    {
        if (isDamagedInvulnerable)
        {
            invulnerabilityTimer -= Time.deltaTime;
            if (invulnerabilityTimer <= 0)
            {
                isDamagedInvulnerable = false;
                Debug.Log("[PlayerState: Status] Temporary damage invulnerability ended.");
            }
        }
    }

    void RegenerateMagic()
    {
        float magicToRestore = magicRegenRate * Time.deltaTime;
        
        if (currentMagic < maxMagic)
        {
             currentMagic = Mathf.Clamp(currentMagic + magicToRestore, 0f, maxMagic);
             // Debug.Log($"[PlayerState: Magic] Gained {magicToRestore:F2} | Current: {currentMagic:F2}/{maxMagic}");
        }
    }

    // --- HP Methods (IDamageable Implementation) ---

    public void TakeDamage(float damage)
    {
        // 1. Check for permanent Invincibility (Highest priority)
        if (isInvincible)
        {
            Debug.Log("[PlayerState: HP] Invincible! Damage blocked.");
            return;
        }

        // 2. Check for temporary Damage Invulnerability (NEW check to stop stacking damage)
        if (isDamagedInvulnerable)
        {
            Debug.Log("[PlayerState: HP] Temporary invulnerability active. Damage blocked.");
            return;
        }
        
        // --- Damage Application ---

        // 3. Apply Block Reduction Logic
        float finalDamage = damage;
        if (isBlocking)
        {
            finalDamage *= blockDamageReduction;
            Debug.Log($"[PlayerState: HP] Blocking! Reduced {damage:F2} damage to {finalDamage:F2}.");
        }
        
        // Ensure damage is positive before applying
        finalDamage = Mathf.Max(0, finalDamage); 

        // 4. Apply Damage
        currentHP -= finalDamage;
        
        // 5. Start Temporary Invulnerability (Damage Cooldown)
        isDamagedInvulnerable = true;
        invulnerabilityTimer = damageInvulnerabilityDuration;
        
        // CRITICAL HP DAMAGE DEBUG
        Debug.Log($"[PlayerState: HP] Took **-{finalDamage:F2}** | Current HP: {currentHP:F2}/{maxHP}");

        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }
    }
    
    void Die()
    {
        Debug.Log("[PlayerState: HP] **Player has died!**");
        // TODO: Add death animation, disable controls, reload scene, etc.
    }

    // --- Magic Methods ---

    public void UseMagic(float amount)
    {
        float previousMagic = currentMagic;
        currentMagic = Mathf.Clamp(currentMagic - amount, 0f, maxMagic);
        
        if (currentMagic < previousMagic)
        {
            Debug.Log($"[PlayerState: Magic] Used {amount:F2} | Current: {currentMagic:F2}/{maxMagic}");
        }
    }

    public void Heal(float amount)
    {
        currentHP = Mathf.Clamp(currentHP + amount, 0f, maxHP);
        Debug.Log($"[PlayerState: HP] Healed +{amount}HP | Current HP: {currentHP:F2}/{maxHP}");
    }

    public void RestoreMagic(float amount)
    {
        currentMagic = Mathf.Clamp(currentMagic + amount, 0f, maxMagic);
        Debug.Log($"[PlayerState: Magic] Restored +{amount}MP | Current Magic: {currentMagic:F2}/{maxMagic}");
    }

    // --- Getter/Setter Methods ---

    public bool IsAlive() => currentHP > 0;
    public float GetCurrentHP() => currentHP;
    public float GetMaxHP() => maxHP;
    public float GetCurrentMagic() => currentMagic;
    public float GetMaxMagic() => maxMagic;

    public void SetInvincible(bool status)
    {
        isInvincible = status;
        Debug.Log($"[PlayerState: Status] Invincible set to: {status}");
    }

    public bool IsInvincible() => isInvincible;

    public void SetBlocking(bool status)
    {
        isBlocking = status;
    }

    public bool IsBlocking() => isBlocking;
}