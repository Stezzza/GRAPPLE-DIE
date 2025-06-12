using UnityEngine;
using TMPro;

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

    [Header("Double Jump")]
    public TextMeshProUGUI doubleJumpText;
    public string doubleJumpZoneTag = "DoubleJumpZone";
    public bool hasDoubleJump = false;

    private Rigidbody2D rb;
    private Animator animator;
    private DashAttack dashAttack;

    public bool IsGrounded { get; private set; }
    private float coyoteTimer;
    private float jumpBufferTimer;

    private bool doubleJumpActivated = false;
    private int jumpCount = 0;
    private int maxJumps = 1;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        dashAttack = GetComponent<DashAttack>();

        if (doubleJumpText != null)
            doubleJumpText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused)
            return;

        maxJumps = hasDoubleJump ? 2 : 1;

        coyoteTimer -= Time.deltaTime;
        jumpBufferTimer -= Time.deltaTime;

        if (Input.GetButtonDown("Jump"))
            jumpBufferTimer = jumpBufferTime;

        if (dashAttack != null && dashAttack.IsDashing)
        {
            animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
            animator.SetBool("IsGrounded", IsGrounded);
        }
        else
        {
            if (jumpBufferTimer > 0f && (IsGrounded || coyoteTimer > 0f || jumpCount < maxJumps))
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpVelocity);
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                IsGrounded = false;
                jumpCount++;
            }

            if (rb.velocity.y > 0f && !Input.GetButton("Jump"))
                rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.deltaTime;
            else if (rb.velocity.y < 0f)
                rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.deltaTime;

            float targetSpeed = Input.GetAxisRaw("Horizontal") * maxSpeed;
            float speedDiff = targetSpeed - rb.velocity.x;
            bool accelerating = Mathf.Abs(targetSpeed) > 0.01f;
            float accelRate = IsGrounded ? (accelerating ? groundAccel : groundDecel) : (accelerating ? airAccel : airDecel);
            float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, 0.9f) * Mathf.Sign(speedDiff) * Time.deltaTime;

            rb.velocity = new Vector2(rb.velocity.x + movement, rb.velocity.y);
            rb.velocity = new Vector2(Mathf.Clamp(rb.velocity.x, -maxSpeed, maxSpeed), rb.velocity.y);

            animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
            animator.SetBool("IsGrounded", IsGrounded);
        }

        if (Mathf.Abs(rb.velocity.x) > 0.01f)
            transform.localScale = new Vector3(Mathf.Sign(rb.velocity.x) * 2.5f, 2.5f, 2.5f);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y > 0.5f)
                {
                    IsGrounded = true;
                    jumpCount = 0;
                    coyoteTimer = coyoteTime;
                    break;
                }
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
            IsGrounded = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!doubleJumpActivated && other.CompareTag(doubleJumpZoneTag))
        {
            hasDoubleJump = true;
            doubleJumpActivated = true;

            if (doubleJumpText != null)
            {
                doubleJumpText.text = "Double Jump Unlocked!";
                doubleJumpText.gameObject.SetActive(true);
            }
        }
    }
}
