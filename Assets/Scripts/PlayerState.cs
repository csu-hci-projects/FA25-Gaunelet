using UnityEngine;
using System; 
using UnityEngine.SceneManagement; // Included for good practice, even if not explicitly used for reset here

public class PlayerState : MonoBehaviour, IDamageable
{
    // --- Persistence Keys for PowerUps ---
    private const string MaxHPBonusKey = "PowerUp_MaxHPBonus";
    private const string MaxMagicBonusKey = "PowerUp_MaxMagicBonus";
    private const string SwordDamageMultiplierKey = "PowerUp_SwordDamageMultiplier";
    private const string MagicDamageMultiplierKey = "PowerUp_MagicDamageMultiplier";

    [Header("Health & Magic Base Stats")]
    [SerializeField] private float baseMaxHP = 100f; // Renamed from maxHP
    [SerializeField] private float currentHP = 100f;
    [SerializeField] private float baseMaxMagic = 100f; // Renamed from maxMagic
    [SerializeField] private float currentMagic = 100f;
    [SerializeField] private float baseSwordDamage = 15f; // NEW: Base sword damage exposed here

    // --- Permanent Power-Up Bonuses (Loaded from Persistence) ---
    private float bonusMaxHP = 0f;
    private float bonusMaxMagic = 0f;
    private float swordDamageMultiplier = 1f;
    private float magicDamageMultiplier = 1f;

    [Header("Regeneration Settings")]
    [SerializeField] private float magicRegenRate = 5f; 
    [SerializeField] private float blockDamageReduction = 0.5f; 

    private bool isInvincible = false; 
    private bool isBlocking = false; 

    void Start()
    {
        // 1. Load permanent power-up effects from persistence.
        // If GameManager used PlayerPrefs.DeleteAll(), these will default to 0/1.
        LoadPowerUps();

        // 2. Initialize current health and magic to the MAX possible values (Base + Bonus)
        currentHP = GetMaxHP();
        currentMagic = GetMaxMagic();
        
        Debug.Log($"[PlayerState] Initialized. HP: {currentHP}/{GetMaxHP()} | Magic: {currentMagic}/{GetMaxMagic()}");
    }

    void Update()
    {
        RegenerateMagic();
    }
    
    // NOTE: The ResetAllPermanentProgress() method has been removed, 
    // as that responsibility now belongs to GameManager.cs in Scene 0.

    /// <summary>
    /// Loads the permanent power-up bonuses from PlayerPrefs.
    /// </summary>
    void LoadPowerUps()
    {
        // Default to 0 for bonuses, 1 for multipliers if no saved value exists.
        bonusMaxHP = PlayerPrefs.GetFloat(MaxHPBonusKey, 0f);
        bonusMaxMagic = PlayerPrefs.GetFloat(MaxMagicBonusKey, 0f);
        swordDamageMultiplier = PlayerPrefs.GetFloat(SwordDamageMultiplierKey, 1f);
        magicDamageMultiplier = PlayerPrefs.GetFloat(MagicDamageMultiplierKey, 1f);

        Debug.Log($"[PlayerState Load] Max HP Bonus: +{bonusMaxHP}");
        Debug.Log($"[PlayerState Load] Max Magic Bonus: +{bonusMaxMagic}");
        Debug.Log($"[PlayerState Load] Sword Damage Multiplier: x{swordDamageMultiplier:F2}");
        Debug.Log($"[PlayerState Load] Magic Damage Multiplier: x{magicDamageMultiplier:F2}");
    }

    void RegenerateMagic()
    {
        float magicToRestore = magicRegenRate * Time.deltaTime;
        
        if (currentMagic < GetMaxMagic()) 
        {
             currentMagic = Mathf.Clamp(currentMagic + magicToRestore, 0f, GetMaxMagic());
        }
    }

