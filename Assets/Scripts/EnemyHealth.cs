using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Optional Death Effect")]
    public GameObject deathEffectPrefab;

    [Header("Cleanup Settings")]
    [Tooltip("How long to keep the dead body before destroying the GameObject.")]
    public float destroyDelay = 0f;

    // Cached components
    private GroundEnemyAI ai;
    private Rigidbody2D rb;
    private Collider2D col;

    void Start()
    {
        currentHealth = maxHealth;
        ai = GetComponent<GroundEnemyAI>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log($"{gameObject.name} took {amount} damage; health now {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        // 1) Spawn death effect
        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        // 2) Disable AI so no more movement or logic runs
        if (ai != null)
            ai.enabled = false;

        // 3) Kill physics: stop velocity and optionally disable collisions
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false;
        }
        if (col != null)
            col.enabled = false;

        // 4) Finally, destroy the GameObject (immediate or after delay)
        Destroy(gameObject, destroyDelay);
    }
}
