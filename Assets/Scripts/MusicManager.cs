using UnityEngine;
using UnityEngine.SceneManagement;

// Singleton class to manage the background music AudioSource.
// It ensures only one instance of the music player exists in the game, 
// and provides a simple interface to change the currently playing clip.
public class MusicManager : MonoBehaviour
{
    // Static reference to the singleton instance
    public static MusicManager Instance { get; private set; }

    // AudioSource component to play the music
    private AudioSource audioSource;

    void Awake()
    {
        // 1. Singleton Setup: Ensure only one MusicManager exists.
        if (Instance == null)
        {
            Instance = this;
            // Crucial: Keeps this object alive when loading new scenes.
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            // If another MusicManager exists, destroy this one.
            Destroy(gameObject);
            return;
        }

        // 2. Component Setup: Get or add the AudioSource component.
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 3. AudioSource Configuration (Set up for background music)
        audioSource.loop = true;          // Music should loop by default
        audioSource.playOnAwake = false;  // We control when music starts
        audioSource.volume = 0.5f;        // Default volume
    }

    // Checks if the given audio clip is already playing.
    public bool IsClipPlaying(AudioClip clip)
    {
        if (audioSource.isPlaying && audioSource.clip == clip)
        {
            return true;
        }
        return false;
    }

    // Stops the current music and starts playing a new clip.
    public void PlayMusic(AudioClip newClip)
    {
        if (newClip == null)
        {
            Debug.LogWarning("[MusicManager] Attempted to play a null audio clip. Stopping music.");
            audioSource.Stop();
            audioSource.clip = null;
            return;
        }

        // If the same clip is already playing, do nothing.
        if (audioSource.clip == newClip && audioSource.isPlaying)
        {
            return;
        }

        // Stop current track, assign new one, and play.
        audioSource.Stop();
        audioSource.clip = newClip;
        audioSource.Play();
        Debug.Log($"[MusicManager] Now playing: {newClip.name}");
    }

    // Stops the music.
    public void StopMusic()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("[MusicManager] Music stopped.");
        }
    }
}