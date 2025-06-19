using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    // static instance to access from other scripts
    public static CameraShake Instance { get; private set; }

    // how much to shake by
    private Vector3 shakeOffset = Vector3.zero;

    void Awake()
    {
        Instance = this;
    }

    // applies the shake after camera follow
    void LateUpdate()
    {
        if (shakeOffset != Vector3.zero)
        {
            transform.position += shakeOffset;
        }
    }

    public void Shake(float duration, float magnitude)
    {
        StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            // set the offset for lateupdate
            shakeOffset = new Vector3(x, y, 0);

            elapsed += Time.deltaTime;

            yield return null; // wait a frame
        }

        // reset when done
        shakeOffset = Vector3.zero;
    }
}