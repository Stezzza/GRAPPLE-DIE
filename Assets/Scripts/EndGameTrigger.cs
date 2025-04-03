using UnityEngine;

public class EndGameTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("OnTriggerEnter2D triggered by: " + other.gameObject.name);

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player detected - ending game");
            GameManager.Instance.TriggerEndGame();
        }
    }
}
