using UnityEngine;

// Controls the periodic up-and-down (bobbing) movement of a game object
// along the Y-axis, simulating an item moving in and out of the ground plane.
public class ItemMove : MonoBehaviour
{
    [Header("Bobbing Configuration")]
    [Tooltip("The total distance the item travels up and down (peak-to-peak).")]
    [SerializeField] private float amplitude = 0.5f;

    [Tooltip("The speed of the bobbing motion. Higher value means faster movement.")]
    [SerializeField] private float frequency = 2.0f;

    [Header("Rotation Configuration")]
    [Tooltip("The speed at which the item rotates around its Y-axis.")]
    [SerializeField] private float rotationSpeed = 50f;

    // The initial Y position is stored here in Start() to act as the center point 
    // for the sine wave.
    private float initialY;

    void Start()
    {
        // Capture the starting Y position of the object. All future movement will
        // be an offset from this point.
        initialY = transform.position.y;
        Debug.Log($"ItemMove initialized. Center Y position: {initialY}");
    }

    void Update()
    {
        // 1. Calculate the periodic vertical movement (Bobbing)
        // Mathf.Sin(time) oscillates smoothly between -1 and 1.
        // Multiplying by 'amplitude' scales the movement range.
        float bobOffset = Mathf.Sin(Time.time * frequency) * amplitude;

        // Apply the offset to the initial Y position to get the new, current Y position.
        Vector3 newPosition = transform.position;
        newPosition.y = initialY + bobOffset;
        transform.position = newPosition;

        // 2. Continuous Rotation (Optional but common for collectibles)
        // Rotates the object around its local Y-axis based on time and speed.
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}