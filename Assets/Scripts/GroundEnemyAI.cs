using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class GroundEnemyAI : MonoBehaviour
{
    [Header("Patrol Settings")]
    public float leftBoundary = 1f;
    public float rightBoundary = 3.5f;
    public float patrolSpeed = 2f;

    [Header("Chase Settings")]
    public Transform player;
    public float chaseSpeed = 3.5f;
    public float detectionRadius = 5f;

    [Header("Sensors")]
    public Vector2 groundSensorOffset = new Vector2(0.5f, -0.5f);
    public float groundSensorRadius = 0.1f;
    public float wallSensorDistance = 0.1f;
    public LayerMask groundLayer;

    // Internals
    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private bool facingRight = true;
    private bool isChasing = false;
    private bool playerAlive = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        // Listen for the player's death event
        PlayerHealth.OnPlayerDeath += HandlePlayerDeath;
    }

    void OnDisable()
    {
        PlayerHealth.OnPlayerDeath -= HandlePlayerDeath;
    }

    private void HandlePlayerDeath()
    {
        // Mark the player as dead so we stop chasing and stop referencing player.transform
        playerAlive = false;
    }

    void FixedUpdate()
    {
        // 1. If player is dead, immediately halt all horizontal movement.
        if (!playerAlive)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            return;
        }

        // 2. Determine whether we should chase (and only if player reference still exists).
        if (player != null &&
            Vector2.Distance(transform.position, player.position) <= detectionRadius)
        {
            isChasing = true;
        }
        else
        {
            isChasing = false;
        }

        // 3. Compute desired horizontal direction: +1 right, -1 left, or 0 to stop.
        int dir;
        if (isChasing)
        {
            dir = (player.position.x > transform.position.x) ? 1 : -1;
        }
        else
        {
            dir = facingRight ? 1 : -1;
        }

        // 4. Sensor positions
        Vector2 groundSensorPos = (Vector2)transform.position +
                                  new Vector2(groundSensorOffset.x * dir,
                                              groundSensorOffset.y);
        Vector2 wallSensorOrigin = transform.position;

        // 5. Perform ground‐and‐wall checks
        bool groundAhead = Physics2D.OverlapCircle(
            groundSensorPos, groundSensorRadius, groundLayer
        );
        bool wallAhead = Physics2D.Raycast(
            wallSensorOrigin,
            Vector2.right * dir,
            wallSensorDistance,
            groundLayer
        );

        // 6. React to edges or walls
        if (!groundAhead || wallAhead)
        {
            if (isChasing)
            {
                // If chasing and we hit an edge or wall, stop rather than run off.
                dir = 0;
            }
            else
            {
                // If patrolling, flip direction and recalc dir
                facingRight = !facingRight;
                dir = facingRight ? 1 : -1;
            }
        }

        // 7. Choose speed based on mode
        float speed = isChasing ? chaseSpeed : patrolSpeed;

        // 8. Apply new velocity (preserving any vertical velocity for gravity)
        rb.velocity = new Vector2(dir * speed, rb.velocity.y);

        // 9. Flip sprite when direction changes
        if (dir > 0 && !facingRight) Flip();
        else if (dir < 0 && facingRight) Flip();
    }

    private void Flip()
    {
        facingRight = !facingRight;
        sprite.flipX = !sprite.flipX;
    }

    void OnDrawGizmosSelected()
    {
        // Visualise detection radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Visualise ground sensor
        Gizmos.color = Color.yellow;
        Vector3 gs = transform.position + (Vector3)groundSensorOffset * (facingRight ? 1 : -1);
        Gizmos.DrawWireSphere(gs, groundSensorRadius);

        // Visualise wall sensor
        Gizmos.color = Color.cyan;
        Vector3 wsDir = Vector3.right * (facingRight ? 1 : -1);
        Gizmos.DrawLine(transform.position, transform.position + wsDir * wallSensorDistance);
    }
}
