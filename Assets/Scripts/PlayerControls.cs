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

    [Header("Block Settings")]
    public float blockMoveSpeedMultiplier = 0.3f; // Slow down movement while blocking
    private bool isBlocking = false;

    // Animator parameter names
    private const string IS_WALKING = "IsWalking";
    private const string IS_BLOCKING = "IsBlocking";

    void Awake()
    {
        Application.targetFrameRate = 400;

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (modelTransform != null)
            animator = modelTransform.GetComponent<Animator>();
        else
            Debug.LogError("PlayerControls: modelTransform is not assigned. Cannot find Animator.");
    }

    void Update()
    {
        // --- Block Input ---
        isBlocking = Input.GetMouseButton(1); // Right mouse button held down
        
        if (animator != null)
        {
            animator.SetBool(IS_BLOCKING, isBlocking);
        }

        // --- Movement Input ---
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(horizontal, 0f, vertical);

        // --- Animator walking ---
        bool isWalking = moveInput.sqrMagnitude > 0.01f;
        if (animator != null)
        {
            animator.SetBool(IS_WALKING, isWalking);
        }
    }

    void FixedUpdate()
    {
        // --- Move Rigidbody ---
        Vector3 moveDirection = moveInput.normalized;
        
        // Apply speed multiplier if blocking
        float currentSpeed = isBlocking ? moveSpeed * blockMoveSpeedMultiplier : moveSpeed;
        rb.linearVelocity = moveDirection * currentSpeed;

        // --- Set target rotation ---
        if (moveDirection.sqrMagnitude > 0.01f)
            targetRotation = Quaternion.LookRotation(moveDirection);
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
}