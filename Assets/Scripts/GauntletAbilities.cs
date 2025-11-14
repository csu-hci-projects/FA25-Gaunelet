using UnityEngine;

// Assuming AbilityType enum now includes Light: public enum AbilityType { Fire, Ice, Invincible, Light } 

[RequireComponent(typeof(PlayerState))]
public class GauntletAbilities : MonoBehaviour
{
    [Header("Ability Settings")]
    public float invincibilityDuration = 3f;
    // NEW: Magic drain rate (per second) for channeled spells
    public float magicDrainRate = 10f; 

    [Header("Ability Availability Toggles")]
    public bool isFireEnabled = true;
    public bool isIceEnabled = true;
    public bool isInvincibleEnabled = true;
    public bool isLightEnabled = false; 

    [Header("VFX References")]
    public ParticleSystem spellEmitter; // Used for Fire/Ice
    public ParticleSystem lightEmitter; // Dedicated emitter for Light

    [Header("VFX Materials")]
    public Material fireMaterial;
    public Material iceMaterial;
    public Material lightMaterial; 

    [Header("Invincible Visuals")]
    public Renderer playerRenderer; 
    public Material invincibleMaterial; 
    
    private PlayerState playerState;
    private Renderer spellEmitterRenderer; 
    
    private Material originalPlayerMaterial; 
    
    private bool gauntletActive = false;
    private AbilityType currentAbility = AbilityType.Fire; 
    // NEW: Tracks if a continuous spell is currently running
    private bool isCasting = false; 

    private float invincibilityEndTime = 0f;

    void Awake()
    {
        playerState = GetComponent<PlayerState>();
        
        if (spellEmitter != null)
        {
            spellEmitterRenderer = spellEmitter.GetComponent<Renderer>();
        }
        
        if (playerRenderer != null)
        {
            originalPlayerMaterial = playerRenderer.material;
        }
    }

    void Start()
    {
        // Initialize the starting ability state in Start()
        EnsureCurrentAbilityIsEnabled(true);
    }

    void Update()
    {
        gauntletActive = Input.GetMouseButton(1); 

        // 1. Ability Cycling (E key) - Independent of Gauntlet Mode
        if (Input.GetKeyDown(KeyCode.E))
        {
            // Stop casting if cycling in the middle of a channeled spell
            if (isCasting) StopCast(); 
            CycleAbility();
        }

        // 2. Channeled Casting Input Logic (ONLY runs if Gauntlet is Active)
        if (gauntletActive)
        {
            // START CASTING: LMB Pressed Down (must not be casting, be a channeled spell, and not be invincible)
            if (Input.GetMouseButtonDown(0) && !isCasting && currentAbility != AbilityType.Invincible && !playerState.IsInvincible())
            {
                StartCast();
            }
            // STOP CASTING: LMB Released OR if we release the gauntlet while casting (handled below)
            else if (Input.GetMouseButtonUp(0) && isCasting)
            {
                StopCast();
            }
            // One-shot ability: Activate Invincible on LMB press
            else if (Input.GetMouseButtonDown(0) && currentAbility == AbilityType.Invincible && !playerState.IsInvincible())
            {
                TryActivateAbility();
            }

            // Invincibility timer check
            if (Time.time > invincibilityEndTime && playerState.IsInvincible())
            {
                EndInvincibility(); 
            }
        }
        
        // 3. Stop casting if gauntlet is released (RMB released)
        if (!gauntletActive && isCasting)
        {
            StopCast();
        }

        // 4. Handle continuous draining and effects
        if (isCasting)
        {
            HandleContinuousCast();
        }
    }

    // NEW: Function to handle magic drain and checks while casting
    void HandleContinuousCast()
    {
        float magicToDrain = magicDrainRate * Time.deltaTime;
        
        if (playerState.GetCurrentMagic() > magicToDrain)
        {
            playerState.UseMagic(magicToDrain);
            // TODO: Add continuous damage/effect application here
        }
        else
        {
            Debug.Log("[Gauntlet] Magic ran out! Stopping cast.");
            StopCast();
        }
    }

    // NEW: Handles the initial click to start the channeled spell
    void StartCast()
    {
        if (!IsAbilityEnabled(currentAbility) || currentAbility == AbilityType.Invincible) return;

        // Require at least 1 frame's worth of magic to start
        if (playerState.GetCurrentMagic() < magicDrainRate * Time.deltaTime) 
        {
            Debug.Log("[Gauntlet] Not enough magic to start cast!");
            return;
        }

        isCasting = true;
        
        switch (currentAbility)
        {
            case AbilityType.Fire:
                Debug.Log("[Gauntlet] Fire Channel START!");
                spellEmitter?.Play();
                break;
            case AbilityType.Ice:
                Debug.Log("[Gauntlet] Ice Channel START!");
                spellEmitter?.Play();
                break;
            case AbilityType.Light:
                Debug.Log("[Gauntlet] Light Channel START!");
                lightEmitter?.Play();
                break;
        }
    }

