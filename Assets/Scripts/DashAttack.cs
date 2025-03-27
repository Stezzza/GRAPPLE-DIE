using UnityEngine;
using System.Collections;

public class DashAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.15f;
    public float damageRadius = 0.5f;
    public int damageAmount = 50;
    public LayerMask enemyLayer;

    [Header("Dash Visual Effects")]
    public GameObject dashParticleEffect; // Effect at the start of a dash.
    public GameObject hitParticleEffect;  // Effect when hitting an enemy.

    [Header("Camera Shake Settings")]
    public float dashCameraShakeDuration = 0.1f;
    public float dashCameraShakeMagnitude = 0.1f;
    public float hitCameraShakeDuration = 0.2f;
    public float hitCameraShakeMagnitude = 0.15f;

    [Header("Dash Trail")]
    public TrailRenderer dashTrail;

    [Header("Ground Check Settings")]
    public Transform groundCheck;         // Assign an empty GameObject positioned at your character's feet.
    public float groundCheckRadius = 0.2f;  // Adjust as needed.
    public LayerMask groundLayer;         // Set this to the layer your ground objects are on.

    private Rigidbody2D rb;
    private Camera mainCam;
    private Animator animator;
    private bool canDash = true;  // Indicates whether a dash is available.
    private bool isDashing = false; // Tracks whether the dash is active.

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;
        animator = GetComponent<Animator>();

        if (dashTrail != null)
        {
            dashTrail.emitting = false;
            dashTrail.time = 0.05f;
            dashTrail.startWidth = 0.1f;
            dashTrail.endWidth = 0f;
        }
    }

    void Update()
    {
        // Check if the player is grounded and not currently dashing.
        if (!canDash && !isDashing && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer))
        {
            canDash = true;
        }

        // Trigger dash on left mouse button, if dash is available.
        if (Input.GetMouseButtonDown(0) && canDash)
        {
            Vector2 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dashDirection = (mousePos - (Vector2)transform.position).normalized;
            HandleFacingDirection(dashDirection);
            StartCoroutine(PerformDash(dashDirection));
        }
    }

    private IEnumerator PerformDash(Vector2 direction)
    {
        canDash = false;
        isDashing = true;

        // Spawn dash particle effect at the start of the dash.
        if (dashParticleEffect != null)
        {
            Instantiate(dashParticleEffect, transform.position, Quaternion.identity);
        }

        // Trigger a brief camera shake for dash initiation.
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(dashCameraShakeDuration, dashCameraShakeMagnitude);
        }

        if (dashTrail != null)
            dashTrail.emitting = true;

        animator.SetTrigger("attack1");

        float elapsedTime = 0f;
        while (elapsedTime < dashDuration && isDashing)
        {
            rb.velocity = direction * dashSpeed;

            // Check for enemy hits during dash.
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, damageRadius, enemyLayer);
            foreach (Collider2D enemy in hitEnemies)
            {
                // Spawn hit particle effect at the enemy's position.
                if (hitParticleEffect != null)
                {
                    Instantiate(hitParticleEffect, enemy.transform.position, Quaternion.identity);
                }

                // Trigger a camera shake on enemy hit.
                if (CameraShake.Instance != null)
                {
                    CameraShake.Instance.Shake(hitCameraShakeDuration, hitCameraShakeMagnitude);
                }

                // Apply damage if the enemy has an EnemyHealth component.
                EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(damageAmount);
                }
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // End dash normally if duration elapsed.
        EndDash();
    }

    private void HandleFacingDirection(Vector2 direction)
    {
        Vector3 localScale = transform.localScale;
        if (direction.x > 0)
            localScale.x = Mathf.Abs(localScale.x);
        else if (direction.x < 0)
            localScale.x = -Mathf.Abs(localScale.x);
        transform.localScale = localScale;
    }

    // End the dash: reset velocity and stop dash effects.
    private void EndDash()
    {
        isDashing = false;
        rb.velocity = Vector2.zero;
        if (dashTrail != null)
            dashTrail.emitting = false;
        animator.Play("Idle");
        // Note: Dash is not immediately reset here.
        // It will only be available again after landing (ground check) or upon a successful grapple.
    }

    // Reset dash externally on a successful grapple.
    public void GrappleSuccess()
    {
        if (isDashing)
        {
            EndDash();
        }
        // Optionally, recharge dash upon grapple success.
        canDash = true;
    }

    // End the dash immediately if colliding with an object while dashing.
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDashing)
        {
            EndDash();
        }
    }

    void OnDrawGizmosSelected()
    {
        // Visualize the damage radius.
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}
