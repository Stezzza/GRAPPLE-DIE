using UnityEngine;
using System.Collections;

public class DashAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.15f;
    public float damageRadius = 0.5f;
    public int damageAmount = 50;
    public LayerMask enemyLayer;

    [Header("Dash Effect")]
    public TrailRenderer dashTrail;

    private Rigidbody2D rb;
    private Camera mainCam;
    private Animator animator;
    private bool canDash = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;
        animator = GetComponent<Animator>();
        dashTrail.emitting = false;
        dashTrail.time = 0.05f;
        dashTrail.startWidth = 0.1f;
        dashTrail.endWidth = 0f;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && canDash)
        {
            Vector2 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dashDirection = (mousePos - (Vector2)transform.position).normalized;
            HandleFacingDirection(dashDirection);
            StartCoroutine(PerformDash(dashDirection));
        }

        if (Input.GetMouseButtonDown(1))
        {
            ResetDash();
        }
    }

    private IEnumerator PerformDash(Vector2 direction)
    {
        canDash = false;
        dashTrail.emitting = true;
        animator.SetTrigger("attack1");

        float elapsedTime = 0f;

        while (elapsedTime < dashDuration)
        {
            rb.velocity = direction * dashSpeed;

            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, damageRadius, enemyLayer);
            foreach (Collider2D enemy in hitEnemies)
            {
                // enemy.GetComponent<EnemyHealth>().TakeDamage(damageAmount);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rb.velocity = Vector2.zero;
        dashTrail.emitting = false;
    }

    private void HandleFacingDirection(Vector2 direction)
    {
        Vector3 localScale = transform.localScale;

        if (direction.x > 0)
            localScale.x = Mathf.Abs(localScale.x);
        else if (direction.x < 0)
            localScale.x = -Mathf.Abs(localScale.x);

        transform.localScale = localScale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
        {
            ResetDash();
        }
    }

    public void FinishAttackAnimation()
    {
        animator.Play("Idle");
    }

    public void ResetDash()
    {
        canDash = true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}
