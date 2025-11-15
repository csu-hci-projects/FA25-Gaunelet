using UnityEngine;

public class DestroyOnTrigger : MonoBehaviour
{
    [Header("Target & Input")]
    [Tooltip("The GameObject that will be destroyed when triggered.")]
    [SerializeField] private GameObject targetToDestroy;

    [Tooltip("The tag of the object expected to enter the trigger (e.g., 'Player').")]
    [SerializeField] private string requiredTag = "Player";
    
    [Tooltip("The input key the player must press to activate the destruction.")]
    [SerializeField] private KeyCode actionKey = KeyCode.Space;

    private bool playerIsInRange = false;

    // --- Collider Detection ---

    // Called when another collider enters the trigger collider
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(requiredTag))
        {
            playerIsInRange = true;
            Debug.Log($"[DestroyOnTrigger] {requiredTag} is in range. Press {actionKey} to destroy {targetToDestroy.name}.");
        }
    }

    // Called when the other collider exits the trigger collider
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(requiredTag))
        {
            playerIsInRange = false;
            Debug.Log($"[DestroyOnTrigger] {requiredTag} is out of range.");
        }
    }

    // --- Input and Destruction ---

    void Update()
    {
        // Check if the player is in range AND the action key is pressed down this frame
        if (playerIsInRange && Input.GetKeyDown(actionKey))
        {
            TryDestroyTarget();
        }
    }

    private void TryDestroyTarget()
    {
        if (targetToDestroy != null)
        {
            // NEW: Debug statement confirming the successful destruction
            Debug.Log($"[DestroyOnTrigger SUCCESS] Action triggered by {requiredTag}! **DESTROYED: {targetToDestroy.name}**");
            
            Destroy(targetToDestroy);
            
            // Disable this script to prevent further attempts/errors
            enabled = false;
        }
        else
        {
            Debug.LogError("[DestroyOnTrigger] Target to Destroy is null! Cannot perform action.");
            // Disable this script since it's now useless
            enabled = false;
        }
    }
}