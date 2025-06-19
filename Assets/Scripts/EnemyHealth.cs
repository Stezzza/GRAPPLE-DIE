using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("health settings")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("death settings")]
    public GameObject deathEffectPrefab;
    public float deathShakeDuration = 0.2f;
    public float deathShakeMagnitude = 0.1f;

    [Header("cleanup settings")]
    public float destroyDelay = 0f;

    // component refs
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
        Debug.Log("enemy took damage");

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        // shake camera
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(deathShakeDuration, deathShakeMagnitude);
        }

        // make death effect
        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        // disable ai
        if (ai != null)
            ai.enabled = false;

        // stop physics
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false;
        }
        if (col != null)
            col.enabled = false;

        // destroy object
        Destroy(gameObject, destroyDelay);
    }
}