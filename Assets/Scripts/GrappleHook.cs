using UnityEngine;

public class GrappleHook : MonoBehaviour
{
    [Header("Grapple Settings")]
    public LineRenderer lineRenderer;
    public Transform hookOrigin;
    public float maxGrappleDistance = 25f;
    public LayerMask grappleLayer;

    [Header("Swing Physics")]
    public float ropeElasticity = 4f;
    public float ropeDamping = 0.6f;
    public float swingForceMultiplier = 20f;
    public float retractSpeed = 5f;
    public float grapplePullForce = 50f;

    private Rigidbody2D rb;
    private DistanceJoint2D ropeJoint;
    private Vector2 grapplePoint;
    private bool isGrappling = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;
    }

    void Update()
    {
        HandleInput();
        HandleGrappleVisual();
    }

    void FixedUpdate()
    {
        if (isGrappling)
        {
            PullTowardsGrapple();
            HandleSwingMovement();
        }
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(1))
            TryGrapple();

        if (Input.GetMouseButtonUp(1))
            ReleaseGrapple();
    }

    void TryGrapple()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 grappleDir = mousePos - (Vector2)hookOrigin.position;

        RaycastHit2D hit = Physics2D.Raycast(hookOrigin.position, grappleDir.normalized, maxGrappleDistance, grappleLayer);

        if (hit.collider != null)
        {
            grapplePoint = hit.point;

            ropeJoint = gameObject.AddComponent<DistanceJoint2D>();
            ropeJoint.autoConfigureDistance = false;
            ropeJoint.connectedAnchor = grapplePoint;
            ropeJoint.distance = Vector2.Distance(transform.position, grapplePoint);
            ropeJoint.enableCollision = true;
            ropeJoint.maxDistanceOnly = true;

            isGrappling = true;
        }
    }

    void ReleaseGrapple()
    {
        if (ropeJoint != null)
            Destroy(ropeJoint);

        isGrappling = false;
        lineRenderer.positionCount = 0;
    }

    void PullTowardsGrapple()
    {
        Vector2 pullDirection = (grapplePoint - (Vector2)transform.position).normalized;
        rb.AddForce(pullDirection * grapplePullForce);

        ropeJoint.distance = Mathf.Max(ropeJoint.distance - retractSpeed * Time.fixedDeltaTime, 1f);
    }

    void HandleSwingMovement()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        Vector2 swingDir = Vector2.Perpendicular(grapplePoint - (Vector2)transform.position).normalized;

        rb.AddForce(swingDir * horizontalInput * swingForceMultiplier);
    }

    void HandleGrappleVisual()
    {
        if (isGrappling)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, hookOrigin.position);
            lineRenderer.SetPosition(1, grapplePoint);
        }
    }
}
