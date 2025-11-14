using UnityEngine;

public class PlayerState : MonoBehaviour, IDamageable
{
    // AbilityType is now defined elsewhere

    [Header("Player Stats")]
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float currentHP = 100f;
    [SerializeField] private float maxMagic = 100f;
    [SerializeField] private float currentMagic = 100f;

    [Header("Damage Settings")]
    [SerializeField] private float invulnerabilityDuration = 0.5f; // Prevent damage spam
    private float lastDamageTime = -999f;

    [Header("State")]
    private bool isBlocking = false;
    private bool isInvincible = false; // Dedicated Invincible state

    [Header("Block Settings")]
    [SerializeField] private float blockDamageReduction = 0f; // 0 = no damage, 0.3 = 30% damage taken

    void Start()
    {
        currentHP = maxHP;
        currentMagic = maxMagic;
    }

    // REMOVED: Update() and SwitchAbility() for ability cycling

    public void TakeDamage(float damage)
    {
        // 1. Invincibility Check (Highest priority)
        if (isInvincible)
        {
            Debug.Log("[PlayerState] Totally Invincible! No damage taken.");
            return;
        }

        // 2. Invulnerability Frame Check
        if (Time.time - lastDamageTime < invulnerabilityDuration)
        {
            return;
        }

        // 3. Block Reduction Check
        if (isBlocking)
        {
            damage *= blockDamageReduction;
            if (damage <= 0)
            {
                Debug.Log("[PlayerState] BLOCKED! No damage taken");
                return;
            }
            Debug.Log($"[PlayerState] BLOCKED! Reduced damage to {damage}");
        }

        currentHP -= damage;
        lastDamageTime = Time.time;

        Debug.Log($"[PlayerState] -{damage}HP | Current HP: {currentHP}/{maxHP}");

        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        Debug.Log($"[PlayerState] +{amount}HP | Current HP: {currentHP}/{maxHP}");
    }

    public void UseMagic(float amount)
    {
        if (currentMagic >= amount)
        {
            currentMagic -= amount;
            Debug.Log($"[PlayerState] -{amount} Magic | Current Magic: {currentMagic}/{maxMagic}");
        }
    }

    public void RestoreMagic(float amount)
    {
        currentMagic = Mathf.Min(currentMagic + amount, maxMagic);
        Debug.Log($"[PlayerState] +{amount} Magic | Current Magic: {currentMagic}/{maxMagic}");
    }

    void Die()
    {
        Debug.Log("[PlayerState] Player has died!");
        // Add death logic here (animation, respawn, game over, etc.)
    }

    // Public Getters
    public float GetCurrentHP() => currentHP;
    public float GetMaxHP() => maxHP;
    public float GetCurrentMagic() => currentMagic;
    public float GetMaxMagic() => maxMagic;
    public bool IsAlive() => currentHP > 0;

    // State Control (called by PlayerControls and GauntletAbilities)
    public void SetBlocking(bool blocking)
    {
        isBlocking = blocking;
    }
    public bool IsBlocking() => isBlocking;

    public void SetInvincible(bool invincible)
    {
        isInvincible = invincible;
    }
    public bool IsInvincible() => isInvincible;
}