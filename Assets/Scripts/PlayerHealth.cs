using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public static event Action OnPlayerDeath;

    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Optional Death Effect")]
    public GameObject deathEffectPrefab;

    [Header("End-Game Settings")]
    [Tooltip("Name must match exactly and be in Build Settings")]
    public string endGameSceneName = "EndGameScene";

    [Tooltip("Delay (seconds) before loading end-game")]
    public float deathDelay = 0.3f;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log($"Player took {amount} damage, health now {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            StartCoroutine(DieRoutine());
    }

    private IEnumerator DieRoutine()
    {
        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        OnPlayerDeath?.Invoke();
        Debug.Log("Player has died! Transitioning to end-game in " + deathDelay + "s");

        yield return new WaitForSeconds(deathDelay);

        // Final check: ensure scene name is valid
        if (Application.CanStreamedLevelBeLoaded(endGameSceneName))
        {
            SceneManager.LoadScene(endGameSceneName);
        }
        else
        {
            Debug.LogError($"Cannot load scene '{endGameSceneName}'. Check Build Settings and spelling.");
        }

        Destroy(gameObject);
    }
}
