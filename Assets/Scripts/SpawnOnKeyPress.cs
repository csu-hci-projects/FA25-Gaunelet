using UnityEngine;

// Spawns a single GameObject prefab (the sign) at a specified Transform location 
// when the player presses the activation key inside the trigger volume.
[RequireComponent(typeof(BoxCollider))] // Requires a Collider for the trigger area
public class SpawnOnKeyPress : MonoBehaviour
{
    [Header("Interaction Config")]
    [Tooltip("The key the player must press to activate the spawn.")]
    public KeyCode activationKey = KeyCode.Space;

    [Header("Spawn Configuration")]
    [Tooltip("The sign prefab object that will be spawned.")]
    public GameObject itemToSpawnPrefab;
    
    [Tooltip("The Transform that marks the precise location where the item should spawn. Drag your empty 'Spawner' object here.")]
    public Transform spawnLocationMarker;

    // Message Content removed: The spawned sign's 'SignReader.cs' now holds the message.

    private bool playerIsInside = false;
    private bool hasBeenActivated = false;

    void Start()
    {
        // Diagnostics to help solve the "not spawning" issue
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null || !boxCollider.isTrigger)
        {
            Debug.LogError($"[Setup Error] {gameObject.name} must have a BoxCollider set to 'Is Trigger' for interaction to work!");
        }
        
        // This is a common requirement if the player object lacks a Rigidbody.
        if (GetComponent<Rigidbody>() == null)
        {
             Debug.LogWarning($"[Setup Warning] {gameObject.name} is missing a Rigidbody. If the player is missing a Rigidbody, the trigger events might not fire.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasBeenActivated) return;
        if (other.CompareTag("Player"))
        {
            playerIsInside = true;
            Debug.Log($"Player entered {gameObject.name}'s spawn zone. Press {activationKey} to spawn.");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (hasBeenActivated) return;

        if (playerIsInside && other.CompareTag("Player") && Input.GetKeyDown(activationKey))
        {
            ActivateSpawn();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsInside = false;
            Debug.Log($"Player exited {gameObject.name}'s spawn zone.");
        }
    }

    // Instantiates the item. The spawned sign is now responsible for handling its own message display.
    private void ActivateSpawn()
    {
        if (hasBeenActivated) return;
        
        // 1. Check for required references
        if (itemToSpawnPrefab == null || spawnLocationMarker == null)
        {
            Debug.LogError($"Spawn components are missing on {gameObject.name}. Cannot spawn item.");
            return;
        }
        
        // 2. Perform the spawn
        Vector3 finalPosition = spawnLocationMarker.position;
        Quaternion finalRotation = itemToSpawnPrefab.transform.rotation;

        GameObject spawnedSign = Instantiate(itemToSpawnPrefab, finalPosition, finalRotation);
        
        Debug.Log($"Spawn triggered! Instantiated {spawnedSign.name}.");

        hasBeenActivated = true;
    }
}