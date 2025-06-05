using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DashAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float dashSpeed = 25f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float damageRadius = 0.5f;
    [SerializeField] private int damageAmount = 50;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Cooldown Settings (Grapple Only)")]
    [SerializeField] private bool rechargeDashOnGrapple = true;

    [Header("Dash Visual Effects")]
    [SerializeField] private GameObject dashParticleEffect;
    [SerializeField] private GameObject hitParticleEffect;

    [Header("Camera Shake Settings")]
    [SerializeField] private float dashCameraShakeDuration = 0.1f;
    [SerializeField] private float dashCameraShakeMagnitude = 0.1f;
    [SerializeField] private float hitCameraShakeDuration = 0.2f;
    [SerializeField] private float hitCameraShakeMagnitude = 0.15f;

    [Header("Dash Trail")]
    [SerializeField] private TrailRenderer dashTrail;

    // Removed: Ground Check Settings - We will use PlayerMovement's grounded state
    // [SerializeField] private Transform groundCheck;
    // [SerializeField] private float groundCheckRadius = 0.2f;
    // [SerializeField] private LayerMask groundLayer;

    // Cached components
    private Rigidbody2D rb;
    private Camera mainCam;
    private Animator animator;
    private PlayerMovement playerMovement; // Reference to PlayerMovement script

    // State tracking
    private bool isDashing = false;
    private bool hasDashCharge = false;
    private bool wasGrounded;

    private readonly HashSet<Collider2D> hitEnemiesThisDash = new HashSet<Collider2D>();
    private static readonly int Attack1Hash = Animator.StringToHash("attack1");
    private static readonly int IdleHash = Animator.StringToHash("Idle");

    public bool IsDashing => isDashing;
    public bool HasDashCharge => hasDashCharge;

    #region Unity Lifecycle
    private void Awake()
    {
        CacheComponents();
        InitializeTrail();
        playerMovement = GetComponent<PlayerMovement>(); // Get reference
        if (playerMovement == null)
        {
            Debug.LogError("DashAttack: PlayerMovement component not found on this GameObject! Dash recharge will not work correctly.");
        }
    }

    private void Start()
    {
        // Initialize wasGrounded using PlayerMovement's state if available
        wasGrounded = InternalIsGrounded();
        if (wasGrounded)
        {
            hasDashCharge = true;
        }
    }

    private void Update()
    {
        bool isCurrentlyGrounded = InternalIsGrounded();

        if (isCurrentlyGrounded && !wasGrounded && !isDashing)
        {
            hasDashCharge = true;
        }

        HandleDashInput();

        wasGrounded = isCurrentlyGrounded;
    }

    // OnCollisionEnter2D from original DashAttack script was removed as EndDash is handled by coroutine.
    // If specific collision during dash should end it, that logic can be re-added here or in PerformDash.

    private void OnDrawGizmosSelected()
    {
        // Damage radius Gizmo
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, damageRadius);

        // Removed ground check Gizmo as it's no longer used by this script
    }
    #endregion

    #region Initialization
    private void CacheComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;
        animator = GetComponent<Animator>();

        if (rb == null) Debug.LogError("Rigidbody2D component required!", this);
        if (mainCam == null) Debug.LogError("Main Camera not found!", this);
        // Removed: if (groundCheck == null) Debug.LogError("Ground check Transform not assigned!", this);
    }

    private void InitializeTrail()
    {
        if (dashTrail != null)
        {
            dashTrail.emitting = false;
            dashTrail.time = 0.05f;
            dashTrail.startWidth = 0.1f;
            dashTrail.endWidth = 0f;
        }
    }
    #endregion

    #region Input and Ground Handling (Now relies on PlayerMovement for ground state)
    private void HandleDashInput()
    {
        if (Input.GetMouseButtonDown(0) && hasDashCharge && !isDashing)
        {
            Vector2 mouseWorldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dashDirection = (mouseWorldPos - (Vector2)transform.position).normalized;

            if (dashDirection.magnitude > 0.1f)
            {
                HandleFacingDirection(dashDirection);
                StartCoroutine(PerformDash(dashDirection));
            }
        }
    }

    // This method now uses PlayerMovement's IsGrounded state
    private bool InternalIsGrounded()
    {
        if (playerMovement != null)
        {
            return playerMovement.IsGrounded;
        }
        // Fallback if PlayerMovement is missing, though an error is logged in Awake.
        return false;
    }
    #endregion

    #region Dash Execution
    private IEnumerator PerformDash(Vector2 direction)
    {
        StartDash(direction);

        float originalGravityScale = rb.gravityScale; // Store original gravity
        rb.gravityScale = 0f; // No gravity during dash for consistent movement

        float elapsedTime = 0f;
        while (elapsedTime < dashDuration && isDashing)
        {
            rb.velocity = direction * dashSpeed;
            CheckForEnemyHits();

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rb.gravityScale = originalGravityScale; // Restore gravity
        EndDash();
    }

    private void StartDash(Vector2 direction)
    {
        isDashing = true;
        hasDashCharge = false;
        hitEnemiesThisDash.Clear();

        SpawnDashEffect();
        TriggerCameraShake(dashCameraShakeDuration, dashCameraShakeMagnitude);

        if (dashTrail != null)
            dashTrail.emitting = true;

        if (animator != null)
            animator.SetTrigger(Attack1Hash);
    }

    private void EndDash()
    {
        isDashing = false;
        // Don't zero out velocity here if you want to carry momentum,
        // or if PlayerMovement should resume control smoothly.
        // For now, let PlayerMovement take over. If dash ends mid-air, gravity will apply.
        // rb.velocity = Vector2.zero; // Original - might feel abrupt.

        if (dashTrail != null)
            dashTrail.emitting = false;

        if (animator != null && gameObject.activeInHierarchy) // Check if animator is still valid
            animator.Play(IdleHash); // Consider using a "DashEnd" trigger or just letting movement states take over
    }
    #endregion

    #region Combat System
    private void CheckForEnemyHits()
    {
        Collider2D[] hitResults = new Collider2D[10];
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            damageRadius,
            hitResults,
            enemyLayer
        );

        for (int i = 0; i < hitCount; i++)
        {
            var enemy = hitResults[i];
            if (enemy != null && !hitEnemiesThisDash.Contains(enemy))
            {
                ProcessEnemyHit(enemy);
                hitEnemiesThisDash.Add(enemy);
            }
        }
    }

    private void ProcessEnemyHit(Collider2D enemy)
    {
        SpawnHitEffect(enemy.transform.position);
        TriggerCameraShake(hitCameraShakeDuration, hitCameraShakeMagnitude);

        var enemyHealth = enemy.GetComponent<EnemyHealth>(); // Assuming you have an EnemyHealth script
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damageAmount);
        }

        var damageable = enemy.GetComponent<IDamageable>(); // Using the interface
        if (damageable != null)
        {
            damageable.TakeDamage(damageAmount);
        }
    }
    #endregion

    #region Visual Effects
    private void SpawnDashEffect()
    {
        if (dashParticleEffect != null)
            Instantiate(dashParticleEffect, transform.position, Quaternion.identity);
    }

    private void SpawnHitEffect(Vector3 position)
    {
        if (hitParticleEffect != null)
            Instantiate(hitParticleEffect, position, Quaternion.identity);
    }

    private void TriggerCameraShake(float duration, float magnitude)
    {
        // Assuming you have a CameraShake singleton or static instance
        // if (CameraShake.Instance != null)
        // CameraShake.Instance.Shake(duration, magnitude);
    }

    private void HandleFacingDirection(Vector2 direction)
    {
        // This will be overridden by PlayerMovement's facing logic unless PlayerMovement's logic is also conditional.
        // PlayerMovement already handles flipping based on rb.velocity.x.
        // So, this method in DashAttack might be redundant or could be removed if PlayerMovement's handling is preferred.
        // For now, let it be, but be aware of potential conflict or redundancy.
        // if (Mathf.Abs(direction.x) < 0.1f) return;
        // Vector3 localScale = transform.localScale;
        // localScale.x = direction.x > 0 ? Mathf.Abs(localScale.x) : -Mathf.Abs(localScale.x);
        // transform.localScale = localScale;
    }
    #endregion

    #region Public Methods
    public void GrappleSuccess()
    {
        if (isDashing)
            EndDash();

        if (rechargeDashOnGrapple)
        {
            hasDashCharge = true;
        }
    }

    public void CancelDash()
    {
        if (isDashing)
        {
            // Need to ensure gravity is restored if PerformDash was interrupted
            rb.gravityScale = playerMovement != null ? playerMovement.GetComponent<Rigidbody2D>().gravityScale : 1f; // Assuming playerMovement holds reference to default gravity scale or get from rb
            EndDash();
        }
    }

    public bool TryDash(Vector2 direction)
    {
        if (!hasDashCharge || isDashing) return false;

        direction = direction.normalized;
        if (direction.magnitude < 0.1f) return false;

        // HandleFacingDirection(direction); // Potentially redundant, see note above
        StartCoroutine(PerformDash(direction));
        return true;
    }
    #endregion
}

// Interface for alternative damage system
public interface IDamageable
{
    void TakeDamage(int damage);
}

// Dummy CameraShake and EnemyHealth for compilation if not present
// public class CameraShake : MonoBehaviour { public static CameraShake Instance; public void Shake(float d, float m) {} }
// public class EnemyHealth : MonoBehaviour { public void TakeDamage(int d) {} }
// public class GameManager : MonoBehaviour { public static GameManager Instance; public bool IsPaused; }