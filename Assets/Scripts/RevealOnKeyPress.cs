using UnityEngine;
using System.Collections.Generic;

// This script enables the MeshRenderer components of a list of target GameObjects
// when the player is inside the trigger volume and presses the specified key.
[RequireComponent(typeof(BoxCollider))] // Requires a Collider for the trigger area
[RequireComponent(typeof(Rigidbody))]    // Requires a Rigidbody to detect the player
public class RevealOnKeyPress : MonoBehaviour
{
    [Header("Reveal Configuration")]
    [Tooltip("The key the player must press to activate the reveal.")]
    public KeyCode activationKey = KeyCode.Space;
    
    [Header("Targets to Reveal")]
    [Tooltip("Drag the objects whose MeshRenderer should be enabled (made visible).")]
    public List<GameObject> objectsToReveal;

    private bool playerIsInside = false;
    private bool hasBeenActivated = false;

    void Start()
    {
        // 1. Ensure the Rigidbody is kinematic (needed for proper trigger detection)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // 2. Hide all the target objects at the start of the game
        SetTargetsVisibility(false);
    }

    // Called when another collider enters the trigger.
    private void OnTriggerEnter(Collider other)
    {
        // Check if the entering object is the player (based on tag)
        if (other.CompareTag("Player"))
        {
            playerIsInside = true;
            Debug.Log($"Player entered {gameObject.name}'s reveal zone. Press {activationKey} to activate.");
        }
    }

    // Called every frame while another collider is inside the trigger.
    private void OnTriggerStay(Collider other)
    {
        if (hasBeenActivated) return;

        // Check if the player is inside and presses the activation key
        if (playerIsInside && other.CompareTag("Player") && Input.GetKeyDown(activationKey))
        {
            ActivateReveal();
        }
    }

    // Called when another collider exits the trigger.
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsInside = false;
            Debug.Log($"Player exited {gameObject.name}'s reveal zone.");
        }
    }

    // Activates the renderers on all target objects.
    private void ActivateReveal()
    {
        if (hasBeenActivated) return;

        Debug.Log("Reveal triggered! Making objects visible.");
        SetTargetsVisibility(true);
        hasBeenActivated = true;

        // Optional: Destroy this trigger object after activation if it's a one-time event
        // Destroy(gameObject);
    }

    // Sets the enabled state of all Renderer components (MeshRenderer, SkinnedMeshRenderer, etc.) 
    // on the target object and all its children.
    private void SetTargetsVisibility(bool isVisible)
    {
        foreach (GameObject target in objectsToReveal)
        {
            if (target != null)
            {
                // Get ALL Renderer components (MeshRenderer, SkinnedMeshRenderer, etc.) 
                // on the target and all its children. This is crucial for complex models.
                Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
                
                if (renderers.Length > 0)
                {
                    foreach (Renderer renderer in renderers)
                    {
                        renderer.enabled = isVisible;
                    }
                }
                else
                {
                    // This warning is now more accurate, checking the whole hierarchy.
                    Debug.LogWarning($"Target object '{target.name}' or its children are missing any Renderer components. Cannot hide/show object visually.");
                }
            }
        }
    }
}