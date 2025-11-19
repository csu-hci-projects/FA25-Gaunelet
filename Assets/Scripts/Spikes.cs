using UnityEngine;

[RequireComponent(typeof(BoxCollider))] // Ensure a Collider exists for the trigger
public class Spikes : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damagePerSecond = 30f; // Damage dealt per second while the player is in contact.

    [Header("Movement Settings")]
    public bool enableMovement = false;
    public float moveDistance = 1.0f;     // How far the spikes move up/down from their start position.
    public float moveSpeed = 1.0f;        // Speed of the spikes' oscillation.
    public float activationDelay = 0.5f;  // Time the spikes stay fully retracted (down) before moving.

    private Vector3 startPosition;
    private float timer = 0f;
    private bool isPlayerInContact = false;

    void Start()
    {
        // 1. Store the initial position
        startPosition = transform.position;

        // 2. Ensure the BoxCollider is set to be a trigger
        BoxCollider bc = GetComponent<BoxCollider>();
        if (bc != null && !bc.isTrigger)
        {
            bc.isTrigger = true;
            Debug.LogWarning("[Spikes] Collider was not set to Trigger. It has been set automatically.");
        }
    }

    void Update()
    {
        if (enableMovement)
        {
            HandleMovement();
        }

        // Handle continuous damage if the player is currently inside the trigger
        if (isPlayerInContact)
        {
            // The damage logic is intentionally put in Update, even though OnTriggerStay is available,
            // to ensure damage is applied using Time.deltaTime for frame-rate independence.
            ApplyDamage();
        }
    }
    
    // Handles the vertical oscillation of the spikes
    void HandleMovement()
    {
        // Add delay before the spikes start moving up/down
        timer += Time.deltaTime * moveSpeed;
        
        // Calculate the movement based on a sine wave (smooth oscillation)
        // Add activationDelay to the wave function to introduce a pause at the lowest point
        float yOffset = (Mathf.Sin(timer) + 1.0f) * 0.5f * moveDistance;
        
        // Reset timer and pause at the lowest point (fully retracted)
        if (yOffset <= 0.01f) // Check if spikes are near the start position
        {
            timer = -activationDelay * moveSpeed;
        }

        // Apply the new position
        transform.position = startPosition + Vector3.up * yOffset;
    }


    // --- Collision/Trigger Methods ---

    // Called when another collider ENTERS this trigger
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInContact = true;
        }
    }

    // Called when another collider STAYS within this trigger
    // Note: We flag the damage in OnTriggerEnter/Exit and run ApplyDamage in Update for reliability.
    /*
    private void OnTriggerStay(Collider other)
    {
        // This is an alternative place to call ApplyDamage if we didn't use the flag,
        // but using the flag and Update() is generally cleaner for continuous damage.
    }
    */

    // Called when another collider EXITS this trigger
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInContact = false;
        }
    }

    // Handles the actual damage application
    void ApplyDamage()
    {
        // Check if the other object has the PlayerState component
        PlayerState playerState = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerState>();
        
        if (playerState != null)
        {
            // Apply damage based on time since last frame
            float damageToApply = damagePerSecond * Time.deltaTime;
            playerState.TakeDamage(damageToApply);
            Debug.Log($"[Spikes] Player took {damageToApply:F2} damage. Total HP/s: {damagePerSecond}");
        }
    }
}