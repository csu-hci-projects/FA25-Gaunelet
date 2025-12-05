using UnityEngine;
using UnityEngine.SceneManagement; // Required for loading scenes

/// <summary>
/// Attaching this script to a trigger volume will load the next specified scene
/// when an object with the required tag (e.g., "Player") enters the volume.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ExitLevel : MonoBehaviour
{
    [Header("Level Transition")]
    [Tooltip("The name of the scene to load next (must be added to Build Settings).")]
    [SerializeField] private string nextSceneName;

    [Tooltip("The tag of the object required to activate the level exit (default: Player).")]
    [SerializeField] private string requiredTag = "Player";

    void Start()
    {
        // Ensure the collider component is set as a trigger for this script to work.
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"[ExitLevel] Collider on {gameObject.name} was not set to 'Is Trigger'. It has been set automatically.");
        }
        
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("[ExitLevel] 'Next Scene Name' is not set! This trigger will not work.");
        }
    }

    /// <summary>
    /// Called when another object enters the trigger volume.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Check if the entering object has the required tag
        if (other.CompareTag(requiredTag))
        {
            LoadNextLevel();
        }
    }

    /// <summary>
    /// Attempts to load the scene specified in the Inspector.
    /// </summary>
    private void LoadNextLevel()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("[ExitLevel] Cannot load level: Next scene name is empty.");
            return;
        }

        Debug.Log($"[ExitLevel] Player entered, loading scene: {nextSceneName}...");
        
        try
        {
            SceneManager.LoadScene(nextSceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ExitLevel] Failed to load scene '{nextSceneName}'. Ensure it is spelled correctly and added to Build Settings.\nError: {e.Message}");
        }
    }
}