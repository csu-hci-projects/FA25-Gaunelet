using UnityEngine;
using System; 
using UnityEngine.SceneManagement; 
using System.Collections; 

// The PlayerState class now handles persistence, stats, and health/magic state only.
public class PlayerState : MonoBehaviour, IDamageable
{
    // --- Persistence Keys for PowerUps ---
    private const string MaxHPBonusKey = "PowerUp_MaxHPBonus";
    private const string MaxMagicBonusKey = "PowerUp_MaxMagicBonus";
    private const string SwordDamageMultiplierKey = "PowerUp_SwordDamageMultiplier";
    private const string MagicDamageMultiplierKey = "PowerUp_MagicDamageMultiplier";

    [Header("Health & Magic Base Stats")]
    [SerializeField] private float baseMaxHP = 100f; 
    [SerializeField] private float currentHP = 100f;
    [SerializeField] private float baseMaxMagic = 100f; 
    [SerializeField] private float currentMagic = 100f;
    [SerializeField] private float baseSwordDamage = 15f; 

    // --- Death Settings ---
    [Header("Death Settings")]
    [Tooltip("If enabled, the current scene will be reloaded upon player death (HP <= 0).")]
    [SerializeField] private bool reloadSceneOnDeath = true; 
    [Tooltip("The time delay (in seconds) before the scene reloads after death.")]
    [SerializeField] private float deathReloadDelay = 2.0f; 
    
    // --- Player State Core Fields ---
    private float bonusMaxHP = 0f;
    private float bonusMaxMagic = 0f;
    private float swordDamageMultiplier = 1f;
    private float magicDamageMultiplier = 1f;

    [Header("Regeneration Settings")]
    [SerializeField] private float magicRegenRate = 5f; 
    [SerializeField] private float blockDamageReduction = 0.5f; 

    private bool isInvincible = false; 
    private bool isBlocking = false; 
    private bool isDying = false; // Prevents calling Die() multiple times

    void Awake()
    {
        // Simple Awake initialization for PlayerState
    }

    void Start()
    {
        // 1. Load permanent power-up effects from persistence.
        LoadPowerUps();

        // 2. Initialize current health and magic to the MAX possible values (Base + Bonus)
        currentHP = GetMaxHP();
        currentMagic = GetMaxMagic();
        
        Debug.Log($"[PlayerState] Initialized. HP: {currentHP}/{GetMaxHP()} | Magic: {currentMagic}/{GetMaxMagic()}");
    }

    void Update()
    {
        if (isDying) return; 
        RegenerateMagic();
    }

    // Loads the permanent power-up bonuses from PlayerPrefs.
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
        // Check if the player is already dying or dead
        if (isDying) return; 

        // 1. Check for permanent Invincibility (Highest priority)
        if (isInvincible)
        {
            Debug.Log("[PlayerState: HP] Invincible! Damage blocked.");
            return;
        }
        
        // --- Damage Application ---
        float finalDamage = damage;
        if (isBlocking)
        {
            finalDamage *= blockDamageReduction;
            Debug.Log($"[PlayerState: HP] Blocking! Reduced {damage:F2} damage to {finalDamage:F2}.");
        }
        
        finalDamage = Mathf.Max(0, finalDamage); 

        // 3. Apply Damage
        currentHP -= finalDamage;
        
        Debug.Log($"[PlayerState: HP] Took **-{finalDamage:F2}** | Current HP: {currentHP:F2}/{GetMaxHP()}");

        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }
    }
    
    void Die()
    {
        if (isDying) return; // Prevent double death execution
        
        isDying = true;
        Debug.Log("[PlayerState: HP] **Player has died!**");

        // The PlayerControls script will now check this 'isDying' flag via the public method.
        
        if (reloadSceneOnDeath)
        {
            StartCoroutine(ReloadSceneAfterDelay());
        }
        else
        {
            Debug.Log("[PlayerState: Death] Scene reload disabled for debugging.");
        }
    }

    // Coroutine to wait for the specified delay and then reload the scene.
    private IEnumerator ReloadSceneAfterDelay()
    {
        Debug.Log($"[PlayerState: Death] Reloading scene in {deathReloadDelay:F2} seconds...");
        
        // Wait for the specified duration
        yield return new WaitForSeconds(deathReloadDelay); 

        // Get the current scene name and reload it
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    // --- Power-Up Application Methods ---
    public void ApplyPermanentHPIncrease(float amount)
    {
        bonusMaxHP += amount;
        PlayerPrefs.SetFloat(MaxHPBonusKey, bonusMaxHP);
        PlayerPrefs.Save();
        currentHP = GetMaxHP(); 
        Debug.Log($"[PlayerState: PowerUp] Permanent Max HP increased by +{amount}. New Max: {GetMaxHP()}.");
    }

    public void ApplyPermanentMagicIncrease(float amount)
    {
        bonusMaxMagic += amount;
        PlayerPrefs.SetFloat(MaxMagicBonusKey, bonusMaxMagic);
        PlayerPrefs.Save();
        currentMagic = GetMaxMagic(); 
        Debug.Log($"[PlayerState: PowerUp] Permanent Max Magic increased by +{amount}. New Max: {GetMaxMagic()}.");
    }

    public void ApplySwordDamageMultiplier(float factor)
    {
        swordDamageMultiplier *= factor;
        PlayerPrefs.SetFloat(SwordDamageMultiplierKey, swordDamageMultiplier);
        PlayerPrefs.Save();
        Debug.Log($"[PlayerState: PowerUp] Sword Damage Multiplier applied: x{factor}. New Total Multiplier: x{swordDamageMultiplier:F2}.");
    }

    public void ApplyMagicDamageMultiplier(float factor)
    {
        magicDamageMultiplier *= factor;
        PlayerPrefs.SetFloat(MagicDamageMultiplierKey, magicDamageMultiplier);
        PlayerPrefs.Save();
        Debug.Log($"[PlayerState: PowerUp] Magic Damage Multiplier applied: x{factor}. New Total Multiplier: x{magicDamageMultiplier:F2}.");
    }

    // --- Public Restoration Methods ---
    public void RestoreHealthToMax()
    {
        currentHP = GetMaxHP();
        Debug.Log($"[PlayerState: Heal] Health restored to Max: {currentHP:F2}");
    }

    public void RestoreMagicToMax()
    {
        currentMagic = GetMaxMagic();
        Debug.Log($"[PlayerState: Magic] Magic restored to Max: {currentMagic:F2}");
    }

    // --- Magic & Combat Methods ---
    public void UseMagic(float amount)
    {
        currentMagic = Mathf.Clamp(currentMagic - amount, 0f, GetMaxMagic());
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
    public float GetBaseSwordDamage() => baseSwordDamage; 
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

    // Public method to expose isDying status for external scripts like PlayerControls
    public bool IsDying() => isDying;
}