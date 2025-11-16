using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target & Speed")]
    public Transform target;
    // TWEAK this value: A slightly higher value will make it feel smoother.
    public float smoothTime = 0.2f; 

    private Vector3 offset;
    private Vector3 velocity = Vector3.zero;
    
    // Removed the private bool useFixedUpdate = false;

    void Start()
    {
        // Calculate the initial fixed offset
        offset = transform.position - target.position;
        
        // We will now rely exclusively on LateUpdate for smooth visual following
    }

    // Use LateUpdate to ensure the camera position is updated 
    // *after* the player's movement and rotation is completed.
    void LateUpdate()
    {
        FollowTarget();
    }
    
    // Removed the empty FixedUpdate()

    void FollowTarget()
    {
        // Calculate the desired position based on the fixed offset
        Vector3 targetPosition = target.position + offset;
        
        // Smoothly move the camera to the target position
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            targetPosition, 
            ref velocity, 
            smoothTime
        );
    }
}