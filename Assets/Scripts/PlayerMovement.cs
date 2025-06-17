using UnityEngine;
using TMPro;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 8f;
    public float groundAccel = 80f;
    public float groundDecel = 80f;
    public float airAccel = 40f;
    public float airDecel = 40f;

    [Header("Jump")]
    public float jumpVelocity = 12f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;
    public float lowJumpMultiplier = 3f;
    public float fallMultiplier = 5f;

    [Header("Ground Check")]
    public LayerMask groundLayer;
    [Tooltip("Size of the box used to check for ground.")]
    public Vector2 groundCheckSize = new Vector2(0.8f, 0.2f);
    [Tooltip("Distance from the collider's bottom to start the ground check.")]
    public float groundCheckYOffset = -0.5f; // Adjust based on your capsule collider
    [Tooltip("How far down the ground check box will look for ground.")]
    public float groundCheckDistance = 0.1f;


    [Header("Double Jump")]
    public TextMeshProUGUI doubleJumpText;
    public string doubleJumpZoneTag = "DoubleJumpZone";
    public bool hasDoubleJump = false;

    private Rigidbody2D rb;
    private Animator animator;
    private DashAttack dashAttack;
    private CapsuleCollider2D capsuleCollider;

    public bool IsGrounded { get; private set; }
    private float coyoteTimer;
    private float jumpBufferTimer;

    private int jumpCount = 0;
    private int maxJumps = 1;
    private float horizontalInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        dashAttack = GetComponent<DashAttack>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();

        if (doubleJumpText != null)
            doubleJumpText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused)
            return;

        // Handle Inputs and Timers in Update
        HandleInput();
        HandleTimers();

        // Handle animation and facing direction
        UpdateAnimator();
        HandleFacingDirection();
    }

    void FixedUpdate()
    {
        // Physics-related logic goes in FixedUpdate
        CheckGrounded();

        if (dashAttack != null && dashAttack.IsDashing)
        {
            return;
        }

        HandleJump();
        HandleMovement();
    }

    private void HandleInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        if (Input.GetButtonDown("Jump"))
            jumpBufferTimer = jumpBufferTime;
    }

    private void HandleTimers()
    {
        coyoteTimer -= Time.deltaTime;
        jumpBufferTimer -= Time.deltaTime;
    }

    private void HandleJump()
    {
        if (jumpBufferTimer > 0f && (IsGrounded || coyoteTimer > 0f || jumpCount < maxJumps))
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpVelocity);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            jumpCount++;
            // Instantly set grounded to false to prevent multiple jumps on the same frame
            IsGrounded = false;
        }

        // Apply better jump physics in FixedUpdate
        if (rb.velocity.y < 0f)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (rb.velocity.y > 0f && !Input.GetButton("Jump"))
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    private void HandleMovement()
    {
        float targetSpeed = horizontalInput * maxSpeed;
        float speedDiff = targetSpeed - rb.velocity.x;
        bool accelerating = Mathf.Abs(targetSpeed) > 0.01f;
        float accelRate = IsGrounded ? (accelerating ? groundAccel : groundDecel) : (accelerating ? airAccel : airDecel);
        float movement = speedDiff * accelRate; // Simplified for FixedUpdate

        rb.AddForce(movement * Vector2.right);

        // Clamp velocity to maxSpeed
        rb.velocity = new Vector2(Mathf.Clamp(rb.velocity.x, -maxSpeed, maxSpeed), rb.velocity.y);
    }

    private void CheckGrounded()
    {
        Vector2 boxPosition = (Vector2)transform.position + new Vector2(0, groundCheckYOffset);
        bool wasGrounded = IsGrounded;
        IsGrounded = Physics2D.BoxCast(boxPosition, groundCheckSize, 0f, Vector2.down, groundCheckDistance, groundLayer);

        if (IsGrounded && !wasGrounded)
        {
            jumpCount = 0;
            coyoteTimer = coyoteTime;
        }

        animator.SetBool("IsGrounded", IsGrounded);
    }

    private void UpdateAnimator()
    {
        animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
        // Add this line to control the falling animation
        animator.SetFloat("VerticalVelocity", rb.velocity.y);
    }

    private void HandleFacingDirection()
    {
        if (horizontalInput > 0.01f)
            transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
        else if (horizontalInput < -0.01f)
            transform.localScale = new Vector3(-2.5f, 2.5f, 2.5f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(doubleJumpZoneTag))
        {
            if (!hasDoubleJump)
            {
                hasDoubleJump = true;
                if (doubleJumpText != null)
                {
                    doubleJumpText.text = "Double Jump Unlocked!";
                    doubleJumpText.gameObject.SetActive(true);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector2 boxPosition = (Vector2)transform.position + new Vector2(0, groundCheckYOffset);
        Gizmos.DrawWireCube(boxPosition + Vector2.down * groundCheckDistance, groundCheckSize);
    }
}