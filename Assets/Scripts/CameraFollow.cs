using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.15f; // smaller = snappier, larger = smoother

    private Vector3 offset;
    private Vector3 velocity = Vector3.zero;
    private bool useFixedUpdate = false;

    void Start()
    {
        offset = transform.position - target.position;

        // Check if player has Rigidbody to decide update mode
        if (target.TryGetComponent<Rigidbody>(out _))
            useFixedUpdate = true;
    }

    void LateUpdate()
    {
        if (!useFixedUpdate) FollowTarget();
    }

    void FixedUpdate()
    {
        if (useFixedUpdate) FollowTarget();
    }

    void FollowTarget()
    {
        Vector3 targetPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }
}