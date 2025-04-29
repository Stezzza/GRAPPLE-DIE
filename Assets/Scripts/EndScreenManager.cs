using UnityEngine;
using UnityEngine.SceneManagement;

public class EndScreenManager : MonoBehaviour
{
    public void RestartGame()
    {
        SceneManager.LoadScene("level");
    }

    // This function can be attached to the Main Menu button
    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainScene");
    }
}