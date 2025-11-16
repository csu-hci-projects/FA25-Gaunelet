using UnityEngine;
using UnityEngine.UI; // Required for Slider component

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    public Slider healthSlider; // Drag your HealthBarSlider here
    public Slider magicSlider;  // Drag your MagicBarSlider here

    [Header("Player Reference")]
    public PlayerState playerState; // Drag the GameObject with PlayerState here

    void Start()
    {
        if (playerState == null)
        {
            Debug.LogError("PlayerState reference is missing in UIManager!");
            return;
        }
        if (healthSlider == null || magicSlider == null)
        {
            Debug.LogError("One or both UI Sliders are missing in UIManager!");
            return;
        }

        // --- HEALTH BAR INITIALIZATION ---
        // 1. Set the maximum value
        healthSlider.maxValue = playerState.GetMaxHP(); 
        
        // 2. Set the current value (should be equal to maxHP at start)
        healthSlider.value = playerState.GetCurrentHP(); 
        
        // --- MAGIC BAR INITIALIZATION ---
        // 1. Set the maximum value
        magicSlider.maxValue = playerState.GetMaxMagic(); 
        
        // 2. Set the current value (should be equal to maxMagic at start)
        magicSlider.value = playerState.GetCurrentMagic(); 
        
        // Log confirmation of successful initialization
        Debug.Log("UI Manager initialized: HP and Magic sliders set to starting values.");
    }

    void Update()
    {
        // Continuously update the UI based on current player stats
        // We only update the value here, the max value doesn't change after Start.
        UpdateHealthBar(playerState.GetCurrentHP());
        UpdateMagicBar(playerState.GetCurrentMagic());
    }

    public void UpdateHealthBar(float currentHP)
    {
        // Ensure the current value is always within the bounds of the slider
        healthSlider.value = currentHP;
    }

    public void UpdateMagicBar(float currentMagic)
    {
        // Ensure the current value is always within the bounds of the slider
        magicSlider.value = currentMagic;
    }
}