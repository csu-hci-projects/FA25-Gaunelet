using UnityEngine;
using System; 
using UnityEngine.SceneManagement; 

[RequireComponent(typeof(PlayerState))]
public class GauntletAbilities : MonoBehaviour
{
    // --- Persistence Keys ---
    private const string FireKey = "Ability_Fire";
    private const string IceKey = "Ability_Ice";
    private const string InvincibleKey = "Ability_Invincible";
    private const string LightKey = "Ability_Light";

    [Header("Ability Settings")]
    public float fireDrainRate = 10f;
    public float iceDrainRate = 10f;
    public float lightDrainRate = 5f;
    public float invincibleDrainRate = 15f; 

    [Header("Ability Availability Toggles")]
    // Inspector values now only serve as a generic fallback for unknown scenes.
    public bool isFireEnabled = true; 
    public bool isIceEnabled = true;
    public bool isInvincibleEnabled = true;
    public bool isLightEnabled = false; 

    // Timing for the Gauntlet activation delay
    [Header("Ability Timing")] 
    public float gauntletReadyDelay = 0.5f; 
    private float gauntletActivateTime = 0f; 

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
    private PlayerControls playerControls; 
    
    private Material[] originalPlayerMaterials; 
    
    private bool gauntletActive = false;
    
    // FIX 1: Default the current ability to None for the start state
    private AbilityType currentAbility = AbilityType.None; 
    private bool isCasting = false; 
    
    private bool isInvincibleActive = false; 

    void Awake()
    {
        playerState = GetComponent<PlayerState>();
        playerControls = GetComponent<PlayerControls>();
        
        if (playerControls == null)
        {
            Debug.LogError("GauntletAbilities: PlayerControls component not found!");
        }

        if (playerRenderer != null)
        {
            // Store the original materials when the component starts
            originalPlayerMaterials = playerRenderer.sharedMaterials;
        }
        else
        {
            Debug.LogError("Player Renderer not assigned! Invincible visuals will not work.");
        }

        // Initialize by stopping all emitters
        ClearAllEmittersVFX();
    }

    void Start()
    {
        // Load or Initialize ability states based on the scene index
        InitializeAbilityStates(); 
        
        // Ensure the current ability is one that is enabled and set initial visuals
        EnsureCurrentAbilityIsEnabled(true);
    }

    // Utility for debugging persistence
    [ContextMenu("Clear All Ability Persistence Data")]
    public void ClearAllAbilityData()
    {
        PlayerPrefs.DeleteKey(FireKey);
        PlayerPrefs.DeleteKey(IceKey);
        PlayerPrefs.DeleteKey(InvincibleKey);
        PlayerPrefs.DeleteKey(LightKey);
        PlayerPrefs.Save();
        Debug.LogWarning("ALL ABILITY PERSISTENCE DATA CLEARED. Next scene load will use scene defaults.");
        // Reload state to reflect the clear (optional)
        InitializeAbilityStates(); 
    }

    // Handles initial setup by strictly enforcing the scene's minimum required abilities.
    // New scene logic:
    // Scene 1 (Woodland): None
    // Scene 2 (Dungeon): Fire, Ice
    // Scene 3 (Labyrinth): Fire, Ice, Invincible
    private void InitializeAbilityStates()
    {
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;

        // 1. Reset all fields to FALSE. This is the baseline state.
        isFireEnabled = false;
        isIceEnabled = false;
        isInvincibleEnabled = false;
        isLightEnabled = false;

        // SCENE 0: Full Reset (Clears persistence)
        if (sceneIndex == 0)
        {
            Debug.Log("[Gauntlet] Scene 0 detected. Disabling all abilities and clearing persistence.");
            // SetAbilityEnabled ensures the PlayerPrefs key is set to 0 (disabled)
            SetAbilityEnabled(AbilityType.Fire, false);
            SetAbilityEnabled(AbilityType.Ice, false);
            SetAbilityEnabled(AbilityType.Invincible, false);
            SetAbilityEnabled(AbilityType.Light, false);
            return;
        }

        // 2. Apply Scene Minimum Requirements (STRICTLY based on scene index)
        
        if (sceneIndex == 1) // Scene 1 (Woodland): No abilities enabled (all remain false)
        {
            Debug.Log("[Gauntlet] Scene 1 Woodland enforcement: No abilities enabled at start.");
        }
        else if (sceneIndex == 2) // Scene 2 (Dungeon): Fire and Ice enabled
        {
            Debug.Log("[Gauntlet] Scene 2 Dungeon enforcement: Fire and Ice enabled.");
            isFireEnabled = true;
            isIceEnabled = true;
        }
        else if (sceneIndex >= 3) // Scene 3 (Labyrinth) and higher: Fire, Ice, and Invincible enabled
        {
            Debug.Log("[Gauntlet] Scene 3 Labyrinth+ enforcement: Fire, Ice, and Invincible enabled.");
            isFireEnabled = true;
            isIceEnabled = true;
            isInvincibleEnabled = true;
        }

        // 3. Load Persistence ONLY for abilities that are OPTIONAL and PERMANENTLY UNLOCKED.
        
        // --- Persistence Check: Light (Ability 4) ---
        // Light is only permanently unlocked starting from Scene 3 or higher.
        if (sceneIndex >= 3)
        {
            int lightInt = PlayerPrefs.GetInt(LightKey, 0); 
            if (lightInt == 1)
            {
                isLightEnabled = true;
                Debug.Log("[Gauntlet Load] Light loaded from persistence (unlocked).");
            }
        }
        else
        {
            // For Scene 1 and 2, isLightEnabled remains false from step 1, 
            // ensuring it must be acquired via pickup.
            Debug.Log($"[Gauntlet Load] Light persistence skipped for Scene {sceneIndex}. Must be acquired in-scene.");
        }
    }
    
