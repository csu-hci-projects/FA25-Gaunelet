using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerControls : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Model & Animator")]
    public Transform modelTransform; // Child with Skinned Mesh + Animator
    private Animator animator;
    
    [Header("Gauntlet Visual Offset")]
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
    // Now references the PlayerState component to check death status
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
            // Store the initial local position of the model
            originalModelLocalPosition = modelTransform.localPosition; 
        }
        else
        {
            Debug.LogError("PlayerControls: modelTransform is not assigned. Cannot find Animator or store position.");
        }
    }

    void Update()
    {
        // --- DEATH CHECK: Disable all input and rotation when dying ---
        if (playerState != null && playerState.IsDying())
        {
            // Optional: Force the animator to idle on death
            if (animator != null)
            {
                animator.SetBool(IS_WALKING, false);
                animator.SetBool(IS_BLOCKING, false);
            }
            return;
        }

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
        // --- DEATH CHECK: Stop all movement when dying ---
        if (playerState != null && playerState.IsDying())
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

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
        // --- DEATH CHECK: Stop visual effects/rotation when dying ---
        if (playerState != null && playerState.IsDying())
        {
            return;
        }

        if (modelTransform == null) return;

        // 1. ROTATION LOGIC
        float currentRotationSpeed = isGauntletMode ? aimRotationSpeed : rotationSmoothSpeed;
        
        // --- GAUNTLET MODE (Aiming/Blocking) ---
        if (isGauntletMode)
        {
            // Rotate the root object smoothly towards the mouse target
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                currentRotationSpeed * Time.deltaTime
            );
            
            // Align model (child) local rotation to identity
            modelTransform.localRotation = Quaternion.Slerp(
                modelTransform.localRotation,
                Quaternion.identity,
                currentRotationSpeed * Time.deltaTime
            );
        }
        // --- NORMAL MODE (Walking/Idle) ---
        else
        {
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