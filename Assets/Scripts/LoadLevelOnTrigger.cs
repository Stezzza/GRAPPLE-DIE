using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadLevelOnTrigger : MonoBehaviour
{
    // Make sure the collider is marked as "Is Trigger"
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) // Assumes your player GameObject has the tag "Player"
        {
            SceneManager.LoadScene("level 1");
        }
    }
}