    // Public method for the AbilityPickup to call
    public void EnableAbility(AbilityType ability)
    {
        // 1. Update the in-memory state
        switch (ability)
        {
            case AbilityType.Fire:
                isFireEnabled = true;
                break;
            case AbilityType.Ice:
                isIceEnabled = true;
                break;
            case AbilityType.Invincible:
                isInvincibleEnabled = true;
                break;
            case AbilityType.Light:
                isLightEnabled = true;
                break;
        }

        // 2. Save the new state permanently (set to 1)
        SaveAbilityState(ability, true);

        // 3. If the newly enabled ability is the first one found, switch to it automatically
        if (!IsAbilityEnabled(currentAbility) || currentAbility == AbilityType.None)
        {
            currentAbility = ability;
            ApplySpellVisuals(currentAbility, true);
        }
        
        Debug.Log($"[Gauntlet] {ability} ability is now permanently ENABLED.");
    }
    
    // Logic to save a single ability state
    private void SaveAbilityState(AbilityType ability, bool isEnabled)
    {
        string key = GetAbilityKey(ability);
        // PlayerPrefs stores 1 for true, 0 for false
        PlayerPrefs.SetInt(key, isEnabled ? 1 : 0);
        PlayerPrefs.Save(); // Ensure data is written to disk immediately
        Debug.Log($"[Gauntlet] Saved {key} state: {isEnabled}");
    }

    // Helper to set ability state (used by InitializeAbilityStates - primarily for Scene 0 reset)
    private void SetAbilityEnabled(AbilityType ability, bool isEnabled)
    {
        switch (ability)
        {
            case AbilityType.Fire: isFireEnabled = isEnabled; break;
            case AbilityType.Ice: isIceEnabled = isEnabled; break;
            case AbilityType.Invincible: isInvincibleEnabled = isEnabled; break;
            case AbilityType.Light: isLightEnabled = isEnabled; break;
            case AbilityType.None: break; // None is never enabled
        }
        // When called from Scene 0, this saves the state as 0 for persistence reset
        SaveAbilityState(ability, isEnabled); 
    }
    
    // Helper to get the PlayerPrefs key
    private string GetAbilityKey(AbilityType ability)
    {
        return ability switch
        {
            AbilityType.Fire => FireKey,
            AbilityType.Ice => IceKey,
            AbilityType.Invincible => InvincibleKey,
            AbilityType.Light => LightKey,
            _ => throw new ArgumentException("Invalid AbilityType")
        };
    }

