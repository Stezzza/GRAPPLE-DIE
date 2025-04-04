using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float jumpForce = 2f;
    private Rigidbody2D rb;
    private Animator animator;

    private bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();  // Grab the Animator on the same GameObject
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        rb.velocity = new Vector2(horizontal * moveSpeed, rb.velocity.y);

        // Flip the sprite based on horizontal movement direction
        if (horizontal > 0.01f) // Moving right
        {
            transform.localScale = new Vector3(1f, transform.localScale.y, transform.localScale.z);
        }
        else if (horizontal < -0.01f) // Moving left
        {
            transform.localScale = new Vector3(-1f, transform.localScale.y, transform.localScale.z);
        }

        // Update Speed parameter (absolute value of horizontal velocity)
        animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));

        // Jump
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }

        // Check if grounded for state transitions
        animator.SetBool("IsGrounded", isGrounded);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // If collision normal is mostly up, we consider this ground
        foreach (var contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;
                return;
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        // No longer grounded
        isGrounded = false;
    }
}
