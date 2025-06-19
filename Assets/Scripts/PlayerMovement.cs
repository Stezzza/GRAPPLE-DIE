using UnityEngine;
using TMPro;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("movement")]
    public float maxSpeed = 8f;
    public float groundAccel = 80f;
    public float groundDecel = 80f;
    public float airAccel = 40f;
    public float airDecel = 40f;

    [Header("jump")]
    public float jumpVelocity = 12f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;
    public float lowJumpMultiplier = 3f;
    public float fallMultiplier = 5f;

    [Header("ground check")]
    public LayerMask groundLayer;
    public Vector2 groundCheckSize = new Vector2(0.8f, 0.2f);
    public float groundCheckYOffset = -0.5f;
    public float groundCheckDistance = 0.1f;

    [Header("double jump")]
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

        // inputs and timers
        HandleInput();
        HandleTimers();

        // visuals
        UpdateAnimator();
        HandleFacingDirection();
    }

    void FixedUpdate()
    {
        // physics
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
            IsGrounded = false;
        }

        // better jump physics
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
        float movement = speedDiff * accelRate;

        rb.AddForce(movement * Vector2.right);

        // limit speed
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