    void Update()
    {
        if (playerState == null) return;
        
        bool wasGauntletActive = gauntletActive; // Store previous state
        gauntletActive = Input.GetMouseButton(1); // Update current state

        // Track when Gauntlet Mode is first activated (RMB press)
        if (gauntletActive && !wasGauntletActive)
        {
            gauntletActivateTime = Time.time;
            // When RMB is first pressed, ensure we show the visual for the selected ability
            ApplySpellVisuals(currentAbility, true);
        }
        
        // When Gauntlet Mode is released (RMB released)
        if (!gauntletActive && wasGauntletActive)
        {
             // When gauntlet is released, we explicitly stop all visuals immediately
             ApplySpellVisuals(AbilityType.None, false); 
        }


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
                // Check if LMB is currently held down (GetMouseButton(0))
                if (Input.GetMouseButton(0))
                {
                    // If the gauntlet is ready, we are not already casting, AND the player is not currently invincible, start the cast.
                    if (!isCasting && IsReadyToCast() && !isInvincibleActive) 
                    {
                        StartCast();
                    }
                }
                // If LMB is not held down, but we are currently casting, stop the cast.
                else if (isCasting)
                {
                    StopCast();
                }
            }
            
            // --- Invincibility Logic ---
            if (currentAbility == AbilityType.Invincible)
            {
                // Invincibility is activated immediately when RMB is held and it is the current ability.
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

    // Helper function to check if the Gauntlet delay has passed
    private bool IsReadyToCast()
    {
        return Time.time >= gauntletActivateTime + gauntletReadyDelay;
    }

    // Aims the emitter to match the player model's forward direction
    void AimEmitterAtModelForward(ParticleSystem emitter)
    {
        if (playerControls != null && playerControls.modelTransform != null && emitter != null)
        {
            // Set the emitter's world rotation to exactly match the player model's world rotation.
            // This ensures the particles launch in the direction the model is visually facing.
            emitter.transform.rotation = playerControls.modelTransform.rotation;
            Debug.Log($"[Gauntlet] {emitter.name} rotation forced to player model rotation.");
        }
    }

    // --- Core Ability Methods ---
    
    // Aggressively stops and clears all particles. Used for cycling or cleanup.
    void ClearAllEmittersVFX()
    {
        fireEmitter?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        iceEmitter?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        lightEmitter?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
        
        // IMPORTANT: We MUST call ApplySpellVisuals here to ensure the material is correct (e.g., if Invincible was active).
        ApplySpellVisuals(currentAbility, true); 

        switch (currentAbility)
        {
            case AbilityType.Fire:
                // 1. Aggressively clear ALL old VFX before starting this channeled beam
                ClearAllEmittersVFX();
                Debug.Log("[Gauntlet] Fire Channel START!");
                // Explicitly check for null and play
                if (fireEmitter == null) { Debug.LogError("Fire Emitter is not assigned in the Inspector!"); return; }
                AimEmitterAtModelForward(fireEmitter); 
                fireEmitter.Play(); 
                break;
                
            case AbilityType.Ice:
                // 1. Aggressively clear ALL old VFX before starting this channeled beam
                ClearAllEmittersVFX();
                Debug.Log("[Gauntlet] Ice Channel START!");
                // Explicitly check for null and play
                if (iceEmitter == null) { Debug.LogError("Ice Emitter is not assigned in the Inspector!"); return; }
                AimEmitterAtModelForward(iceEmitter); 
                iceEmitter.Play(); 
                break;
                
            case AbilityType.Light:
                // DO NOT call ClearAllEmittersVFX here. This allows previous Light particles to persist.
                Debug.Log("[Gauntlet] Light Channel START! Previous lights will persist.");
                // Explicitly check for null and play
                if (lightEmitter == null) { Debug.LogError("Light Emitter is not assigned in the Inspector! Check the VFX Emitters section."); return; }
                AimEmitterAtModelForward(lightEmitter);
                lightEmitter.Play();
                break;
        }
    }

    void StopCast()
    {
        if (!isCasting) return;

        isCasting = false;
        
        ParticleSystem activeEmitter = currentAbility switch
        {
            AbilityType.Fire => fireEmitter,
            AbilityType.Ice => iceEmitter,
            AbilityType.Light => lightEmitter,
            _ => null
        };

        if (activeEmitter != null)
        {
            // By using StopEmitting, stop generating new particles, but allow existing ones
            // to continue until their individual lifespan expires (natural fade-out).
            activeEmitter.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

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

    void StartInvincibility()
    {
        // Check availability first. If it's disabled, the persistence logic is working.
        if (!IsAbilityEnabled(AbilityType.Invincible) || isInvincibleActive) return;

        if (playerState.GetCurrentMagic() < invincibleDrainRate * Time.deltaTime) 
        {
            Debug.Log("[Gauntlet] Not enough magic to activate Invincibility!");
            return;
        }

        // Must stop any active channeling VFX before activating Invincibility visual
        if (isCasting) StopCast(); 
        
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

    void EndInvincibility()
    {
        if (!isInvincibleActive) return;
        
        // Restore the original material array
        if (playerRenderer != null && originalPlayerMaterials != null)
        {
            // IMPORTANT: Assigning the cached array restores the original appearance
            playerRenderer.materials = originalPlayerMaterials; 
        }

        playerState.SetInvincible(false); 
        isInvincibleActive = false;
        Debug.Log("[Gauntlet] Invincibility ended.");
    }
    
    // --- Helper Logic Methods ---

    // Public accessor for other scripts to check the currently selected ability type.
    public AbilityType GetCurrentAbility()
    {
        return currentAbility;
    }

    // Checks the current in-memory state of an ability (determined by Scene Default OR Persistence).
    public bool IsAbilityEnabled(AbilityType ability)
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
        int startIndex = (int)currentAbility; // Starts checking from AbilityType.None (0)
        int nextIndex;

        for (int i = 1; i < maxAttempts; i++) // Start loop from 1 to skip AbilityType.None
        {
            nextIndex = (startIndex + i) % maxAttempts;
            AbilityType nextAbility = (AbilityType)nextIndex;

            if (nextAbility == AbilityType.None) continue; // Skip the None entry again if it wraps around

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
        
        // FIX 2: If the loop completes and no enabled abilities are found, explicitly set to None.
        // This ensures the currentAbility is not stuck on a disabled spell like 'Fire' (if it was the default).
        currentAbility = AbilityType.None;
        Debug.LogWarning("[Gauntlet] No abilities are currently enabled! Setting currentAbility to None.");
    }

    void CycleAbility()
    {
        int maxAbilities = Enum.GetValues(typeof(AbilityType)).Length;
        int nextIndex = (int)currentAbility;
        AbilityType previous = currentAbility;
        
        // If the current spell is 'None', start cycling from the beginning (index 1 for Fire)
        if (currentAbility == AbilityType.None)
        {
            nextIndex = 0; // The AbilityType enum starts at 0 (None)
        }

        for (int i = 1; i <= maxAbilities; i++)
        {
            nextIndex = (nextIndex + 1) % maxAbilities;
            AbilityType nextAbility = (AbilityType)nextIndex;
            
            if (nextAbility == AbilityType.None) continue; // Skip the 'None' state when cycling

            if (IsAbilityEnabled(nextAbility))
            {
                currentAbility = nextAbility;
                
                Debug.Log($"[Gauntlet] Ability switched: {previous} -> {currentAbility}");
                
                // 1. Aggressively clear old VFX before applying new visual
                ClearAllEmittersVFX(); 
                
                // CRUCIAL: Call ApplySpellVisuals to ensure visuals are correct (e.g. reset materials if needed).
                ApplySpellVisuals(currentAbility, true); 

                return;
            }
        }
        
        // If the cycle completes and no enabled spells are found, revert to None.
        currentAbility = AbilityType.None;
        Debug.LogWarning("[Gauntlet] No available ability to switch to! Reverting to None.");
    }
    
    // Handles applying the ability's material to the player model when the gauntlet is active.
    // This function now ONLY handles restoring the original materials (AbilityType.None or Fire/Ice/Light 
    // when Invincible is not active) or is a safety check. The full Invincible override is handled 
    // in StartInvincibility().
    void ApplySpellVisuals(AbilityType ability, bool force = false)
    {
        // If we are not actively holding RMB, only proceed if forced (e.g., in Start() or Cycle())
        if (!gauntletActive && !force && ability != AbilityType.None) return;

        // If the player is currently invincible, do nothing here. Invincible is handled entirely 
        // by StartInvincibility() and EndInvincibility().
        if (isInvincibleActive) return; 

        if (playerRenderer == null || originalPlayerMaterials == null || originalPlayerMaterials.Length == 0) return;

        // --- Handle Gauntlet Release (Return to Normal) or Non-Invincible Abilities ---
        
        // If ability is None (RMB released) OR Fire/Ice/Light (RMB held, but no spell material effect allowed), 
        // ensure original materials are applied/restored.
        if (ability == AbilityType.None || ability == AbilityType.Fire || ability == AbilityType.Ice || ability == AbilityType.Light)
        {
             // When the gauntlet is deactivated (RMB released) or RMB is held with a channeled spell selected,
             // we restore the entire original material array to prevent discoloration.
             playerRenderer.materials = originalPlayerMaterials; 
             return;
        }
    }
}