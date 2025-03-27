using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;

    // Particle effect prefab to play on enemy death.
    public GameObject deathEffectPrefab;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        Debug.Log($"{gameObject.name} took {amount} damage!");
        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Instantiate death effect at the enemy's position if assigned.
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        }

        // Destroy the enemy after playing the death effect.
        Destroy(gameObject);
    }
}
