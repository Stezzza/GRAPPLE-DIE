using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DashAttack : MonoBehaviour
{
    [Header("attack settings")]
    [SerializeField] private float dashSpeed = 25f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float damageRadius = 0.5f;
    [SerializeField] private int damageAmount = 50;
    [SerializeField] private LayerMask enemyLayer;

    [Header("cooldown settings (grapple only)")]
    [SerializeField] private bool rechargeDashOnGrapple = true;

    [Header("dash visual effects")]
    [SerializeField] private GameObject dashParticleEffect;
    [SerializeField] private GameObject hitParticleEffect;

    [Header("camera shake settings")]
    [SerializeField] private float dashCameraShakeDuration = 0.1f;
    [SerializeField] private float dashCameraShakeMagnitude = 0.1f;
    [SerializeField] private float hitCameraShakeDuration = 0.2f;
    [SerializeField] private float hitCameraShakeMagnitude = 0.15f;

    [Header("dash trail")]
    [SerializeField] private TrailRenderer dashTrail;

    // component refs
    private Rigidbody2D rb;
    private Camera mainCam;
    private Animator animator;
    private PlayerMovement playerMovement;

    // state
    private bool isDashing = false;
    private bool hasDashCharge = false;
    private bool wasGrounded;

    private readonly HashSet<Collider2D> hitEnemiesThisDash = new HashSet<Collider2D>();
    private static readonly int Attack1Hash = Animator.StringToHash("attack1");
    private static readonly int IdleHash = Animator.StringToHash("Idle");

    public bool IsDashing => isDashing;
    public bool HasDashCharge => hasDashCharge;

    // setup
    private void Awake()
    {
        CacheComponents();
        InitializeTrail();
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("dashattack: playermovement component not found");
        }
    }

    // more setup
    private void Start()
    {
        wasGrounded = InternalIsGrounded();
        if (wasGrounded)
        {
            hasDashCharge = true;
        }
    }

    // main loop
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

    // gizmos for scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }

    // gets components
    private void CacheComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;
        animator = GetComponent<Animator>();

        if (rb == null) Debug.LogError("rigidbody2d required", this);
        if (mainCam == null) Debug.LogError("main camera not found", this);
    }

    // setup for trail renderer
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

    // checks for mouse click
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

    // check if grounded using player movement script
    private bool InternalIsGrounded()
    {
        if (playerMovement != null)
        {
            return playerMovement.IsGrounded;
        }
        return false;
    }

    // does the dash
    private IEnumerator PerformDash(Vector2 direction)
    {
        StartDash(direction);

        float originalGravityScale = rb.gravityScale;
        rb.gravityScale = 0f; // turn off gravity

        float elapsedTime = 0f;
        while (elapsedTime < dashDuration && isDashing)
        {
            rb.velocity = direction * dashSpeed;
            CheckForEnemyHits();

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rb.gravityScale = originalGravityScale; // turn gravity back on
        EndDash();
    }

    // stuff to do at start of dash
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

    // stuff to do at end of dash
    private void EndDash()
    {
        isDashing = false;

        if (dashTrail != null)
            dashTrail.emitting = false;

        if (animator != null && gameObject.activeInHierarchy)
            animator.Play(IdleHash);
    }

    // checks for enemies in radius
    private void CheckForEnemyHits()
    {
        Collider2D[] hitResults = new Collider2D[10];
        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, damageRadius, hitResults, enemyLayer);

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

    // what to do when an enemy is hit
    private void ProcessEnemyHit(Collider2D enemy)
    {
        SpawnHitEffect(enemy.transform.position);
        TriggerCameraShake(hitCameraShakeDuration, hitCameraShakeMagnitude);

        var enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damageAmount);
        }

        var damageable = enemy.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damageAmount);
        }
    }

    // makes dash particles
    private void SpawnDashEffect()
    {
        if (dashParticleEffect != null)
            Instantiate(dashParticleEffect, transform.position, Quaternion.identity);
    }

    // makes hit particles
    private void SpawnHitEffect(Vector3 position)
    {
        if (hitParticleEffect != null)
            Instantiate(hitParticleEffect, position, Quaternion.identity);
    }

    // shakes camera
    private void TriggerCameraShake(float duration, float magnitude)
    {
        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(duration, magnitude);
    }

    // makes player face dash direction
    private void HandleFacingDirection(Vector2 direction)
    {
        // empty for now, playermovement handles it
    }

    // called when grapple hits
    public void GrappleSuccess()
    {
        if (isDashing)
            EndDash();

        if (rechargeDashOnGrapple)
        {
            hasDashCharge = true;
        }
    }

    // stops the dash early
    public void CancelDash()
    {
        if (isDashing)
        {
            rb.gravityScale = playerMovement != null ? playerMovement.GetComponent<Rigidbody2D>().gravityScale : 1f;
            EndDash();
        }
    }

    // tries to dash
    public bool TryDash(Vector2 direction)
    {
        if (!hasDashCharge || isDashing) return false;

        direction = direction.normalized;
        if (direction.magnitude < 0.1f) return false;

        StartCoroutine(PerformDash(direction));
        return true;
    }
}

// interface for things that can be damaged
public interface IDamageable
{
    void TakeDamage(int damage);
}