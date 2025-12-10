using UnityEngine;

// Handles the key collection logic. When the player (tagged "Player")
// enters the trigger volume, it destroys a specified target door, 
// displays a success message via the UIManager, and then destroys itself.
public class Key : MonoBehaviour
{
    [Header("Door Target")]
    [Tooltip("The GameObject (usually a door or barrier) that this key will destroy.")]
    public GameObject doorTarget;

    [Header("UI Feedback")]
    [Tooltip("Reference to the scene's UIManager to display the success message.")]
    public UIManager uiManager; // Remember to drag the UIManager GameObject here!
    [Tooltip("The message displayed when the key is picked up and the door is destroyed.")]
    [TextArea(2, 5)]
    public string successMessage = "Key acquired! The path ahead is now open.";

    private const string PlayerTag = "Player";

    void Start()
    {
        // Basic safety checks
        if (doorTarget == null)
        {
            Debug.LogError($"Key on {gameObject.name} is missing a Door Target assignment!");
        }

        if (uiManager == null)
        {
            // Try to find the UIManager using the Singleton pattern if not assigned
            uiManager = UIManager.Instance;
            if (uiManager == null)
            {
                Debug.LogError($"Key on {gameObject.name} could not find UIManager instance.");
            }
        }
    }

    // Called when another collider enters this key's trigger volume.
    // <param name="other">The Collider that entered the trigger.</param>
    void OnTriggerEnter(Collider other)
    {
        // 1. Check if the object entering the trigger is the Player
        if (other.CompareTag(PlayerTag))
        {
            Debug.Log($"[Key] Key collected by {other.gameObject.name}. Processing effects...");

            // 2. Destroy the target door/barrier if it still exists
            if (doorTarget != null)
            {
                Destroy(doorTarget);
                Debug.Log($"[Key] Successfully destroyed door: {doorTarget.name}.");
                
                // 3. Display the message using the UIManager (pauses game)
                if (uiManager != null && !string.IsNullOrEmpty(successMessage))
                {
                    uiManager.DisplayPickupMessage(successMessage);
                }
            }
            else
            {
                 Debug.LogWarning("[Key] Door target was already null or unassigned. Only message will be shown.");
                 // If the door is already gone, still show a message if one exists.
                 if (uiManager != null && !string.IsNullOrEmpty(successMessage))
                 {
                    // If using DisplayPickupMessage, this will pause the game.
                    uiManager.DisplayPickupMessage(successMessage);
                 }
            }
            
            // 4. Destroy the key item itself
            Destroy(gameObject);
        }
    }
}