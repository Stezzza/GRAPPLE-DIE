using UnityEngine;

public class GroundEnemyAI : MonoBehaviour
{
    [Header("Patrol Points")]
    // The left and right boundaries for patrol; set x-values to 1 and 3.5 respectively.
    public Vector2 pointA = new Vector2(1f, 0f);
    public Vector2 pointB = new Vector2(3.5f, 0f);

    [Header("Movement Settings")]
    public float moveSpeed = 2f;

    private Vector2 target;   // Current patrol target
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Set the y-values of both patrol points to the enemy's current y-position.
        float currentY = transform.position.y;
        pointA.y = currentY;
        pointB.y = currentY;

        // Start by moving towards pointB.
        target = pointB;
    }

    void Update()
    {
        // Move the enemy smoothly toward the current target.
        Vector2 newPosition = Vector2.MoveTowards(rb.position, target, moveSpeed * Time.deltaTime);
        rb.MovePosition(newPosition);

        // Flip sprite based on direction (if you later swap the red box for a more directional sprite).
        if (target.x > rb.position.x)
            spriteRenderer.flipX = false;
        else if (target.x < rb.position.x)
            spriteRenderer.flipX = true;

        // If the enemy reaches the target, switch to the opposite point.
        if (Vector2.Distance(rb.position, target) < 0.1f)
        {
            target = (target == pointA) ? pointB : pointA;
        }
    }
}
