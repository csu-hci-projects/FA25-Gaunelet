using UnityEngine;
using TMPro;

public class SignInfo : MonoBehaviour
{
    // === MODIFIED: Public field, editable in the Inspector ===
    [Header("Sign Content")]
    [TextArea(3, 10)] // Makes the input field multi-line in the Inspector
    public string signMessage = 
        "Four forest objects are needed to advance to the cult dungeon!\n" +
        "Find and active them with the action button (space) to clear the way\n\n" +
        "1. Southeast: Bush\n" +
        "2. Southwest: Stump\n" +
        "3. Northwest: Mushroom\n" +
        "4. Northeast: You will figure it out... FIGHT!";

    [Header("UI References")]
    public GameObject displayPanel; 
    public TextMeshProUGUI displayText; 

    private bool isTextShowing = false;
    private bool isPlayerInRange = false;

    // The tag used to identify the player object
    private const string PLAYER_TAG = "Player"; 

    // --- Interaction Logic ---
    
    void Update()
    {
        // 1. Check for the Space key press ONLY if the player is within the trigger range
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.Space))
        {
            ToggleTextDisplay();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger is the player
        if (other.CompareTag(PLAYER_TAG))
        {
            isPlayerInRange = true;
            // You could add logic here to show a small "Press Space" prompt to the player
            Debug.Log("Player entered sign range. Press Space to read.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if the object exiting the trigger is the player
        if (other.CompareTag(PLAYER_TAG))
        {
            isPlayerInRange = false;
            // Force the UI closed when the player walks away.
            HideText();
            // You could add logic here to hide the small "Press Space" prompt
            Debug.Log("Player left sign range.");
        }
    }

    // --- UI Management Methods ---

    public void ToggleTextDisplay()
    {
        // Check both UI references before proceeding
        if (displayPanel == null || displayText == null)
        {
            Debug.LogError("SignInfo UI references are missing on: " + gameObject.name);
            return;
        }

        isTextShowing = !isTextShowing;
        
        if (isTextShowing)
        {
            // Set the text using the public variable and activate the panel
            displayText.text = signMessage;
            displayPanel.SetActive(true);
        }
        else
        {
            // Deactivate the panel
            displayPanel.SetActive(false);
        }
    }

    // Method to force the UI closed when the player walks away.
    public void HideText()
    {
        // Only proceed if the panel reference is set
        if (displayPanel != null)
        {
            // Only deactivate if it's currently active
            if (displayPanel.activeSelf)
            {
                displayPanel.SetActive(false);
                isTextShowing = false;
            }
        }
    }
}