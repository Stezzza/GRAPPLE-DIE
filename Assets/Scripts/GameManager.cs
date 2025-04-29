using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Tooltip("Assign the End Game UI panel (or leave null if using a separate scene).")]
    public GameObject endGameScreen;

    private bool isPaused;
    public bool IsPaused => isPaused;

    private void Awake()
    {
        // Enforce singleton pattern
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

    /// <summary>
    /// Pause or unpause the game by setting timeScale.
    /// </summary>
    public void SetPause(bool pause)
    {
        isPaused = pause;
        Time.timeScale = pause ? 0f : 1f;
    }

    /// <summary>
    /// Reloads the current scene.
    /// </summary>
    public void RestartLevel()
    {
        var currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }

    /// <summary>
    /// Invoked when the game is over.  
    /// If using in-scene UI, enables the endGameScreen panel;  
    /// otherwise, loads the EndGame scene.
    /// </summary>
    public void TriggerEndGame()
    {
        if (endGameScreen != null)
        {
            // Show an in-scene UI panel
            endGameScreen.SetActive(true);
            Time.timeScale = 0f;
        }
        else
        {
            // Fallback: load a dedicated end-game scene
            SceneManager.LoadScene("EndGameScene");
        }
    }

    /// <summary>
    /// Return to the main menu.  
    /// Make sure "MenuScene" is added in Build Settings.
    /// </summary>
    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MenuScene");
    }
}
