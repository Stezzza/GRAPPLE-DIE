using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class GrappleHook : MonoBehaviour
{
    [Header("Grapple Settings")]
    public LineRenderer lineRenderer;         // Assigned via the GrappleOrigin GameObject
    public Transform hookOrigin;              // The origin point for the grapple (child of the Player)
    public float maxDistance = 20f;           // Maximum grapple distance
    public LayerMask grappleableLayer;        // Only these layers can be grappled

    [Header("Grapple Control")]
    public float horizontalControlForce = 10f; // Force applied for left/right input while grappled
    public bool useSpringJoint = true;        // Toggle to switch between SpringJoint2D and DistanceJoint2D

    // Spring Joint Parameters (only relevant if useSpringJoint == true)
    [Range(0f, 10f)]
    public float springFrequency = 2f;        // Higher = stiffer spring
    [Range(0f, 1f)]
    public float springDampingRatio = 0.2f;   // Higher = more damping
    public float springDistanceBuffer = 0.5f; // How "relaxed" the rope is at the start

    private Vector2 grapplePoint;
    private Joint2D activeJoint;              // Could be a SpringJoint2D or DistanceJoint2D
    private Transform playerTransform;
    private Rigidbody2D rb;
    private bool isGrappled = false;

    void Start()
    {
        // Assume hookOrigin is a child of the Player
        playerTransform = hookOrigin.parent;
        rb = playerTransform.GetComponent<Rigidbody2D>();

        // Improve collision reliability and stability
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        // Ensure the LineRenderer is properly initialized
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        // By default, hide the rope
        lineRenderer.positionCount = 0;
    }

    void Update()
    {
        // Right-click initiates grapple; release on button up
        if (Input.GetMouseButtonDown(1))
        {
            StartGrapple();
        }
        if (Input.GetMouseButtonUp(1))
        {
            EndGrapple();
        }

        // While grappled, apply horizontal control force
        if (isGrappled && activeJoint != null)
        {
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            rb.AddForce(new Vector2(horizontalInput * horizontalControlForce, 0), ForceMode2D.Force);
        }
    }

    void StartGrapple()
    {
        // Convert mouse position to a world point and determine the direction
        Vector2 targetPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = targetPos - (Vector2)hookOrigin.position;

        // Initial raycast to find a grappleable surface
        RaycastHit2D hit = Physics2D.Raycast(hookOrigin.position, direction, maxDistance, grappleableLayer);
        if (hit.collider != null)
        {
            Vector2 hitPoint = hit.point;

            // --- Additional check: ensure no floor blocks the line of sight ---
            // This second raycast checks from the player to the hit point to confirm nothing else
            // (like a floor or an obstacle) is in between.
            float distanceToHit = Vector2.Distance(playerTransform.position, hitPoint);
            RaycastHit2D floorCheck = Physics2D.Raycast(
                playerTransform.position,
                hitPoint - (Vector2)playerTransform.position,
                distanceToHit,
                grappleableLayer
            );

            // If the second raycast hits the *same* collider, we have clear line of sight.
            if (floorCheck.collider != null && floorCheck.collider == hit.collider)
            {
                grapplePoint = hitPoint;
                CreateJoint();
                // Enable the visual rope
                lineRenderer.positionCount = 2;
                isGrappled = true;
            }
        }
    }

    void CreateJoint()
    {
        // Clean up any existing joint before creating a new one
        if (activeJoint != null)
        {
            Destroy(activeJoint);
        }

        float currentDistance = Vector2.Distance(playerTransform.position, grapplePoint);

        if (useSpringJoint)
        {
            // Create a SpringJoint2D for more dynamic swinging
            SpringJoint2D springJoint = playerTransform.gameObject.AddComponent<SpringJoint2D>();
            springJoint.connectedAnchor = grapplePoint;

            // The distance at which the spring is “at rest”
            // Add a small buffer so you can move closer than initial distance
            springJoint.distance = currentDistance - springDistanceBuffer;
            if (springJoint.distance < 0f) springJoint.distance = 0f;

            // Stiffness & damping of the spring
            springJoint.frequency = springFrequency;
            springJoint.dampingRatio = springDampingRatio;

            // Allow collisions so rope can’t pass through colliders
            springJoint.enableCollision = true;

            activeJoint = springJoint;
        }
        else
        {
            // If you prefer the simpler DistanceJoint2D approach:
            DistanceJoint2D distanceJoint = playerTransform.gameObject.AddComponent<DistanceJoint2D>();
            distanceJoint.connectedAnchor = grapplePoint;
            distanceJoint.autoConfigureDistance = false;

            // Use maxDistanceOnly so the rope is only a maximum length;
            // the player can still move closer (prevents “standing” on the rope).
            distanceJoint.distance = currentDistance;
            distanceJoint.maxDistanceOnly = true;
            distanceJoint.enableCollision = true;

            activeJoint = distanceJoint;
        }
    }

    void EndGrapple()
    {
        // Clean up the joint and visual rope when releasing the grapple
        if (activeJoint != null)
        {
            Destroy(activeJoint);
            activeJoint = null;
        }
        lineRenderer.positionCount = 0;
        isGrappled = false;
    }

    void LateUpdate()
    {
        // Update the rope line to draw from the hook origin to the grapple point
        if (isGrappled && activeJoint != null)
        {
            lineRenderer.SetPosition(0, hookOrigin.position);
            lineRenderer.SetPosition(1, grapplePoint);
        }
    }
}
