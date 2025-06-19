using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class GroundEnemyAI : MonoBehaviour
{
    [Header("patrol settings")]
    public float leftBoundary = 1f;
    public float rightBoundary = 3.5f;
    public float patrolSpeed = 2f;

    [Header("chase settings")]
    public Transform player;
    public float chaseSpeed = 3.5f;
    public float detectionRadius = 5f;

    [Header("sensors")]
    public Vector2 groundSensorOffset = new Vector2(0.5f, -0.5f);
    public float groundSensorRadius = 0.1f;
    public float wallSensorDistance = 0.1f;
    public LayerMask groundLayer;

    // internal vars
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
        // listen for player death
        PlayerHealth.OnPlayerDeath += HandlePlayerDeath;
    }

    void OnDisable()
    {
        PlayerHealth.OnPlayerDeath -= HandlePlayerDeath;
    }

    private void HandlePlayerDeath()
    {
        // stop chasing when player dies
        playerAlive = false;
    }

    void FixedUpdate()
    {
        // stop if player dead
        if (!playerAlive)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            return;
        }

        // check if should chase
        if (player != null && Vector2.Distance(transform.position, player.position) <= detectionRadius)
        {
            isChasing = true;
        }
        else
        {
            isChasing = false;
        }

        // get move direction
        int dir;
        if (isChasing)
        {
            dir = (player.position.x > transform.position.x) ? 1 : -1;
        }
        else
        {
            dir = facingRight ? 1 : -1;
        }

        // sensor positions
        Vector2 groundSensorPos = (Vector2)transform.position + new Vector2(groundSensorOffset.x * dir, groundSensorOffset.y);
        Vector2 wallSensorOrigin = transform.position;

        // check sensors
        bool groundAhead = Physics2D.OverlapCircle(groundSensorPos, groundSensorRadius, groundLayer);
        bool wallAhead = Physics2D.Raycast(wallSensorOrigin, Vector2.right * dir, wallSensorDistance, groundLayer);

        // what to do at edge or wall
        if (!groundAhead || wallAhead)
        {
            if (isChasing)
            {
                // stop if chasing
                dir = 0;
            }
            else
            {
                // flip if patrolling
                facingRight = !facingRight;
                dir = facingRight ? 1 : -1;
            }
        }

        // choose speed
        float speed = isChasing ? chaseSpeed : patrolSpeed;

        // move
        rb.velocity = new Vector2(dir * speed, rb.velocity.y);

        // flip sprite
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
        // draw gizmos for editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}