using UnityEngine;
using TMPro; // Required for TextMeshProUGUI
using System.Collections; // Required for Coroutines

// Displays a customizable introduction message when the scene loads.
// This script manages its own UI panel and handles pausing/unpausing the game.
// This component should be attached to the Canvas in scenes 1, 2, and 3.
public class MessageLoader : MonoBehaviour
{
    [Header("UI Message")]
    [Tooltip("The introductory text to display on screen when this level loads.")]
    [TextArea(3, 8)]
    public string sceneMessage = "Welcome to the level. Press Space to continue.";

    [Header("UI References (ASSIGN IN INSPECTOR)")]
    [Tooltip("The actual GameObject panel to enable/disable (e.g., Sign Panel).")]
    public GameObject displayPanel; 
    
    [Tooltip("The TextMeshProUGUI component inside the panel for displaying text.")]
    public TextMeshProUGUI displayText; 

    // --- State Management ---
    private bool isMessageActive = false;
    private const KeyCode DismissKey = KeyCode.Space;
    private float previousTimeScale = 1f;

    void Start()
    {
        // Start a coroutine to ensure all other scene initializations have run first.
        StartCoroutine(LoadMessageAfterDelay());
    }

    void Update()
    {
        // Only check for dismissal if this specific message is active
        if (isMessageActive && Input.GetKeyDown(DismissKey))
        {
            DismissSceneMessage();
        }
    }

    // Waits one frame for stability, then displays the message and pauses the game.
    private IEnumerator LoadMessageAfterDelay()
    {
        // Wait one frame for stability (ensures UIManager and other components are initialized)
        yield return null; 

        if (displayPanel == null || displayText == null)
        {
            Debug.LogError("[MessageLoader] UI References (Panel or Text) are missing! Cannot display scene message.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(sceneMessage))
        {
            Debug.LogWarning("[MessageLoader] Message is empty for this scene. Skipping message display.");
            yield break;
        }

        // --- Display Logic ---
        displayText.text = sceneMessage;
        displayPanel.SetActive(true);
        isMessageActive = true;

        // Pause the game
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        
        Debug.Log("[MessageLoader] Initial scene message displayed and game paused. Press Space to resume.");
    }
    
    // Hides the message panel and resumes game time.
    private void DismissSceneMessage()
    {
        // 1. Hide the UI elements
        displayPanel.SetActive(false);
        isMessageActive = false;

        // 2. Resume the game
        Time.timeScale = previousTimeScale;
        
        Debug.Log("[MessageLoader] Message dismissed. Game Resumed.");
    }
}