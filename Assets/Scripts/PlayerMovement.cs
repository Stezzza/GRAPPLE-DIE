using UnityEngine;
using TMPro;  // Required for TextMeshPro references

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

    [Header("Speed Boost")]
    [Tooltip("TextMeshProUGUI element that displays once the boost is collected")]
    public TextMeshProUGUI boostIndicatorText;

    [Tooltip("Tag of the trigger object that grants a permanent speed boost")]
    public string speedBoostZoneTag = "SpeedBoostZone";

    [Tooltip("Multiplier applied to maxSpeed when the boost is picked up (e.g. 1.3 = +30%)")]
    [Range(1f, 2f)]
    public float boostMultiplier = 1.3f;

    private Rigidbody2D rb;
    private Animator animator;
    private DashAttack dashAttack;

    public bool IsGrounded { get; private set; }
    private float coyoteTimer;
    private float jumpBufferTimer;

    // When true, the boost has already been applied once; do not apply again
    private bool isBoosted = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        dashAttack = GetComponent<DashAttack>();

        if (dashAttack == null)
        {
            Debug.LogWarning("PlayerMovement: DashAttack component not found on this GameObject.");
        }

        // Ensure the boost indicator is hidden at the start
        if (boostIndicatorText != null)
        {
            boostIndicatorText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused)
            return;

        // Update timers for coyote time and jump buffering
        coyoteTimer -= Time.deltaTime;
        jumpBufferTimer -= Time.deltaTime;

        if (Input.GetButtonDown("Jump"))
            jumpBufferTimer = jumpBufferTime;

        // If dashing, let DashAttack handle movement; only update animator here
        if (dashAttack != null && dashAttack.IsDashing)
        {
            animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
            animator.SetBool("IsGrounded", IsGrounded);
        }
        else
        {
            // Handle jump if buffered and within coyote time
            if (jumpBufferTimer > 0f && (IsGrounded || coyoteTimer > 0f))
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpVelocity);
                IsGrounded = false;
                coyoteTimer = 0f;
                jumpBufferTimer = 0f;
            }

            // Apply variable jump height modifications
            if (rb.velocity.y > 0f && !Input.GetButton("Jump"))
            {
                rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.deltaTime;
            }
            else if (rb.velocity.y < 0f)
            {
                rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.deltaTime;
            }

            // Calculate horizontal movement smoothing
            float targetSpeed = Input.GetAxisRaw("Horizontal") * maxSpeed;
            float speedDiff = targetSpeed - rb.velocity.x;
            bool accelerating = Mathf.Abs(targetSpeed) > 0.01f;
            float accelRate = IsGrounded
                ? (accelerating ? groundAccel : groundDecel)
                : (accelerating ? airAccel : airDecel);
            float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, 0.9f)
                             * Mathf.Sign(speedDiff) * Time.deltaTime;
            rb.velocity = new Vector2(rb.velocity.x + movement, rb.velocity.y);

            // Clamp horizontal velocity to maxSpeed
            rb.velocity = new Vector2(Mathf.Clamp(rb.velocity.x, -maxSpeed, maxSpeed), rb.velocity.y);

            // Update animator parameters
            animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
            animator.SetBool("IsGrounded", IsGrounded);
        }

        // Flip sprite based on horizontal velocity
        if (Mathf.Abs(rb.velocity.x) > 0.01f)
        {
            transform.localScale = new Vector3(Mathf.Sign(rb.velocity.x) * 2.5f, 2.5f, 2.5f);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
        {
            bool onValidGround = false;
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y > 0.5f)
                {
                    onValidGround = true;
                    break;
                }
            }

            if (onValidGround)
            {
                IsGrounded = true;
                coyoteTimer = coyoteTime;
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
        {
            IsGrounded = false;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // If the player enters a trigger tagged "SpeedBoostZone" and has not yet been boosted
        if (!isBoosted && other.CompareTag(speedBoostZoneTag))
        {
            // Permanently apply the speed boost
            maxSpeed *= boostMultiplier;
            isBoosted = true;

            if (boostIndicatorText != null)
            {
                boostIndicatorText.text = "Run Speed +30%";
                boostIndicatorText.gameObject.SetActive(true);
            }
        }
    }

    // No OnTriggerExit2D needed since boost is permanent
}
