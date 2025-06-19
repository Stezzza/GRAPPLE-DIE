using UnityEngine;
using TMPro;

public class DoubleJumpZone : MonoBehaviour
{
    public TextMeshProUGUI labelText;
    public string displayText = "Extra Jump";

    void Start()
    {
        if (labelText != null)
        {
            labelText.text = displayText;
            labelText.gameObject.SetActive(true);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement player = other.GetComponent<PlayerMovement>();
            if (player != null && !player.hasDoubleJump)
            {
                player.hasDoubleJump = true;
                labelText.text = "Double Jump Unlocked!";
                Destroy(gameObject); // pickup disappears
            }
        }
    }
}