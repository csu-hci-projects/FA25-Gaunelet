using UnityEngine;

public class MPGain : MonoBehaviour
{
    [Header("Magic Settings")]
    [SerializeField] private float magicAmount = 5f; // Default MP to restore
    
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
            Debug.LogError("MPGain script requires both a Collider and a MeshRenderer!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PLAYER_TAG))
        {
            PlayerState playerState = other.GetComponent<PlayerState>();
            
            if (playerState != null)
            {
                // 1. Increment MP
                playerState.RestoreMagic(magicAmount); // Assuming you have a RestoreMagic method in PlayerState
                
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