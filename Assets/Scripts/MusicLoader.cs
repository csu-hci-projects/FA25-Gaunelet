using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Listens for scene changes and loads the corresponding music track
/// based on the scene's build index.
/// 
/// SCENE INDEX MAPPING (You must verify these indices in your Build Settings):
/// 1: Woodland
/// 2: Dungeon
/// 3: Labyrinth
/// </summary>
public class MusicLoader : MonoBehaviour
{
    [Header("Music Tracks (Assign by Scene Index)")]
    [Tooltip("Index 0 is often the Title/Menu screen. Assign clips starting from the index 1 (Woodland).")]
    public AudioClip[] sceneMusicTracks;

    // --- You will need to manually ensure the music manager exists once ---
    private MusicManager musicManagerInstance;

    void Start()
    {
        // 1. Find or CREATE the MusicManager instance
        musicManagerInstance = MusicManager.Instance;
        if (musicManagerInstance == null)
        {
            // If the singleton doesn't exist (e.g., loaded directly into a level)
            // We dynamically create a new GameObject and add the MusicManager component to it.
            GameObject managerObject = new GameObject("_MusicManager_AutoCreated");
            musicManagerInstance = managerObject.AddComponent<MusicManager>();
            Debug.Log("[MusicLoader] MusicManager was automatically created for this scene.");
        }

        // 2. Load the music for the scene we just started in.
        LoadMusicForCurrentScene();
        
        // 3. Register for future scene loads.
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Called when the scene is unloaded (component or GameObject destroyed)
    void OnDestroy()
    {
        // Unregister the event handler to prevent memory leaks/null references
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Event handler called every time a new scene finishes loading.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only load music if the scene was loaded in Single mode (standard level change)
        if (mode == LoadSceneMode.Single)
        {
            LoadMusicForSceneIndex(scene.buildIndex);
        }
    }

    /// <summary>
    /// Checks the current scene's index and attempts to play the corresponding track.
    /// </summary>
    private void LoadMusicForCurrentScene()
    {
        int index = SceneManager.GetActiveScene().buildIndex;
        LoadMusicForSceneIndex(index);
    }
    
    /// <summary>
    /// Plays the music clip corresponding to the given scene index.
    /// </summary>
    private void LoadMusicForSceneIndex(int sceneIndex)
    {
        // musicManagerInstance should now always be available due to the fix in Start()
        if (musicManagerInstance == null) 
        {
            // This should only happen if the component was somehow destroyed later
            Debug.LogError("[MusicLoader] Cannot load music: MusicManager is null.");
            return;
        }

        if (sceneIndex < 0 || sceneIndex >= sceneMusicTracks.Length)
        {
            Debug.Log($"[MusicLoader] No music clip assigned for scene index {sceneIndex}. Stopping music.");
            musicManagerInstance.StopMusic();
            return;
        }

        AudioClip trackToPlay = sceneMusicTracks[sceneIndex];
        
        // Only attempt to play if the clip is valid and not already playing
        if (trackToPlay != null && !musicManagerInstance.IsClipPlaying(trackToPlay))
        {
            musicManagerInstance.PlayMusic(trackToPlay);
        }
    }
}