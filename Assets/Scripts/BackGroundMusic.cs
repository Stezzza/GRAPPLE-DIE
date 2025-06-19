using UnityEngine;
using UnityEngine.UI;

public class BackgroundMusic : MonoBehaviour
{
    // holds the music player
    private AudioSource musicSource;

    // function for the slider to call
    public void SetVolume(float volume)
    {
        // find the music player if we don't have it
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
        }

        // just in case it's still not found
        if (musicSource == null)
        {
            Debug.LogWarning("audio source not found on " + gameObject.name);
            return;
        }

        // change the volume
        musicSource.volume = volume;
    }
}