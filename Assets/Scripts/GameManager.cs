using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameObject endGameScreen;
    private bool isPaused;
    public bool IsPaused => isPaused;

    private void Awake()
    {
        // only one gamemanager
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

    // pause or unpause game
    public void SetPause(bool pause)
    {
        isPaused = pause;
        Time.timeScale = pause ? 0f : 1f;
    }

    // reloads the level
    public void RestartLevel()
    {
        var currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }

    // ends the game
    public void TriggerEndGame()
    {
        if (endGameScreen != null)
        {
            // shows the ui panel
            endGameScreen.SetActive(true);
            Time.timeScale = 0f;
        }
        else
        {
            // loads end scene
            SceneManager.LoadScene("EndGameScene");
        }
    }

    // go to main menu
    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MenuScene");
    }
}