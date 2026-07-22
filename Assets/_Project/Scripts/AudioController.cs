using UnityEngine;

public class AudioController : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip spinSound;
    [SerializeField] private AudioClip modifierAddSound;
    [SerializeField] private AudioClip successSound;

    public void PlayRoll() => Play(spinSound);
    public void PlayModifier() => Play(modifierAddSound);
    public void PlaySuccess() => Play(successSound);

    private void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}
