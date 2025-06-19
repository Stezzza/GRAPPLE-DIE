using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public static event Action OnPlayerDeath;

    [Header("health settings")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("death effect")]
    public GameObject deathEffectPrefab;

    [Header("end game settings")]
    public string endGameSceneName = "EndGameScene";
    public float deathDelay = 0.3f;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log("player took damage");

        if (currentHealth <= 0)
            StartCoroutine(DieRoutine());
    }

    private IEnumerator DieRoutine()
    {
        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        OnPlayerDeath?.Invoke(); // tell other scripts the player died
        Debug.Log("player died");

        yield return new WaitForSeconds(deathDelay);

        // load end scene
        if (Application.CanStreamedLevelBeLoaded(endGameSceneName))
        {
            SceneManager.LoadScene(endGameSceneName);
        }
        else
        {
            Debug.LogError("cannot load scene " + endGameSceneName);
        }

        Destroy(gameObject);
    }
}