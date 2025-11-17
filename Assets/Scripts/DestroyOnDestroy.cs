using UnityEngine;

public class DestroyOnDestroy : MonoBehaviour
{
    [Header("Target to Destroy")]
    [Tooltip("Drag the barrier or object that should be destroyed when this enemy dies here.")]
    public GameObject targetToDestroy;

    // OnDestroy is a special Unity function called when the component's GameObject is destroyed.
    void OnDestroy()
    {
        // Safety check to ensure the target object still exists
        if (targetToDestroy != null)
        {
            // Destroy the linked object (the barrier)
            Destroy(targetToDestroy);
            Debug.Log($"[DestroyOnDestroy] Host object ({gameObject.name}) was destroyed. Barrier ({targetToDestroy.name}) destroyed.");
        }
    }
}