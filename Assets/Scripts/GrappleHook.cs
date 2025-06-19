using UnityEngine;
using System.Collections;

public class GrappleHook : MonoBehaviour
{
    [Header("grapple settings")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform hookOrigin;
    [SerializeField] private float maxGrappleDistance = 50f;
    [SerializeField] private LayerMask grappleLayer;
    [SerializeField] private float hookTravelSpeed = 100f;
    [SerializeField] private bool allowGrappleWhileGrounded = false;

    [Header("swing physics")]
    [SerializeField] private float swingForceMultiplier = 20f;
    [SerializeField] private float verticalClimbSpeed = 15f;
    [SerializeField] private float maxSwingSpeed = 30f;
    [SerializeField] private float airResistance = 0.95f;

    [Header("rope controls")]
    [SerializeField] private float ropeExtendSpeed = 10f;
    [SerializeField] private float ropeRetractSpeed = 15f;
    [SerializeField] private float minRopeLength = 2f;
    [SerializeField] private float maxRopeLength = 60f;
    [SerializeField] private bool autoAdjustRopeLength = true;
    [SerializeField] private LayerMask groundLayer;

    [Header("advanced physics")]
    [SerializeField] private float momentumConservation = 0.8f;
    [SerializeField] private float releaseBoostMultiplier = 1.2f;
    [SerializeField] private AnimationCurve swingForceCurve = AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1f);

    [Header("visual effects")]
    [SerializeField] private GameObject hookProjectile;
    [SerializeField] private GameObject grappleHitEffect;
    [SerializeField] private float ropeWaveAmplitude = 0.1f;
    [SerializeField] private float ropeWaveFrequency = 2f;
    [SerializeField] private int ropeSegments = 20;

    [Header("audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip grappleFireSound;
    [SerializeField] private AudioClip grappleHitSound;
    [SerializeField] private AudioClip grappleReleaseSound;

    // component refs
    private Rigidbody2D rb;
    private DistanceJoint2D ropeJoint;
    private Camera mainCam;
    private DashAttack dashAttack;

    // state
    private Vector2 grapplePoint;
    private Vector2 hookVelocity;
    private bool isGrappling = false;
    private bool isHookTraveling = false;
    private bool wasGrappling = false;
    private float currentRopeLength;
    private float targetRopeLength;
    private Vector2 releaseVelocity;

    // input
    private float horizontalInput;
    private float verticalInput;
    private bool grappleInputHeld;
    private bool grappleInputPressed;
    private bool grappleInputReleased;

    // rope visuals
    private Vector3[] ropePoints;

    public bool IsGrappling => isGrappling;
    public bool IsHookTraveling => isHookTraveling;
    public float RopeLength => currentRopeLength;
    public Vector2 GrapplePoint => grapplePoint;

    // setup
    private void Awake()
    {
        CacheComponents();
        InitializeRope();
    }

    // main loop
    private void Update()
    {
        HandleInput();
        HandleGrappleLogic();
        UpdateVisuals();
    }

    // physics loop
    private void FixedUpdate()
    {
        if (isGrappling)
        {
            ApplySwingPhysics();
            HandleRopeLength();
            ApplyAirResistance();
        }
    }

    // runs after update
    private void LateUpdate()
    {
        if (wasGrappling && !isGrappling)
        {
            ApplyReleaseBoost();
        }
        wasGrappling = isGrappling;
    }

    // get components
    private void CacheComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;
        dashAttack = GetComponent<DashAttack>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;
    }

    // rope setup
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

    // check for player input
    private void HandleInput()
    {
        grappleInputPressed = Input.GetMouseButtonDown(1);
        grappleInputHeld = Input.GetMouseButton(1);
        grappleInputReleased = Input.GetMouseButtonUp(1);

        float rawHorizontal = Input.GetAxisRaw("Horizontal");
        float rawVertical = Input.GetAxisRaw("Vertical");

        horizontalInput = Mathf.Abs(rawHorizontal) > 0.1f ? rawHorizontal : 0f;
        verticalInput = Mathf.Abs(rawVertical) > 0.1f ? rawVertical : 0f;
    }

    // grapple logic
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

    // try to shoot hook
    private void TryFireGrapple()
    {
        if (!allowGrappleWhileGrounded && IsGrounded())
            return;

        Vector2 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 grappleDirection = (mousePos - (Vector2)hookOrigin.position).normalized;

        StartCoroutine(FireHook(grappleDirection));
        PlaySound(grappleFireSound);
    }

    // hook flying through air
    private IEnumerator FireHook(Vector2 direction)
    {
        isHookTraveling = true;
        Vector2 hookPosition = hookOrigin.position;
        hookVelocity = direction * hookTravelSpeed;

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

            RaycastHit2D hit = Physics2D.Raycast(hookPosition, hookVelocity.normalized, segmentDistance, grappleLayer);

            if (hit.collider != null)
            {
                // hit something
                AttachGrapple(hit.point);
                if (hook != null) Destroy(hook);
                break;
            }

            hookPosition = newPosition;
            travelDistance += segmentDistance;

            if (hook != null)
                hook.transform.position = hookPosition;

            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, hookOrigin.position);
                lineRenderer.SetPosition(1, hookPosition);
            }