    // NEW: Handles the stop/release of the channeled spell
    void StopCast()
    {
        isCasting = false;

        spellEmitter?.Stop();
        lightEmitter?.Stop();

        Debug.Log($"[Gauntlet] {currentAbility} Channel STOP!");
    }

    // Invincible uses this one-shot trigger now
    void TryActivateAbility()
    {
        if (currentAbility == AbilityType.Invincible && IsAbilityEnabled(currentAbility))
        {
            StartInvincibility();
        }
    }

    // --- Helper methods (unchanged) ---
    private bool IsAbilityEnabled(AbilityType ability)
    {
        return ability switch
        {
            AbilityType.Fire => isFireEnabled,
            AbilityType.Ice => isIceEnabled,
            AbilityType.Invincible => isInvincibleEnabled,
            AbilityType.Light => isLightEnabled,
            _ => false,
        };
    }
    
    void EnsureCurrentAbilityIsEnabled(bool forceVisuals = false)
    {
        if (IsAbilityEnabled(currentAbility))
        {
            if (forceVisuals)
            {
                ApplySpellVisuals(currentAbility, true);
                Debug.Log($"[Gauntlet] Initial ability set to default enabled state: {currentAbility}");
            }
            return;
        }

        int maxAttempts = System.Enum.GetValues(typeof(AbilityType)).Length;
        int startIndex = (int)currentAbility;
        int nextIndex;

        for (int i = 1; i <= maxAttempts; i++)
        {
            nextIndex = (startIndex + i) % maxAttempts;
            AbilityType nextAbility = (AbilityType)nextIndex;

            if (IsAbilityEnabled(nextAbility))
            {
                currentAbility = nextAbility;
                if (forceVisuals)
                {
                    ApplySpellVisuals(currentAbility, true);
                }
                Debug.Log($"[Gauntlet] Initial ability set to first found enabled state: {currentAbility}");
                return;
            }
        }
        Debug.LogWarning("[Gauntlet] No abilities are currently enabled! Cycling will not work.");
    }

    void CycleAbility()
    {
        int maxAbilities = System.Enum.GetValues(typeof(AbilityType)).Length;
        int nextIndex = (int)currentAbility;
        AbilityType previous = currentAbility;
        
        for (int i = 1; i <= maxAbilities; i++)
        {
            nextIndex = (nextIndex + 1) % maxAbilities;
            AbilityType nextAbility = (AbilityType)nextIndex;

            if (IsAbilityEnabled(nextAbility))
            {
                currentAbility = nextAbility;
                
                Debug.Log($"[Gauntlet] Ability switched: {previous} -> {currentAbility}");
                
                if (gauntletActive)
                {
                    ApplySpellVisuals(currentAbility);
                }
                return;
            }
        }
        
        Debug.LogWarning("[Gauntlet] No available ability to switch to!");
    }
    
    void ApplySpellVisuals(AbilityType ability, bool force = false)
    {
        if (!gauntletActive && !force) return;

        spellEmitter?.Stop();
        lightEmitter?.Stop();

        if (ability == AbilityType.Fire || ability == AbilityType.Ice)
        {
            if (spellEmitterRenderer != null)
            {
                spellEmitterRenderer.material = (ability == AbilityType.Fire) ? fireMaterial : iceMaterial;
            }
        }
        else if (ability == AbilityType.Light)
        {
            if (lightEmitter != null)
            {
                 Renderer lightRenderer = lightEmitter.GetComponent<Renderer>();
                 if (lightRenderer != null && lightRenderer.material != lightMaterial)
                 {
                      lightRenderer.material = lightMaterial;
                 }
            }
        }
    }
    
    void StartInvincibility()
    {
        if (playerRenderer != null && invincibleMaterial != null)
        {
            playerRenderer.material = invincibleMaterial;
        }

        playerState.SetInvincible(true);
        invincibilityEndTime = Time.time + invincibilityDuration;
        Debug.Log("[Gauntlet] Invincibility activated!");
    }

    void EndInvincibility()
    {
        if (playerRenderer != null && originalPlayerMaterial != null)
        {
            playerRenderer.material = originalPlayerMaterial;
        }

        playerState.SetInvincible(false); 
        Debug.Log("[Gauntlet] Invincibility ended.");
    }
}