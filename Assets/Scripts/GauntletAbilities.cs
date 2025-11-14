using UnityEngine;

// Assuming AbilityType enum now includes Light: public enum AbilityType { Fire, Ice, Invincible, Light } 

[RequireComponent(typeof(PlayerState))]
public class GauntletAbilities : MonoBehaviour
{
    [Header("Ability Settings")]
    public float invincibilityDuration = 3f;
    public float magicDrainRate = 10f; 

    [Header("Ability Availability Toggles")]
    public bool isFireEnabled = true;
    public bool isIceEnabled = true;
    public bool isInvincibleEnabled = true;
    public bool isLightEnabled = false; 

    [Header("VFX Emitters")] // RENAMED HEADER
    public ParticleSystem fireEmitter; // NEW DEDICATED EMITTER
    public ParticleSystem iceEmitter;  // NEW DEDICATED EMITTER
    public ParticleSystem lightEmitter; // Existing dedicated emitter

    [Header("VFX Materials")]
    // We keep these for consistency/future use, but the material is set on the emitters themselves now.
    public Material fireMaterial;
    public Material iceMaterial;
    public Material lightMaterial; 

    [Header("Invincible Visuals")]
    public Renderer playerRenderer; 
    public Material invincibleMaterial; 
    
    private PlayerState playerState;
    // We no longer need spellEmitterRenderer
    // private Renderer spellEmitterRenderer; 
    
    private Material originalPlayerMaterial; 
    
    private bool gauntletActive = false;
    private AbilityType currentAbility = AbilityType.Fire; 
    private bool isCasting = false; 

    private float invincibilityEndTime = 0f;

    void Awake()
    {
        playerState = GetComponent<PlayerState>();
        
        // Removed spellEmitterRenderer setup
        
        if (playerRenderer != null)
        {
            originalPlayerMaterial = playerRenderer.material;
        }

        // Ensure all emitters start stopped and disabled.
        StopAllEmitters(true);
    }

    void Start()
    {
        EnsureCurrentAbilityIsEnabled(true);
    }

    void Update()
    {
        gauntletActive = Input.GetMouseButton(1); 

        // 1. Ability Cycling (E key)
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (isCasting) StopCast(); 
            CycleAbility();
        }

        // 2. Channeled Casting Input Logic (ONLY runs if Gauntlet is Active)
        if (gauntletActive)
        {
            // START CASTING: LMB Pressed Down
            if (Input.GetMouseButtonDown(0) && !isCasting && currentAbility != AbilityType.Invincible && !playerState.IsInvincible())
            {
                StartCast();
            }
            // STOP CASTING: LMB Released
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

    // NEW HELPER: Stops all emitters and optionally disables the GameObjects
    void StopAllEmitters(bool disableObjects = false)
    {
        fireEmitter?.Stop();
        iceEmitter?.Stop();
        lightEmitter?.Stop();

        if (disableObjects)
        {
            // It's safer to disable the particle system component or its parent if needed, 
            // but just stopping playback is often sufficient for channeled effects.
        }
    }

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

    void StartCast()
    {
        if (!IsAbilityEnabled(currentAbility) || currentAbility == AbilityType.Invincible) return;

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
                fireEmitter?.Play(); // PLAY DEDICATED EMITTER
                break;
            case AbilityType.Ice:
                Debug.Log("[Gauntlet] Ice Channel START!");
                iceEmitter?.Play(); // PLAY DEDICATED EMITTER
                break;
            case AbilityType.Light:
                Debug.Log("[Gauntlet] Light Channel START!");
                lightEmitter?.Play();
                break;
        }
    }

    void StopCast()
    {
        isCasting = false;
        StopAllEmitters(); // Use the new helper function

        Debug.Log($"[Gauntlet] {currentAbility} Channel STOP!");
    }

    void TryActivateAbility()
    {
        if (currentAbility == AbilityType.Invincible && IsAbilityEnabled(currentAbility))
        {
            StartInvincibility();
        }
    }

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
    
    // MODIFIED: Simplified to only handle visual setup/cleanup, no material swap needed
    void ApplySpellVisuals(AbilityType ability, bool force = false)
    {
        if (!gauntletActive && !force) return;

        StopAllEmitters(); // Ensure all are stopped before potentially activating one for visual setup

        // Now we just ensure the correct emitter's material is set on its renderer if needed
        // (Though ideally, the material is set on the prefab/object itself for Fire/Ice/Light)
        
        if (ability == AbilityType.Fire)
        {
            // Fire setup code here (if needed)
        }
        else if (ability == AbilityType.Ice)
        {
            // Ice setup code here (if needed)
        }
        else if (ability == AbilityType.Light)
        {
            // Light setup code here (if needed)
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