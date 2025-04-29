using UnityEngine;

public class EndGameTrigger : MonoBehaviour
{
    [Tooltip("Tag of the incoming object that should end the game.")]
    [SerializeField]
    private string triggerTag = "Player";

    private void Reset()
    {
        // Automatically add and configure a 2D BoxCollider if missing
        var col = GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only react when the specified tagged object enters
        if (other.CompareTag(triggerTag))
        {
            Debug.Log($"EndGameTrigger: '{other.name}' touched the end zone.");
            GameManager.Instance.TriggerEndGame();
        }
    }
}
