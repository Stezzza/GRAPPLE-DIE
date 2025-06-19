using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("follow settings")]
    public Transform player;
    public Vector3 offset = new Vector3(0f, 2f, -10f);
    public float smoothSpeed = 10f;

    [Header("zoom settings")]
    public float baseFOV = 60f;
    public float maxFOVIncrease = 10f;
    public float zoomSpeed = 5f;
    public float maxPlayerSpeed = 20f;

    // component refs
    private Camera cam;
    private Rigidbody2D playerRb;

    void Awake()
    {
        // get camera
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        // set starting fov
        cam.fieldOfView = baseFOV;

        // get player physics
        if (player != null)
            playerRb = player.GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        // listen for player death
        PlayerHealth.OnPlayerDeath += HandlePlayerDeath;
    }

    void OnDisable()
    {
        PlayerHealth.OnPlayerDeath -= HandlePlayerDeath;
    }

    void LateUpdate()
    {
        // if no player, try to find one
        if (player == null)
        {
            TryFindPlayer();
            if (player == null)
                return; // exit if still no player
        }

        FollowPlayer();
        DynamicZoom();
    }

    // moves camera to follow player
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

    // zooms out based on player speed
    private void DynamicZoom()
    {
        float speed = 0f;
        if (playerRb != null)
            speed = playerRb.velocity.magnitude;

        // speed from 0 to 1
        float t = Mathf.Clamp01(speed / maxPlayerSpeed);

        // calculate and set new fov
        float targetFOV = baseFOV + (maxFOVIncrease * t);
        cam.fieldOfView = Mathf.Lerp(
            cam.fieldOfView,
            targetFOV,
            Time.deltaTime * zoomSpeed
        );
    }

    // clears player when they die
    private void HandlePlayerDeath()
    {
        player = null;
        playerRb = null;
    }

    // finds player by tag
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