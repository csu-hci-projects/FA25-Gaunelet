using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerControls : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Model & Animator")]
    public Transform modelTransform; // Child with Skinned Mesh + Animator
    private Animator animator;

    private Rigidbody rb;
    private Vector3 moveInput;
    private Quaternion targetRotation;

    [Header("Rotation Settings")]
    public float rotationSmoothSpeed = 10f; // higher = faster rotation smoothing
    
    // === NEW INTERACTION VARIABLES ===
    private const string ACTION_KEY = "space";
    // Now stores a reference to the SignInfo component, not just the GameObject
    private SignInfo currentSignInfo; 
    // =================================

    // Use a public const string for the Animator parameter to prevent typos
    private const string IS_WALKING = "IsWalking"; 

    void Awake()
    {
        Application.targetFrameRate = 400;
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Get the Animator component from the designated child Transform
        if (modelTransform != null)
        {
            animator = modelTransform.GetComponent<Animator>();
        }
        else
        {
            Debug.LogError("PlayerControls: modelTransform is not assigned. Cannot find Animator.");
        }
    }

    void Update()
    {
        // --- Input ---
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(horizontal, 0f, vertical);

        // --- Animator Logic ---
        bool isWalking = moveInput.sqrMagnitude > 0.01f; 
        
        if (animator != null)
        {
            animator.SetBool(IS_WALKING, isWalking);
        }

        // === NEW INTERACTION LOGIC ===
        // Check if player is in range of a sign AND presses the spacebar
        if (currentSignInfo != null && Input.GetKeyDown(ACTION_KEY))
        {
            // Call the public method on the Sign to toggle its UI display
            currentSignInfo.ToggleTextDisplay();
        }
        // =============================
    }

    void FixedUpdate()
    {
        // --- Move Rigidbody ---
        Vector3 moveDirection = moveInput.normalized;
        rb.linearVelocity = moveDirection * moveSpeed;

        // --- Set target rotation ---
        if (moveDirection.sqrMagnitude > 0.01f)
            targetRotation = Quaternion.LookRotation(moveDirection);
        else if (rb.linearVelocity.sqrMagnitude < 0.01f)
            rb.linearVelocity = Vector3.zero;
    }

    void LateUpdate()
    {
        // --- Smooth model rotation ---
        if (modelTransform != null)
        {
            modelTransform.rotation = Quaternion.Slerp(
                modelTransform.rotation,
                targetRotation,
                rotationSmoothSpeed * Time.deltaTime
            );
        }
    }

    // === TRIGGER METHODS ===

    // Called when the player enters a trigger collider
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entered is the Sign (by Tag)
        if (other.gameObject.CompareTag("Sign")) 
        {
            // Store the SignInfo component reference
            currentSignInfo = other.gameObject.GetComponent<SignInfo>();
        }
    }

    // Called when the player leaves a trigger collider
    private void OnTriggerExit(Collider other)
    {
        // Check if the object exited is the currently tracked sign
        if (other.gameObject.CompareTag("Sign") && currentSignInfo != null)
        {
            // Hide the text if it's visible, then clear the reference
            currentSignInfo.HideText(); // Ensures the panel is hidden when walking away
            currentSignInfo = null;
        }
    }
}
