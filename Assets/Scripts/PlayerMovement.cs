using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
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

    private Rigidbody2D rb;
    private Animator animator;

    private bool isGrounded;
    private float coyoteTimer;
    private float jumpBufferTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused)
            return;

        // Timers for coyote time and jump buffering
        coyoteTimer -= Time.deltaTime;
        jumpBufferTimer -= Time.deltaTime;

        if (Input.GetButtonDown("Jump"))
            jumpBufferTimer = jumpBufferTime;

        // Jump if buffered and within coyote window
        if (jumpBufferTimer > 0f && (isGrounded || coyoteTimer > 0f))
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpVelocity);
            isGrounded = false;
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
        }

        // Variable jump height
        if (rb.velocity.y > 0f && !Input.GetButton("Jump"))
            rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.deltaTime;
        else if (rb.velocity.y < 0f)
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.deltaTime;

        // Horizontal movement smoothing
        float targetSpeed = Input.GetAxisRaw("Horizontal") * maxSpeed;
        float speedDiff = targetSpeed - rb.velocity.x;
        bool accelerating = Mathf.Abs(targetSpeed) > 0.01f;
        float accelRate = isGrounded ? (accelerating ? groundAccel : groundDecel) : (accelerating ? airAccel : airDecel);
        float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, 0.9f) * Mathf.Sign(speedDiff) * Time.deltaTime;
        rb.velocity = new Vector2(rb.velocity.x + movement, rb.velocity.y);

        // Clamp horizontal speed
        rb.velocity = new Vector2(Mathf.Clamp(rb.velocity.x, -maxSpeed, maxSpeed), rb.velocity.y);

        // Maintain uniform scale of (2.5,2.5,2.5), flipping only the X-axis
        if (Mathf.Abs(rb.velocity.x) > 0.1f)
            transform.localScale = new Vector3(Mathf.Sign(rb.velocity.x) * 2.5f, 2.5f, 2.5f);
        else
            transform.localScale = new Vector3(transform.localScale.x, 2.5f, 2.5f);

        // Update animator parameters
        animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
        animator.SetBool("IsGrounded", isGrounded);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
        {
            isGrounded = true;
            coyoteTimer = coyoteTime;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
            isGrounded = false;
    }
}
