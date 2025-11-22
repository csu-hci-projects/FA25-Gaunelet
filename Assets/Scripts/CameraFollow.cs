using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target & Speed")]
    public Transform target;
    public float smoothTime = 0.2f; 

    private Vector3 offset;
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        // Calculate the initial fixed offset
        offset = transform.position - target.position;
    }

    // Use LateUpdate to ensure the camera position is updated 
    // *after* the player's movement and rotation is completed.
    void LateUpdate()
    {
        FollowTarget();
    }

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