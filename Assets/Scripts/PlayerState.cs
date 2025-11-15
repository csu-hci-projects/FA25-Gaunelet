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

    private bool isInvincible = false;
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
        if (isInvincible)
        {
            Debug.Log("[PlayerState: HP] Invincible! Damage blocked.");
            return;
        }
        
        // 1. Apply Block Reduction Logic
        float finalDamage = damage;
        if (isBlocking)
        {
            finalDamage *= blockDamageReduction;
            Debug.Log($"[PlayerState: HP] Blocking! Reduced {damage:F2} damage to {finalDamage:F2}.");
        }
        
        // Ensure damage is positive before applying
        finalDamage = Mathf.Max(0, finalDamage); 

        currentHP -= finalDamage;
        
        // CRITICAL HP DAMAGE DEBUG
        Debug.Log($"[PlayerState: HP] Took **-{finalDamage:F2}** | Current HP: {currentHP:F2}/{maxHP}");

        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }
    }
    // ... (Die, Magic Methods, Getter/Setters are the same)
    
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