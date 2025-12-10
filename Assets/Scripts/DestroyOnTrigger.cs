using UnityEngine;

//A reusable trigger volume that sends a target GameObject and a message to the UIManager 
//for destruction and display upon key press. The actual destruction is delayed until the message is dismissed.
public class DestroyOnTrigger : MonoBehaviour
{
    [Header("Action Configuration")]
    [Tooltip("The GameObject that will be destroyed when the player dismisses the UI message.")]
    [SerializeField] private GameObject targetToDestroy;
    
    [Tooltip("The message that will be displayed on the Canvas when the action is successful.")]
    [TextArea(3, 5)]
    [SerializeField] private string messageToDisplay = "Barrier removed!";

    [Header("Trigger Settings")]
    [Tooltip("The tag of the object expected to enter the trigger (e.g., 'Player').")]
    [SerializeField] private string requiredTag = "Player";
    
    [Tooltip("The input key the player must press to activate the action.")]
    [SerializeField] private KeyCode actionKey = KeyCode.Space;

    private bool playerIsInRange = false;

    // Collider Detection

    // Called when another collider enters the trigger collider
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(requiredTag))
        {
            playerIsInRange = true;
            Debug.Log($"[DestroyOnTrigger] {requiredTag} is in range. Press {actionKey} to activate action.");
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

    // Input and Action

    void Update()
    {
        // Check if the player is in range AND the action key is pressed down this frame
        if (playerIsInRange && Input.GetKeyDown(actionKey))
        {
            PerformAction();
        }
    }

    private void PerformAction()
    {
        if (targetToDestroy != null)
        {
            
            if (UIManager.Instance != null)
            {
                // 1. Send the message and the object to the UIManager for delayed destruction
                UIManager.Instance.DisplayActionMessage(messageToDisplay, targetToDestroy);
                
                // 2. Disable this script instantly after triggering the message.
                // This prevents the player from spamming the destroy action while the game is paused.
                enabled = false;
                
                Debug.Log($"[DestroyOnTrigger ACTIVATED] Action triggered. Message displayed, destruction of {targetToDestroy.name} scheduled for dismissal.");
            }
            else
            {
                Debug.LogError("[DestroyOnTrigger] UIManager.Instance is missing! Cannot display message or schedule destruction. Destroying immediately for fallback.");
                Destroy(targetToDestroy);
            }
        }
        else
        {
            Debug.LogError("[DestroyOnTrigger] Target to Destroy is null! Cannot perform action.");
            enabled = false;
        }
    }
}