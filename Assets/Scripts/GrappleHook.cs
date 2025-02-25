using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class GrappleHook : MonoBehaviour
{
    [Header("Grapple Settings")]
    public LineRenderer lineRenderer;
    public Transform hookOrigin;
    public float maxDistance = 20f;
    public LayerMask grappleableLayer;
    public float hookTravelSpeed = 30f;

    [Header("Grapple Control")]
    public float pullSpeed = 25f;
    public float minPullDistance = 0.5f;
    public float swingForce = 15f;
    public float swingDamping = 0.95f;
    public float maxSwingVelocity = 20f;

    [Header("Visuals & Effects")]
    public GameObject hookPrefab;
    public float hookSize = 0.2f;

    private Vector2 grapplePoint;
    private Transform playerTransform;
    private Rigidbody2D rb;
    private bool isGrappled = false;
    private bool isHookTraveling = false;
    private Vector2 hookPosition;
    private GameObject hookInstance;
    private Vector2 pendulumPivot;
    private Vector2 hookDirection;
    private float hookTravelTime;

    void Start()
    {
        playerTransform = hookOrigin.parent;
        rb = playerTransform.GetComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        if (!lineRenderer) lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1) && !isGrappled && !isHookTraveling)
        {
            FireGrapple();
        }
        if (Input.GetMouseButtonUp(1))
        {
            EndGrapple();
        }

        if (isHookTraveling)
        {
            UpdateHookTravel();
        }
        else if (isGrappled)
        {
            ApplySwingPhysics();
            PullToGrapplePoint();
        }
    }

    void LateUpdate()
    {
        if (isHookTraveling || isGrappled)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, hookOrigin.position);
            lineRenderer.SetPosition(1, isHookTraveling ? hookPosition : grapplePoint);
        }
        else
        {
            lineRenderer.positionCount = 0;
        }

        if (hookInstance) hookInstance.transform.position = hookPosition;
    }

    void FireGrapple()
    {
        Vector2 targetPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        hookDirection = (targetPos - (Vector2)hookOrigin.position).normalized;

        RaycastHit2D hit = Physics2D.Raycast(hookOrigin.position, hookDirection, maxDistance, grappleableLayer);
        if (hit.collider != null)
        {
            // Check if there is a clear line of sight from the player to the grapple point
            Vector2 hitPoint = hit.point;
            if (Physics2D.Linecast(playerTransform.position, hitPoint, grappleableLayer).collider == hit.collider)
            {
                grapplePoint = hitPoint;
                pendulumPivot = grapplePoint;
                hookPosition = hookOrigin.position;
                isHookTraveling = true;
                hookTravelTime = Vector2.Distance(hookOrigin.position, grapplePoint) / hookTravelSpeed;

                if (hookPrefab)
                {
                    hookInstance = Instantiate(hookPrefab, hookPosition, Quaternion.identity);
                    hookInstance.transform.localScale = Vector3.one * hookSize;
                }
            }
        }
    }

    void UpdateHookTravel()
    {
        hookPosition = Vector2.Lerp(hookPosition, grapplePoint, Time.deltaTime / hookTravelTime);
        if (Vector2.Distance(hookPosition, grapplePoint) < 0.2f)
        {
            isHookTraveling = false;
            isGrappled = true;
            rb.velocity = Vector2.zero;

            if (hookInstance) Destroy(hookInstance);
        }
    }

    void ApplySwingPhysics()
    {
        Vector2 toPivot = (Vector2)playerTransform.position - pendulumPivot;
        float distanceToPivot = toPivot.magnitude;
        Vector2 directionToPivot = toPivot.normalized;

        float horizontalInput = Input.GetAxisRaw("Horizontal");
        Vector2 swingDirection = Vector2.Perpendicular(directionToPivot) * -horizontalInput;
        rb.AddForce(swingDirection * swingForce, ForceMode2D.Force);

        Vector2 gravityForce = -directionToPivot * (Physics2D.gravity.magnitude * 2f);
        rb.AddForce(gravityForce, ForceMode2D.Force);

        rb.velocity *= swingDamping;
        rb.velocity = Vector2.ClampMagnitude(rb.velocity, maxSwingVelocity);
    }

    void PullToGrapplePoint()
    {
        float distanceToPoint = Vector2.Distance(playerTransform.position, grapplePoint);
        if (distanceToPoint > minPullDistance)
        {
            Vector2 pullDirection = (grapplePoint - (Vector2)playerTransform.position).normalized;
            rb.velocity = Vector2.Lerp(rb.velocity, pullDirection * pullSpeed, Time.deltaTime * 10f);
        }
        else
        {
            rb.velocity *= 0.8f;
        }
    }

    void EndGrapple()
    {
        if (isHookTraveling && hookInstance) Destroy(hookInstance);
        lineRenderer.positionCount = 0;
        isGrappled = false;
        isHookTraveling = false;
    }

    void OnDrawGizmos()
    {
        if (isGrappled)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(grapplePoint, 0.1f);
        }
    }
}
