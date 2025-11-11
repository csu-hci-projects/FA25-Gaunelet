using UnityEngine;
using TMPro;

public class SignInfo : MonoBehaviour
{
    // === MODIFIED: Private constant string defined in code ===
    // This value cannot be changed in the Inspector
    private const string SIGN_MESSAGE = "Four forest objects are needed to advance to the cult dungeon!\n\n1. Southeast: Bush\n2. Southwest: Stump\n3. Northwest: Mushroom\n4. Northeast: You will figure it out... FIGHT!";
    // =========================================================

    [Header("UI References")]
    public GameObject displayPanel; 
    public TextMeshProUGUI displayText; 

    private bool isTextShowing = false;

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
            // Set the text using the private constant
            displayText.text = SIGN_MESSAGE;
            displayPanel.SetActive(true);
        }
        else
        {
            displayPanel.SetActive(false);
        }
    }

    // Method to force the UI closed when the player walks away.
    public void HideText()
    {
        if (displayPanel != null)
        {
            if (displayPanel.activeSelf)
            {
                displayPanel.SetActive(false);
                isTextShowing = false;
            }
        }
    }
}
