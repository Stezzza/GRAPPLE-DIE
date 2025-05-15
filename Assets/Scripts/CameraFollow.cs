using UnityEngine;

[RequireComponent(typeof(Camera))]
public class UniqueCameraFollow : MonoBehaviour
{
    [Header("Camera Follow Settings")]
    [Tooltip("Drag your Player transform here (must have a Rigidbody2D).")]
    public Transform player;

    [Tooltip("Offset from the player's position.")]
    public Vector3 offset = new Vector3(0f, 2f, -10f);

    [Tooltip("How quickly the camera moves to follow.")]
    public float smoothSpeed = 10f;

    [Header("Dynamic Zoom Settings")]
    [Tooltip("Base field of view when player is stationary.")]
    public float baseFOV = 60f;

    [Tooltip("Maximum additional FOV at top speed.")]
    public float maxFOVIncrease = 10f;

    [Tooltip("How quickly the FOV interpolates.")]
    public float zoomSpeed = 5f;

    [Tooltip("Approximate top player speed used to normalize zoom (e.g. 20 units/sec).")]
    public float maxPlayerSpeed = 20f;

    // Cached references
    private Camera cam;
    private Rigidbody2D playerRb;

    void Awake()
    {
        // Cache camera component
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        // Initialize FOV
        cam.fieldOfView = baseFOV;

        // Cache the player's Rigidbody2D if possible
        if (player != null)
            playerRb = player.GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        // Subscribe to a player-death event (optional)
        PlayerHealth.OnPlayerDeath += HandlePlayerDeath;
    }

    void OnDisable()
    {
        PlayerHealth.OnPlayerDeath -= HandlePlayerDeath;
    }

    void LateUpdate()
    {
        // If we no longer have a player reference, try to find one by tag
        if (player == null)
        {
            TryFindPlayer();
            if (player == null)
                return;  // nothing to follow
        }

        FollowPlayer();
        DynamicZoom();
    }

    /// <summary>
    /// Smoothly moves the camera toward the player’s position plus offset.
    /// </summary>
    private void FollowPlayer()
    {
        Vector3 desiredPosition = player.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position,
            desiredPosition,
            Time.deltaTime * smoothSpeed
        );
        transform.position = smoothedPosition;
    }

    /// <summary>
    /// Adjusts the camera’s field of view based on the player’s speed.
    /// </summary>
    private void DynamicZoom()
    {
        float speed = 0f;
        if (playerRb != null)
            speed = playerRb.velocity.magnitude;

        // Normalize speed to a 0–1 range
        float t = Mathf.Clamp01(speed / maxPlayerSpeed);

        // Compute the target FOV and interpolate toward it
        float targetFOV = baseFOV + (maxFOVIncrease * t);
        cam.fieldOfView = Mathf.Lerp(
            cam.fieldOfView,
            targetFOV,
            Time.deltaTime * zoomSpeed
        );
    }

    /// <summary>
    /// Clears our player reference when they die, preventing further access.
    /// </summary>
    private void HandlePlayerDeath()
    {
        player = null;
        playerRb = null;
    }

    /// <summary>
    /// Attempts to locate a Player by tag if the reference has been lost.
    /// </summary>
    private void TryFindPlayer()
    {
        GameObject go = GameObject.FindWithTag("Player");
        if (go != null)
        {
            player = go.transform;
            playerRb = go.GetComponent<Rigidbody2D>();
        }
    }
}
