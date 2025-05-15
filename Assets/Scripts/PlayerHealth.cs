using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    // Fired when the player dies, before the GameObject is destroyed.
    public static event Action OnPlayerDeath;

    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Optional Death Effect")]
    public GameObject deathEffectPrefab;

    void Start()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Inflicts damage on the player. If health falls to zero or below, triggers Die().
    /// </summary>
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log($"Player took {amount} damage, health now {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            Die();
    }

    /// <summary>
    /// Plays any death effect, invokes the OnPlayerDeath event, then destroys the GameObject.
    /// </summary>
    private void Die()
    {
        // Play a death effect if one is assigned.
        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        // Notify all subscribers (e.g. the camera) that the player is about to be destroyed.
        OnPlayerDeath?.Invoke();

        Debug.Log("Player has died!");
        Destroy(gameObject);
    }
}
