using UnityEngine;
using UnityEngine.SceneManagement;

public class EndScreenManager : MonoBehaviour
{
    public void RestartGame()
    {
        SceneManager.LoadScene("level");
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainScene");
    }
}