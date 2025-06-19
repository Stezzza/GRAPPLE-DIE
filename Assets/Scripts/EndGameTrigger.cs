using UnityEngine;

public class EndGameTrigger : MonoBehaviour
{
    [SerializeField]
    private string triggerTag = "Player";

    private void Reset()
    {
        // auto adds a trigger collider
        var col = GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // check for player tag
        if (other.CompareTag(triggerTag))
        {
            Debug.Log("player hit the end zone");
            GameManager.Instance.TriggerEndGame();
        }
    }
}