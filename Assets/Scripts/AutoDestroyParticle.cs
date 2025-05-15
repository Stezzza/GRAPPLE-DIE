using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class AutoDestroyParticle : MonoBehaviour
{
    void Start()
    {
        // Determine the longest possible lifetime of any particle
        var ps = GetComponent<ParticleSystem>();
        float totalLifetime = ps.main.duration
                             + ps.main.startLifetime.constantMax;

        // Destroy this GameObject after the system has played through
        Destroy(gameObject, totalLifetime);
    }
}
