using UnityEngine;
using System; 

[RequireComponent(typeof(PlayerState))]
public class GauntletAbilities : MonoBehaviour
{
    [Header("Ability Settings")]
    public float fireDrainRate = 10f;       
    public float iceDrainRate = 10f;        
    public float lightDrainRate = 5f;       
    public float invincibleDrainRate = 15f; 

    [Header("Ability Availability Toggles")]
    public bool isFireEnabled = true;
    public bool isIceEnabled = true;
    public bool isInvincibleEnabled = true;
    public bool isLightEnabled = false; 

    [Header("VFX Emitters")]
    public ParticleSystem fireEmitter; 
    public ParticleSystem iceEmitter; 
    public ParticleSystem lightEmitter; 

    [Header("VFX Materials")]
    public Material fireMaterial;
    public Material iceMaterial;
    public Material lightMaterial; 

    [Header("Invincible Visuals")]
    public Renderer playerRenderer; 
    public Material invincibleMaterial; 
    
    private PlayerState playerState;
    
    // MODIFIED: This now stores the entire array of original materials
    private Material[] originalPlayerMaterials; 
    
    private bool gauntletActive = false;
    private AbilityType currentAbility = AbilityType.Fire; 
    private bool isCasting = false; 
    
    private bool isInvincibleActive = false; 

    void Awake()
    {
        playerState = GetComponent<PlayerState>();
        
        if (playerRenderer != null)
        {
            // NEW: Store the entire array of original materials from the Renderer
            originalPlayerMaterials = playerRenderer.sharedMaterials;
        }
        else
        {
             Debug.LogError("Player Renderer not assigned! Invincible visuals will not work.");
        }

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
            if (isInvincibleActive) EndInvincibility(); 
            CycleAbility();
        }

        // 2. Main Input Logic
        if (gauntletActive)
        {
            // --- Channeled Spell Logic (Fire/Ice/Light) ---
            if (currentAbility != AbilityType.Invincible)
            {
                if (Input.GetMouseButtonDown(0) && !isCasting && !playerState.IsInvincible())
                {
                    StartCast();
                }
                else if (Input.GetMouseButtonUp(0) && isCasting)
                {
                    StopCast();
                }
            }
            
            // --- Invincibility Logic ---
            if (currentAbility == AbilityType.Invincible)
            {
                if (!isInvincibleActive)
                {
                    StartInvincibility();
                }
            }
        }
        
        // 3. Stop all channeled effects if gauntlet is released (RMB released)
        if (!gauntletActive)
        {
            if (isCasting) StopCast();
            if (isInvincibleActive) EndInvincibility(); 
        }
        
        // 4. Handle continuous draining for the active effect
        if (isCasting)
        {
            HandleContinuousCast();
        }
        
        if (isInvincibleActive)
        {
            HandleInvincibilityDrain();
        }
    }

    // --- Core Ability Methods ---

    void StopAllEmitters(bool disableObjects = false)
    {
        fireEmitter?.Stop();
        iceEmitter?.Stop();
        lightEmitter?.Stop();
    }

    float GetCurrentChannelDrainRate()
    {
        return currentAbility switch
        {
            AbilityType.Fire => fireDrainRate,
            AbilityType.Ice => iceDrainRate,
            AbilityType.Light => lightDrainRate,
            _ => 0f 
        };
    }
    
    void HandleContinuousCast()
    {
        float magicToDrain = GetCurrentChannelDrainRate() * Time.deltaTime; 
        
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

        if (playerState.GetCurrentMagic() < GetCurrentChannelDrainRate() * Time.deltaTime) 
        {
            Debug.Log("[Gauntlet] Not enough magic to start cast!");
            return;
        }

        isCasting = true;
        
        switch (currentAbility)
        {
            case AbilityType.Fire:
                Debug.Log("[Gauntlet] Fire Channel START!");
                fireEmitter?.Play(); 
                break;
            case AbilityType.Ice:
                Debug.Log("[Gauntlet] Ice Channel START!");
                iceEmitter?.Play(); 
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
        StopAllEmitters();

        Debug.Log($"[Gauntlet] {currentAbility} Channel STOP!");
    }

    void HandleInvincibilityDrain()
    {
        float magicToDrain = invincibleDrainRate * Time.deltaTime;
        
        if (playerState.GetCurrentMagic() > magicToDrain)
        {
            playerState.UseMagic(magicToDrain);
        }
        else
        {
            Debug.Log("[Gauntlet] Invincibility Magic ran out! Ending ability.");
            EndInvincibility();
        }
    }

    // MODIFIED: Sets ALL materials to the invincible material
    void StartInvincibility()
    {
        if (!IsAbilityEnabled(AbilityType.Invincible) || isInvincibleActive) return;

        if (playerState.GetCurrentMagic() < invincibleDrainRate * Time.deltaTime) 
        {
             Debug.Log("[Gauntlet] Not enough magic to activate Invincibility!");
             return;
        }

        if (playerRenderer != null && invincibleMaterial != null)
        {
            // 1. Get the number of materials slots to fill
            int materialCount = playerRenderer.sharedMaterials.Length;
            
            // 2. Create a new array and fill it entirely with the invincible material
            Material[] newMaterials = new Material[materialCount];
            for (int i = 0; i < materialCount; i++)
            {
                newMaterials[i] = invincibleMaterial;
            }

            // 3. Apply the new array, visually overriding the entire model
            playerRenderer.materials = newMaterials;
        }

        playerState.SetInvincible(true);
        isInvincibleActive = true;
        Debug.Log("[Gauntlet] Invincibility activated and draining magic!");
    }

    // MODIFIED: Restores the ENTIRE array of original materials
    void EndInvincibility()
    {
        if (!isInvincibleActive) return;
        
        // Restore the original material array
        if (playerRenderer != null && originalPlayerMaterials != null)
        {
            playerRenderer.materials = originalPlayerMaterials;
        }

        playerState.SetInvincible(false); 
        isInvincibleActive = false;
        Debug.Log("[Gauntlet] Invincibility ended.");
    }
    
    // --- Helper Logic Methods ---

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

        int maxAttempts = Enum.GetValues(typeof(AbilityType)).Length;
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
        int maxAbilities = Enum.GetValues(typeof(AbilityType)).Length;
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

        StopAllEmitters(); 

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
}