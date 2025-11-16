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
        groundPlane = new Plane(Vector3.up, transform.position);

        // Get PlayerState component
        playerState = GetComponent<PlayerState>();
        if (playerState == null)
        {
            Debug.LogError("PlayerControls: PlayerState component not found!");
        }

        if (modelTransform != null)
            animator = modelTransform.GetComponent<Animator>();
        else
            Debug.LogError("PlayerControls: modelTransform is not assigned. Cannot find Animator.");
    }

    void Update()
    {
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
            rb.linearVelocity = moveDirection * currentSpeed;
        }
        else
        {
            rb.linearVelocity = Vector3.zero;
        }

        // NOTE: targetRotation is set in Update() and applied in LateUpdate()
    }

    void LateUpdate()
    {
        // --- Smooth model rotation ---
        if (modelTransform != null)
        {
            float currentRotationSpeed = isGauntletMode ? aimRotationSpeed : rotationSmoothSpeed;
            
            modelTransform.rotation = Quaternion.Slerp(
                modelTransform.rotation,
                targetRotation,
                currentRotationSpeed * Time.deltaTime
            );
        }
    }
}