    public void TakeDamage(float damage) 
    {
        // 1. Check for permanent Invincibility (Highest priority)
        if (isInvincible)
        {
            Debug.Log("[PlayerState: HP] Invincible! Damage blocked.");
            return;
        }
        
        // --- Damage Application ---

        // 2. Apply Block Reduction Logic
        float finalDamage = damage;
        if (isBlocking)
        {
            finalDamage *= blockDamageReduction;
            Debug.Log($"[PlayerState: HP] Blocking! Reduced {damage:F2} damage to {finalDamage:F2}.");
        }
        
        // Ensure damage is positive before applying
        finalDamage = Mathf.Max(0, finalDamage); 

        // 3. Apply Damage
        currentHP -= finalDamage;
        
        // CRITICAL HP DAMAGE DEBUG
        Debug.Log($"[PlayerState: HP] Took **-{finalDamage:F2}** | Current HP: {currentHP:F2}/{GetMaxHP()}");

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

    // --- Power-Up Application Methods (Public API for PowerUp.cs) ---
    
    /// <summary>
    /// Increases the player's permanent Max HP and saves the new value. Heals player to the new max.
    /// </summary>
    public void ApplyPermanentHPIncrease(float amount)
    {
        bonusMaxHP += amount;
        PlayerPrefs.SetFloat(MaxHPBonusKey, bonusMaxHP);
        PlayerPrefs.Save();
        
        // Heal the player to the new max
        currentHP = GetMaxHP(); 
        
        Debug.Log($"[PlayerState: PowerUp] Permanent Max HP increased by +{amount}. New Max: {GetMaxHP()}.");
    }

    /// <summary>
    /// Increases the player's permanent Max Magic and saves the new value. Restores magic to the new max.
    /// </summary>
    public void ApplyPermanentMagicIncrease(float amount)
    {
        bonusMaxMagic += amount;
        PlayerPrefs.SetFloat(MaxMagicBonusKey, bonusMaxMagic);
        PlayerPrefs.Save();
        
        // Restore magic to the new max
        currentMagic = GetMaxMagic(); 
        
        Debug.Log($"[PlayerState: PowerUp] Permanent Max Magic increased by +{amount}. New Max: {GetMaxMagic()}.");
    }

    /// <summary>
    /// Multiplies the current sword damage multiplier by the given factor and saves the new value.
    /// </summary>
    public void ApplySwordDamageMultiplier(float factor)
    {
        swordDamageMultiplier *= factor;
        PlayerPrefs.SetFloat(SwordDamageMultiplierKey, swordDamageMultiplier);
        PlayerPrefs.Save();
        Debug.Log($"[PlayerState: PowerUp] Sword Damage Multiplier applied: x{factor}. New Total Multiplier: x{swordDamageMultiplier:F2}.");
    }

    /// <summary>
    /// Multiplies the current magic damage multiplier by the given factor and saves the new value.
    /// </summary>
    public void ApplyMagicDamageMultiplier(float factor)
    {
        magicDamageMultiplier *= factor;
        PlayerPrefs.SetFloat(MagicDamageMultiplierKey, magicDamageMultiplier);
        PlayerPrefs.Save();
        Debug.Log($"[PlayerState: PowerUp] Magic Damage Multiplier applied: x{factor}. New Total Multiplier: x{magicDamageMultiplier:F2}.");
    }

    // --- Public Restoration Methods (Called by PowerUp.cs) ---

    /// <summary>
    /// Restores the player's current health to their maximum possible HP (Base + Bonus).
    /// Called when a PowerUp is collected.
    /// </summary>
    public void RestoreHealthToMax()
    {
        currentHP = GetMaxHP();
        Debug.Log($"[PlayerState: Heal] Health restored to Max: {currentHP:F2}");
    }

    /// <summary>
    /// Restores the player's current magic to their maximum possible Magic (Base + Bonus).
    /// Called when a PowerUp is collected.
    /// </summary>
    public void RestoreMagicToMax()
    {
        currentMagic = GetMaxMagic();
        Debug.Log($"[PlayerState: Magic] Magic restored to Max: {currentMagic:F2}");
    }


    // --- Magic Methods (Using updated GetMax methods) ---

    public void UseMagic(float amount)
    {
        float previousMagic = currentMagic;
        currentMagic = Mathf.Clamp(currentMagic - amount, 0f, GetMaxMagic());
        
        if (currentMagic < previousMagic)
        {
            // Debug.Log($"[PlayerState: Magic] Used {amount:F2} | Current: {currentMagic:F2}/{GetMaxMagic()}");
        }
    }

    public void Heal(float amount)
    {
        currentHP = Mathf.Clamp(currentHP + amount, 0f, GetMaxHP());
        Debug.Log($"[PlayerState: HP] Healed +{amount}HP | Current HP: {currentHP:F2}/{GetMaxHP()}");
    }

    public void RestoreMagic(float amount)
    {
        currentMagic = Mathf.Clamp(currentMagic + amount, 0f, GetMaxMagic());
        Debug.Log($"[PlayerState: Magic] Restored +{amount}MP | Current Magic: {currentMagic:F2}/{GetMaxMagic()}");
    }

    // --- Getter/Setter Methods ---

    public bool IsAlive() => currentHP > 0;
    public float GetCurrentHP() => currentHP;
    public float GetMaxHP() => baseMaxHP + bonusMaxHP; 
    public float GetCurrentMagic() => currentMagic;
    public float GetMaxMagic() => baseMaxMagic + bonusMaxMagic; 
    
    // NEW: Expose the base sword damage
    public float GetBaseSwordDamage() => baseSwordDamage; 

    // Expose damage multipliers for other components to read
    public float GetSwordDamageMultiplier() => swordDamageMultiplier;
    public float GetMagicDamageMultiplier() => magicDamageMultiplier;

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