            yield return null;
        }

        if (isHookTraveling)
        {
            isHookTraveling = false;
            if (hook != null) Destroy(hook);
            if (lineRenderer != null) lineRenderer.positionCount = 0;
        }
    }

    // attach to a point
    private void AttachGrapple(Vector2 hitPoint)
    {
        grapplePoint = hitPoint;
        isHookTraveling = false;
        isGrappling = true;

        currentRopeLength = Vector2.Distance(transform.position, grapplePoint);
        targetRopeLength = currentRopeLength;

        ropeJoint = gameObject.AddComponent<DistanceJoint2D>();
        ropeJoint.autoConfigureDistance = false;
        ropeJoint.connectedAnchor = grapplePoint;
        ropeJoint.distance = currentRopeLength;
        ropeJoint.enableCollision = false;
        ropeJoint.maxDistanceOnly = true;

        SpawnHitEffect();
        PlaySound(grappleHitSound);

        if (dashAttack != null)
            dashAttack.GrappleSuccess();
    }

    // let go of grapple
    private void ReleaseGrapple()
    {
        if (ropeJoint != null)
        {
            Destroy(ropeJoint);
        }

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

    // applies swing force
    private void ApplySwingPhysics()
    {
        Vector2 playerToGrapple = grapplePoint - (Vector2)transform.position;
        Vector2 swingDirection = Vector2.Perpendicular(playerToGrapple).normalized;

        if (transform.position.x > grapplePoint.x)
            swingDirection *= -1f;

        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            float swingIntensity = swingForceCurve.Evaluate(Mathf.Abs(Vector2.Dot(rb.velocity.normalized, swingDirection)));
            Vector2 swingForce = swingDirection * horizontalInput * swingForceMultiplier * swingIntensity;
            rb.AddForce(swingForce);
        }

        if (Mathf.Abs(verticalInput) > 0.1f)
        {
            Vector2 climbDirection = playerToGrapple.normalized;
            Vector2 climbForce = climbDirection * verticalInput * verticalClimbSpeed;
            rb.AddForce(climbForce);
        }

        if (rb.velocity.magnitude > maxSwingSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSwingSpeed;
        }
    }

    // changes rope length with w/s
    private void HandleRopeLength()
    {
        if (Mathf.Abs(verticalInput) > 0.1f)
        {
            if (verticalInput > 0)
            {
                targetRopeLength -= ropeRetractSpeed * Time.fixedDeltaTime;
            }
            else
            {
                RaycastHit2D groundHit = Physics2D.Raycast(transform.position, Vector2.down, 1.5f, groundLayer);
                if (groundHit.collider == null)
                {
                    targetRopeLength += ropeExtendSpeed * Time.fixedDeltaTime;
                }
            }
            targetRopeLength = Mathf.Clamp(targetRopeLength, minRopeLength, maxRopeLength);
        }
        else if (autoAdjustRopeLength)
        {
            float velocityFactor = rb.velocity.magnitude / maxSwingSpeed;
            float desiredLength = Mathf.Lerp(currentRopeLength, targetRopeLength, velocityFactor * 0.1f);
            targetRopeLength = desiredLength;
        }

        currentRopeLength = Mathf.Lerp(currentRopeLength, targetRopeLength, Time.fixedDeltaTime * 5f);

        if (ropeJoint != null)
        {
            ropeJoint.distance = currentRopeLength;
        }
    }

    // slows down player in air
    private void ApplyAirResistance()
    {
        rb.velocity *= airResistance;
    }

    // boost when releasing grapple
    private void ApplyReleaseBoost()
    {
        if (releaseVelocity.magnitude > 5f)
        {
            Vector2 boostVelocity = releaseVelocity * (releaseBoostMultiplier * momentumConservation);
            rb.velocity = boostVelocity;
        }
    }

    // draw rope visuals
    private void UpdateVisuals()
    {
        if (isGrappling && lineRenderer != null)
        {
            UpdateRopeVisual();
        }
    }

    // make rope look wavy
    private void UpdateRopeVisual()
    {
        lineRenderer.positionCount = ropeSegments + 1;
        Vector2 ropeStart = hookOrigin.position;
        Vector2 ropeEnd = grapplePoint;

        for (int i = 0; i <= ropeSegments; i++)
        {
            float t = (float)i / ropeSegments;
            Vector2 basePosition = Vector2.Lerp(ropeStart, ropeEnd, t);

            float sag = Mathf.Sin(t * Mathf.PI) * ropeWaveAmplitude * currentRopeLength * 0.1f;
            float wave = Mathf.Sin(Time.time * ropeWaveFrequency + t * Mathf.PI * 2) * ropeWaveAmplitude * 0.5f;
            Vector2 perpendicular = Vector2.Perpendicular((ropeEnd - ropeStart).normalized);
            ropePoints[i] = basePosition + perpendicular * (sag + wave);
        }
        lineRenderer.SetPositions(ropePoints);
    }

    // makes hit effect
    private void SpawnHitEffect()
    {
        if (grappleHitEffect != null)
        {
            Instantiate(grappleHitEffect, grapplePoint, Quaternion.identity);
        }
    }

    // play sound
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // check if on ground
    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(transform.position + Vector3.down * 0.6f, 0.4f, groundLayer);
    }

    // public function to force release
    public void ForceReleaseGrapple()
    {
        ReleaseGrapple();
    }

    // public function to check if can grapple
    public bool CanGrapple()
    {
        return !isGrappling && !isHookTraveling && (allowGrappleWhileGrounded || !IsGrounded());
    }

    // draw gizmos in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (hookOrigin != null)
        {
            Gizmos.DrawWireSphere(hookOrigin.position, maxGrappleDistance);
        }
    }
}