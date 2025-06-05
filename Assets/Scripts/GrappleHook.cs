using UnityEngine;
using System.Collections;

public class GrappleHook : MonoBehaviour
{
    [Header("Grapple Settings")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform hookOrigin;
    [SerializeField] private float maxGrappleDistance = 50f;
    [SerializeField] private LayerMask grappleLayer;
    [SerializeField] private float hookTravelSpeed = 100f;
    [SerializeField] private bool allowGrappleWhileGrounded = false;

    [Header("Swing Physics")]
    [SerializeField] private float swingForceMultiplier = 20f;
    [SerializeField] private float verticalClimbSpeed = 15f;
    [SerializeField] private float maxSwingSpeed = 30f;
    [SerializeField] private float airResistance = 0.95f;

    [Header("Rope Controls")]
    [SerializeField] private float ropeExtendSpeed = 10f;
    [SerializeField] private float ropeRetractSpeed = 15f;
    [SerializeField] private float minRopeLength = 2f;
    [SerializeField] private float maxRopeLength = 60f;
    [SerializeField] private bool autoAdjustRopeLength = true;
    [SerializeField] private LayerMask groundLayer; // Add ground detection

    [Header("Advanced Physics")]
    [SerializeField] private float momentumConservation = 0.8f;
    [SerializeField] private float releaseBoostMultiplier = 1.2f;
    [SerializeField] private AnimationCurve swingForceCurve = AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1f);

    [Header("Visual Effects")]
    [SerializeField] private GameObject hookProjectile;
    [SerializeField] private GameObject grappleHitEffect;
    [SerializeField] private float ropeWaveAmplitude = 0.1f;
    [SerializeField] private float ropeWaveFrequency = 2f;
    [SerializeField] private int ropeSegments = 20;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip grappleFireSound;
    [SerializeField] private AudioClip grappleHitSound;
    [SerializeField] private AudioClip grappleReleaseSound;

    // Cached components
    private Rigidbody2D rb;
    private DistanceJoint2D ropeJoint;
    private Camera mainCam;
    private DashAttack dashAttack;

    // State tracking
    private Vector2 grapplePoint;
    private Vector2 hookVelocity;
    private bool isGrappling = false;
    private bool isHookTraveling = false;
    private bool wasGrappling = false;
    private float currentRopeLength;
    private float targetRopeLength;
    private Vector2 releaseVelocity;

    // Input tracking
    private float horizontalInput;
    private float verticalInput;
    private bool grappleInputHeld;
    private bool grappleInputPressed;
    private bool grappleInputReleased;

    // Visual rope points
    private Vector3[] ropePoints;

    // Properties
    public bool IsGrappling => isGrappling;
    public bool IsHookTraveling => isHookTraveling;
    public float RopeLength => currentRopeLength;
    public Vector2 GrapplePoint => grapplePoint;

    #region Unity Lifecycle
    private void Awake()
    {
        CacheComponents();
        InitializeRope();
    }

    private void Update()
    {
        HandleInput();
        HandleGrappleLogic();
        UpdateVisuals();
    }

    private void FixedUpdate()
    {
        if (isGrappling)
        {
            ApplySwingPhysics();
            HandleRopeLength();
            ApplyAirResistance();
        }
    }

    private void LateUpdate()
    {
        // Track state changes for momentum conservation
        if (wasGrappling && !isGrappling)
        {
            ApplyReleaseBoost();
        }
        wasGrappling = isGrappling;
    }
    #endregion

