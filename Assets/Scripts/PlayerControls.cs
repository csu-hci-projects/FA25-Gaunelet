using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerControls : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Model & Animator")]
    public Transform modelTransform; // Child with Skinned Mesh + Animator
    private Animator animator;
    
    // NEW: Model Offset during Gauntlet Mode
    [Header("Gauntlet Visual Offset")]
    // Adjust this value in the Inspector to find the perfect height
    public float gauntletYOffset = 0.08f; 
    private Vector3 originalModelLocalPosition; // Store initial position

    private Rigidbody rb;
    private Vector3 moveInput;
    private Quaternion targetRotation;

    [Header("Rotation Settings")]
    public float rotationSmoothSpeed = 10f; // higher = faster rotation smoothing
    
    // NEW: Plane used for raycasting mouse position
    private Plane groundPlane; 
    
    [Header("Gauntlet / Block Settings")]
    public float blockMoveSpeedMultiplier = 0.3f; // Slow down movement while blocking
    public float aimRotationSpeed = 20f; 
    private bool isGauntletMode = false;

    // References
    private PlayerState playerState;

    // Animator parameter names
    private const string IS_WALKING = "IsWalking";
    private const string IS_BLOCKING = "IsBlocking";

    void Awake()
    {
        Application.targetFrameRate = 400;

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // NEW: Define the ground plane at the player's Y level
        // We set the initial position here, but update it dynamically in Update()
        groundPlane = new Plane(Vector3.up, transform.position);

        // Get PlayerState component
        playerState = GetComponent<PlayerState>();
        if (playerState == null)
        {
            Debug.LogError("PlayerControls: PlayerState component not found!");
        }

        if (modelTransform != null)
        {
            animator = modelTransform.GetComponent<Animator>();
            // NEW: Store the initial local position of the model
            originalModelLocalPosition = modelTransform.localPosition; 
        }
        else
        {
            Debug.LogError("PlayerControls: modelTransform is not assigned. Cannot find Animator or store position.");
        }
    }

    void Update()
    {
        // Update the ground plane's position dynamically to the current player height
        groundPlane.SetNormalAndPosition(Vector3.up, transform.position);
        
        // --- Gauntlet/Block Input ---
        isGauntletMode = Input.GetMouseButton(1); // Right mouse button held down
        
        // Tell PlayerState about blocking state
        if (playerState != null)
        {
            playerState.SetBlocking(isGauntletMode);
        }
        
        if (animator != null)
        {
            animator.SetBool(IS_BLOCKING, isGauntletMode);
        }

        // --- Movement Input ---
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(horizontal, 0f, vertical); // Uses world axes

        // --- Animator walking ---
        bool isWalking = moveInput.sqrMagnitude > 0.01f;
        if (animator != null)
        {
            animator.SetBool(IS_WALKING, isWalking);
        }

        // NEW: Calculate target rotation based on aiming state in Update
        CalculateTargetRotation();
    }

    private void CalculateTargetRotation()
    {
        if (isGauntletMode)
        {
            // --- AIMING MODE: Face Mouse Cursor on Ground Plane ---
            
            // Cast a ray from the camera through the mouse position
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            
            float distance;
            if (groundPlane.Raycast(ray, out distance))
            {
                // Find the intersection point on the ground plane
                Vector3 point = ray.GetPoint(distance);
                
                // Calculate the direction vector from the player to the point
                Vector3 lookDirection = point - transform.position;
                
                // Crucial: Only rotate on the Y-axis (XZ plane)
                lookDirection.y = 0; 
                
                if (lookDirection.sqrMagnitude > 0.01f)
                {
                    targetRotation = Quaternion.LookRotation(lookDirection);
                }
            }
        }
        else if (moveInput.sqrMagnitude > 0.01f)
        {
            // --- NORMAL MODE: Rotate in the direction of movement ---
            Vector3 moveDirection = moveInput.normalized;
            targetRotation = Quaternion.LookRotation(moveDirection);
        }
    }

    void FixedUpdate()
    {
        // --- Move Rigidbody (Always relative to World Axes, as requested) ---
        
        Vector3 moveDirection = moveInput.normalized;
        float currentSpeed = isGauntletMode ? moveSpeed * blockMoveSpeedMultiplier : moveSpeed;

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            // Set the Rigidbody's velocity directly for immediate, non-physics movement
            rb.linearVelocity = moveDirection * currentSpeed;
        }
        else
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    void LateUpdate()
    {
        if (modelTransform == null) return;

        // 1. ROTATION LOGIC
        float currentRotationSpeed = isGauntletMode ? aimRotationSpeed : rotationSmoothSpeed;
        
        // --- GAUNTLET MODE (Aiming/Blocking) ---
        if (isGauntletMode)
        {
            // When aiming, the ROOT object MUST be rotated so abilities fired from the root 
            // (e.g., GauntletAbilities.cs) use the correct forward vector.
            
            // Rotate the root object smoothly towards the mouse target
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                currentRotationSpeed * Time.deltaTime
            );
            
            // The model (child) will inherit this rotation. We align the model's local rotation 
            // to identity to ensure it follows the parent exactly and doesn't introduce double-rotation.
            modelTransform.localRotation = Quaternion.Slerp(
                modelTransform.localRotation,
                Quaternion.identity,
                currentRotationSpeed * Time.deltaTime
            );
        }
        // --- NORMAL MODE (Walking/Idle) ---
        else
        {
            // When not aiming, the root object remains fixed (rb.freezeRotation is true and we don't set transform.rotation).
            // Only the child model rotates for visual smoothness.
            modelTransform.rotation = Quaternion.Slerp(
                modelTransform.rotation,
                targetRotation,
                currentRotationSpeed * Time.deltaTime
            );
        }

        // 2. POSITION OFFSET LOGIC (Visual Model Lift)
        Vector3 targetLocalPosition;
        const float smoothFactor = 8f; 

        if (isGauntletMode)
        {
            // Target position is the original position plus the vertical offset
            targetLocalPosition = originalModelLocalPosition + new Vector3(0, gauntletYOffset, 0);
        }
        else
        {
            // Target position is the original local position (no offset)
            targetLocalPosition = originalModelLocalPosition;
        }

        // Smoothly move the model to the target local position
        modelTransform.localPosition = Vector3.Lerp(
            modelTransform.localPosition, 
            targetLocalPosition, 
            smoothFactor * Time.deltaTime
        );
    }
}