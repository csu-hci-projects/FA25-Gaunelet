using UnityEngine;

public class HPGain : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float healthAmount = 10f; // Default HP to restore
    
    [Header("Respawn Settings")]
    [SerializeField] private float respawnTime = 60f; // Time in seconds before respawning

    private Collider pickupCollider;
    private MeshRenderer meshRenderer;

    private const string PLAYER_TAG = "Player";

    void Start()
    {
        pickupCollider = GetComponent<Collider>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (pickupCollider == null || meshRenderer == null)
        {
            Debug.LogError("HPGain script requires both a Collider and a MeshRenderer!");
            // Optionally, try to find in children if the pickup item is complex
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PLAYER_TAG))
        {
            PlayerState playerState = other.GetComponent<PlayerState>();
            
            if (playerState != null)
            {
                // 1. Increment HP
                playerState.Heal(healthAmount); // Assuming you have a Heal method in PlayerState
                
                // 2. Hide and Disable Pickup
                DisablePickup();
                
                // 3. Start Respawn Timer
                Invoke(nameof(RespawnPickup), respawnTime);
            }
        }
    }

    private void DisablePickup()
    {
        if (meshRenderer != null) meshRenderer.enabled = false;
        if (pickupCollider != null) pickupCollider.enabled = false;
    }

    private void RespawnPickup()
    {
        if (meshRenderer != null) meshRenderer.enabled = true;
        if (pickupCollider != null) pickupCollider.enabled = true;
        Debug.Log($"{gameObject.name} has respawned.");
    }
}