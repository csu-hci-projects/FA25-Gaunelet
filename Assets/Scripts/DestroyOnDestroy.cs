using UnityEngine;

public class DestroyOnDestroy : MonoBehaviour
{
    [Header("Target to Destroy")]
    [Tooltip("Drag the barrier or object that should be destroyed when this enemy dies here.")]
    public GameObject targetToDestroy;

    [Header("UI Feedback")]
    [Tooltip("Reference to the scene's UIManager to display the success message.")]
    public UIManager uiManager; // NOTE: You must drag your UIManager object here in the Inspector!
    [Tooltip("The message displayed when the target is successfully destroyed.")]
    [TextArea(2, 5)]
    public string successMessage = "A path has opened!";

    // OnDestroy is a special Unity function called when the component's GameObject is destroyed.
    void OnDestroy()
    {
        // Safety check to ensure the target object still exists
        if (targetToDestroy != null)
        {
            // Destroy the linked object (the barrier)
            Destroy(targetToDestroy);
            Debug.Log($"[DestroyOnDestroy] Host object ({gameObject.name}) was destroyed. Barrier ({targetToDestroy.name}) destroyed.");

            // Display the success message on the UI
            if (uiManager != null && !string.IsNullOrEmpty(successMessage))
            {
                // Calling DisplayPickupMessage pauses the game and requires Spacebar to dismiss.
                uiManager.DisplayPickupMessage(successMessage);
            }
        }
    }
}