using Unity.AppUI.UI;
using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    public void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null)
            return;

        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.PlayOneShot(clip);
        audioSource.pitch = 1f;
    }
}