    #region Initialization
    private void CacheComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;
        dashAttack = GetComponent<DashAttack>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Optimize rigidbody settings
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;
    }

    private void InitializeRope()
    {
        ropePoints = new Vector3[ropeSegments + 1];

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
            lineRenderer.useWorldSpace = true;
            lineRenderer.sortingOrder = 10;
        }
    }
    #endregion

    #region Input Handling
    private void HandleInput()
    {
        // Grapple input
        grappleInputPressed = Input.GetMouseButtonDown(1);
        grappleInputHeld = Input.GetMouseButton(1);
        grappleInputReleased = Input.GetMouseButtonUp(1);

        // Movement input with deadzone
        float rawHorizontal = Input.GetAxisRaw("Horizontal");
        float rawVertical = Input.GetAxisRaw("Vertical");

        horizontalInput = Mathf.Abs(rawHorizontal) > 0.1f ? rawHorizontal : 0f;
        verticalInput = Mathf.Abs(rawVertical) > 0.1f ? rawVertical : 0f;
    }
    #endregion

    #region Grapple Logic
    private void HandleGrappleLogic()
    {
        if (grappleInputPressed && !isGrappling && !isHookTraveling)
        {
            TryFireGrapple();
        }
        else if (grappleInputReleased && (isGrappling || isHookTraveling))
        {
            ReleaseGrapple();
        }
    }

    private void TryFireGrapple()
    {
        // Check if grounded and grappling is disabled while grounded
        if (!allowGrappleWhileGrounded && IsGrounded())
            return;

        Vector2 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 grappleDirection = (mousePos - (Vector2)hookOrigin.position).normalized;

        StartCoroutine(FireHook(grappleDirection));
        PlaySound(grappleFireSound);
    }

    private IEnumerator FireHook(Vector2 direction)
    {
        isHookTraveling = true;
        Vector2 hookPosition = hookOrigin.position;
        hookVelocity = direction * hookTravelSpeed;

        // Spawn hook projectile if available
        GameObject hook = null;
        if (hookProjectile != null)
        {
            hook = Instantiate(hookProjectile, hookPosition, Quaternion.LookRotation(Vector3.forward, direction));
        }

        float travelDistance = 0f;
        while (travelDistance < maxGrappleDistance && isHookTraveling)
        {
            Vector2 newPosition = hookPosition + hookVelocity * Time.deltaTime;
            float segmentDistance = Vector2.Distance(hookPosition, newPosition);

            // Raycast for collision
            RaycastHit2D hit = Physics2D.Raycast(hookPosition, hookVelocity.normalized, segmentDistance, grappleLayer);

            if (hit.collider != null)
            {
                // Hit something!
                AttachGrapple(hit.point);
                if (hook != null) Destroy(hook);
                break;
            }

            hookPosition = newPosition;
            travelDistance += segmentDistance;

            // Update hook projectile position
            if (hook != null)
                hook.transform.position = hookPosition;

            // Show hook travel line
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, hookOrigin.position);
                lineRenderer.SetPosition(1, hookPosition);
            }

            yield return null;
        }

        // Hook missed or reached max distance
        if (isHookTraveling)
        {
            isHookTraveling = false;
            if (hook != null) Destroy(hook);
            if (lineRenderer != null) lineRenderer.positionCount = 0;
        }
    }

    private void AttachGrapple(Vector2 hitPoint)
    {
        grapplePoint = hitPoint;
        isHookTraveling = false;
        isGrappling = true;

        // Calculate rope length
        currentRopeLength = Vector2.Distance(transform.position, grapplePoint);
        targetRopeLength = currentRopeLength;

        // Create distance joint
        ropeJoint = gameObject.AddComponent<DistanceJoint2D>();
        ropeJoint.autoConfigureDistance = false;
        ropeJoint.connectedAnchor = grapplePoint;
        ropeJoint.distance = currentRopeLength;
        ropeJoint.enableCollision = false;
        ropeJoint.maxDistanceOnly = true;

        // Effects
        SpawnHitEffect();
        PlaySound(grappleHitSound);

        // Notify dash attack of successful grapple
        if (dashAttack != null)
            dashAttack.GrappleSuccess();
    }

    private void ReleaseGrapple()
    {
        if (ropeJoint != null)
        {
            Destroy(ropeJoint);
        }

        // Store release velocity for momentum boost
        if (isGrappling)
        {
            releaseVelocity = rb.velocity;
        }

        isGrappling = false;
        isHookTraveling = false;

        if (lineRenderer != null)
            lineRenderer.positionCount = 0;

        PlaySound(grappleReleaseSound);
    }
    #endregion

    #region Physics
    private void ApplySwingPhysics()
    {
        Vector2 playerToGrapple = grapplePoint - (Vector2)transform.position;
        Vector2 swingDirection = Vector2.Perpendicular(playerToGrapple).normalized;

        // Determine swing direction based on player position relative to grapple point
        if (transform.position.x > grapplePoint.x)
            swingDirection *= -1f;

        // Apply horizontal swing force
        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            float swingIntensity = swingForceCurve.Evaluate(Mathf.Abs(Vector2.Dot(rb.velocity.normalized, swingDirection)));
            Vector2 swingForce = swingDirection * horizontalInput * swingForceMultiplier * swingIntensity;
            rb.AddForce(swingForce);
        }

        // Apply vertical climbing force
        if (Mathf.Abs(verticalInput) > 0.1f)
        {
            Vector2 climbDirection = playerToGrapple.normalized;
            Vector2 climbForce = climbDirection * verticalInput * verticalClimbSpeed;
            rb.AddForce(climbForce);
        }

        // Limit maximum swing speed
        if (rb.velocity.magnitude > maxSwingSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSwingSpeed;
        }
    }

    private void HandleRopeLength()
    {
        // Manual rope length control
        if (Mathf.Abs(verticalInput) > 0.1f)
        {
            if (verticalInput > 0) // W key - shorten rope
            {
                targetRopeLength -= ropeRetractSpeed * Time.fixedDeltaTime;
            }
            else // S key - lengthen rope (but check for ground collision)
            {
                // Check if extending rope would put player through ground
                Vector2 playerToGrapple = grapplePoint - (Vector2)transform.position;
                Vector2 directionToGrapple = playerToGrapple.normalized;
                float extendAmount = ropeExtendSpeed * Time.fixedDeltaTime;

                // Raycast downward to check for ground
                RaycastHit2D groundHit = Physics2D.Raycast(
                    transform.position,
                    Vector2.down,
                    extendAmount + 1f,
                    groundLayer
                );

                // Also check in the direction we're moving when extending
                RaycastHit2D moveHit = Physics2D.Raycast(
                    transform.position,
                    -directionToGrapple,
                    extendAmount + 0.5f,
                    groundLayer
                );

                // Only extend if we won't hit ground
                if (groundHit.collider == null && moveHit.collider == null)
                {
                    targetRopeLength += extendAmount;
                }
                else
                {
                    // If we would hit ground, limit rope length to current distance
                    float safeDistance = Vector2.Distance(transform.position, grapplePoint) - 0.5f;
                    targetRopeLength = Mathf.Min(targetRopeLength, safeDistance);
                }
            }

            targetRopeLength = Mathf.Clamp(targetRopeLength, minRopeLength, maxRopeLength);
        }
        else if (autoAdjustRopeLength)
        {
            // Auto-adjust rope length based on momentum
            float velocityFactor = rb.velocity.magnitude / maxSwingSpeed;
            float desiredLength = Mathf.Lerp(currentRopeLength, targetRopeLength, velocityFactor * 0.1f);
            targetRopeLength = desiredLength;
        }

        // Smoothly adjust rope length
        currentRopeLength = Mathf.Lerp(currentRopeLength, targetRopeLength, Time.fixedDeltaTime * 5f);

        // Additional ground collision check when rope is shortening
        Vector2 currentPlayerToGrapple = grapplePoint - (Vector2)transform.position;
        if (currentPlayerToGrapple.magnitude < currentRopeLength)
        {
            // Check if current position would intersect with ground
            RaycastHit2D immediateGroundCheck = Physics2D.Raycast(
                transform.position,
                Vector2.down,
                0.6f,
                groundLayer
            );

            if (immediateGroundCheck.collider != null)
            {
                // Prevent rope from pulling player through ground
                float safeRopeLength = Vector2.Distance(transform.position, grapplePoint) + 0.5f;
                currentRopeLength = Mathf.Max(currentRopeLength, safeRopeLength);
            }
        }

        if (ropeJoint != null)
        {
            ropeJoint.distance = currentRopeLength;
        }
    }

    private void ApplyAirResistance()
    {
        rb.velocity *= airResistance;
    }

    private void ApplyReleaseBoost()
    {
        if (releaseVelocity.magnitude > 5f)
        {
            Vector2 boostVelocity = releaseVelocity * (releaseBoostMultiplier * momentumConservation);
            rb.velocity = boostVelocity;
        }
    }
    #endregion

    #region Visuals
    private void UpdateVisuals()
    {
        if (isGrappling && lineRenderer != null)
        {
            UpdateRopeVisual();
        }
    }

    private void UpdateRopeVisual()
    {
        lineRenderer.positionCount = ropeSegments + 1;

        Vector2 ropeStart = hookOrigin.position;
        Vector2 ropeEnd = grapplePoint;

        for (int i = 0; i <= ropeSegments; i++)
        {
            float t = (float)i / ropeSegments;
            Vector2 basePosition = Vector2.Lerp(ropeStart, ropeEnd, t);

            // Add rope sag/wave effect
            float sag = Mathf.Sin(t * Mathf.PI) * ropeWaveAmplitude * currentRopeLength * 0.1f;
            float wave = Mathf.Sin(Time.time * ropeWaveFrequency + t * Mathf.PI * 2) * ropeWaveAmplitude * 0.5f;

            Vector2 perpendicular = Vector2.Perpendicular((ropeEnd - ropeStart).normalized);
            ropePoints[i] = basePosition + perpendicular * (sag + wave);
        }

        lineRenderer.SetPositions(ropePoints);
    }

    private void SpawnHitEffect()
    {
        if (grappleHitEffect != null)
        {
            Instantiate(grappleHitEffect, grapplePoint, Quaternion.identity);
        }
    }
    #endregion

    #region Audio
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    #endregion

    #region Utility
    private bool IsGrounded()
    {
        // More comprehensive ground check
        return Physics2D.OverlapCircle(transform.position + Vector3.down * 0.6f, 0.4f, groundLayer);
    }

    // Public methods for external systems
    public void ForceReleaseGrapple()
    {
        ReleaseGrapple();
    }

    public bool CanGrapple()
    {
        return !isGrappling && !isHookTraveling && (allowGrappleWhileGrounded || !IsGrounded());
    }
    #endregion

    #region Debug
    private void OnDrawGizmosSelected()
    {
        // Draw grapple range
        Gizmos.color = Color.yellow;
        if (hookOrigin != null)
        {
            Gizmos.DrawWireSphere(hookOrigin.position, maxGrappleDistance);
        }

        // Draw current grapple line
        if (isGrappling)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, grapplePoint);
            Gizmos.DrawWireSphere(grapplePoint, 0.5f);
        }

        // Draw rope length limits
        if (isGrappling)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(grapplePoint, minRopeLength);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(grapplePoint, maxRopeLength);
        }
    }
    #endregion
}