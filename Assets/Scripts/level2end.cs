using UnityEngine;
using UnityEngine.SceneManagement;

public class level2end : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) // check if it's the player
        {
            SceneManager.LoadScene("EndGameScene");
        }
    }
}