using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Required for accessing Slider components

// Handles scaling the UI Sliders for Health and Magic by 50% when an upgrade is picked up.
// Also updates the Slider's logic (maxValue) to match the new stats immediately.
public class StatMeterScaler : MonoBehaviour
{
    // --- Persistence Keys ---
    private const string HPUPSCALED_KEY = "UI_HP_Scaled";
    private const string MAGICUPSCALED_KEY = "UI_Magic_Scaled";
    
    [Header("UI Slider References")]
    [Tooltip("Drag the entire HP Slider object here.")]
    public Slider hpSlider;
    [Tooltip("Drag the entire Magic Slider object here.")]
    public Slider magicSlider;

    // --- REMOVED: Text Label References are no longer needed as the text is detached in the hierarchy ---

    // The scale factor (1.0 + 0.5 = 1.5 for 50% increase)
    private const float SCALE_FACTOR = 1.5f;

    // Internal state tracking
    private bool isHpScaled = false;
    private bool isMagicScaled = false;

    void Start()
    {
        // 1. Load the persisted state
        isHpScaled = PlayerPrefs.GetInt(HPUPSCALED_KEY, 0) == 1;
        isMagicScaled = PlayerPrefs.GetInt(MAGICUPSCALED_KEY, 0) == 1;

        Debug.Log($"[StatMeterScaler] HP Scaled: {isHpScaled}, Magic Scaled: {isMagicScaled}");

        // 2. Apply the VISUAL scaling if needed. 
        if (isHpScaled)
        {
            ApplyScaleVisuals(hpSlider, SCALE_FACTOR);
        }
        if (isMagicScaled)
        {
            ApplyScaleVisuals(magicSlider, SCALE_FACTOR);
        }
    }
    
    // --- Public Methods for Upgrade Pickups ---

    // Call this method when the player picks up a permanent Health upgrade.
    public void OnHealthUpgradePickedUp()
    {
        if (isHpScaled || hpSlider == null) return;

        // 1. Scale Visuals
        ApplyScaleVisuals(hpSlider, SCALE_FACTOR);
        
        // 2. Update Logic (Critical for the depletion bug)
        // We increase the max value so the bar doesn't stay full while health > 100.
        hpSlider.maxValue *= SCALE_FACTOR; 

        isHpScaled = true;
        PlayerPrefs.SetInt(HPUPSCALED_KEY, 1);
        PlayerPrefs.Save();
        Debug.Log("[StatMeterScaler] HP meter scaled and maxValue updated.");
    }

    // Call this method when the player picks up a permanent Magic upgrade.
    public void OnMagicUpgradePickedUp()
    {
        if (isMagicScaled || magicSlider == null) return;

        // 1. Scale Visuals
        ApplyScaleVisuals(magicSlider, SCALE_FACTOR);

        // 2. Update Logic
        magicSlider.maxValue *= SCALE_FACTOR;

        isMagicScaled = true;
        PlayerPrefs.SetInt(MAGICUPSCALED_KEY, 1);
        PlayerPrefs.Save();
        Debug.Log("[StatMeterScaler] Magic meter scaled and maxValue updated.");
    }

    // --- Private Helper Method ---

    // Applies the scale factor to the width of the Slider root.
    // Uses position compensation to ensure it grows ONLY to the right.
    private void ApplyScaleVisuals(Slider slider, float factor)
    {
        if (slider == null) return;

        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        if (sliderRect == null) return;

        // 1. Calculate dimensions
        float oldWidth = sliderRect.rect.width;
        float newWidth = oldWidth * factor;
        float widthDiff = newWidth - oldWidth;

        // 2. Set the new width directly
        sliderRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);

        // 3. Compensate Slider Position (The "Grow Right" Trick)
        // Calculate the shift needed to keep the left edge pinned.
        float shiftAmount = widthDiff * sliderRect.pivot.x;
        
        // No need for Canvas.ForceUpdateCanvases() or text adjustment now.
        
        // 4. Shift Slider Position (The "Grow Right" Trick)
        Vector2 sliderPos = sliderRect.anchoredPosition;
        sliderPos.x += shiftAmount;
        sliderRect.anchoredPosition = sliderPos;
        
        Debug.Log($"[UI Fix] Slider {sliderRect.gameObject.name} scaled and shifted RIGHT by {shiftAmount} units. New Width: {newWidth}");
    }
    
    // Utility for debugging/resetting persistence in Editor
    [ContextMenu("Clear Meter Scaling Persistence Data")]
    public void ClearScalingData()
    {
        PlayerPrefs.DeleteKey(HPUPSCALED_KEY);
        PlayerPrefs.DeleteKey(MAGICUPSCALED_KEY);
        PlayerPrefs.Save();
        isHpScaled = false;
        isMagicScaled = false;
        Debug.LogWarning("Meter Scaling Data CLEARED. UI will reset on next Start.");
    }
}