using UnityEngine;

/// <summary>
/// Plays a looping BGM track for a world scene.
/// Drop on any GameObject in the scene and assign bgmClip in the Inspector
/// (or run Tools → Otowa → Audio → Wire Day1World Audio to auto-assign).
/// </summary>
public class WorldBGM : MonoBehaviour
{
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] [Range(0f, 1f)] private float volume = 0.4f;

    private void Start()
    {
        if (bgmClip == null) return;
        var src = gameObject.AddComponent<AudioSource>();
        src.clip        = bgmClip;
        src.volume      = volume;
        src.loop        = true;
        src.playOnAwake = false;
        src.Play();
    }
}
