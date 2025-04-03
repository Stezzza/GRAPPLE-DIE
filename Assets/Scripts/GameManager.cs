using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameObject endGameScreen;

    private bool isPaused;
    public bool IsPaused => isPaused;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetPause(bool value)
    {
        isPaused = value;
        Time.timeScale = value ? 0f : 1f;
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void TriggerEndGame()
    {
        if (endGameScreen != null)
        {
            endGameScreen.SetActive(true);
            SetPause(true);
        }
        else
        {
            Debug.LogError("No EndGameScreen assigned to GameManager!");
        }
    }

    // New method to load the menu scene when quitting
    public void QuitToMenu()
    {
        // Optionally, reset time scale in case the game is paused.
        Time.timeScale = 1f;
        SceneManager.LoadScene("MenuScene"); // Ensure "MenuScene" is added to Build Settings
    }
}
