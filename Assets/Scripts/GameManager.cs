using UnityEngine;
using UnityEngine.SceneManagement; 

public class GameManager : MonoBehaviour
{
    // Start Game Method (handles the hard reset)
    public void StartGame()
    {
        // CRITICAL RESET LOGIC:
        // 1. Wipe ALL persistence data stored in PlayerPrefs.
        // This clears all persistent player stats (e.g., bonus HP) 
        // AND all specific "PowerUp_Collected_..." flags.
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        
        Debug.Log("GameManager: Full game progress reset executed. All stats and collected items cleared.");

        // 2. Load the first game level (assuming it is index 1)
        SceneManager.LoadScene(1); 
    }

    // New method for exiting the game
    public void ExitGame()
    {
        // 1. Quits the application when running a built game (PC, Mac, etc.)
        Application.Quit();

        // 2. A special command to stop the game playing 
        // in the Unity Editor (only runs if the game is NOT built)
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}