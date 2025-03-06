using UnityEngine;

public class UniqueCameraFollow : MonoBehaviour
{
    [Header("Camera Follow Settings")]
    public Transform player;
    public Vector3 offset = new Vector3(0f, 2f, -10f);
    public float smoothSpeed = 0.125f;

    [Header("Dynamic Zoom Settings")]
    public float targetFOV = 90f;
    public float zoomSpeed = 5f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.fieldOfView = targetFOV;
    }

    void LateUpdate()
    {
        FollowPlayer();
        DynamicZoom();
    }

    void FollowPlayer()
    {
        Vector3 desiredPosition = player.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // Optional rotation toward the player for dynamic effect
        transform.LookAt(player);
    }

    void DynamicZoom()
    {
        // Example of subtle dynamic zoom effect based on player's speed
        float playerSpeed = player.GetComponent<Rigidbody2D>().velocity.magnitude;
        float dynamicFOV = Mathf.Lerp(targetFOV, targetFOV + 10f, playerSpeed / 20f);

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, dynamicFOV, Time.deltaTime * zoomSpeed);
    }
}
