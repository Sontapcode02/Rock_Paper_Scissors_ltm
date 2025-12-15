using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("--- AUDIO SOURCES ---")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("--- AUDIO CLIPS ---")]
    public AudioClip backgroundMusic;
    public AudioClip clickSound;
    public AudioClip winSound;
    public AudioClip loseSound;
    public AudioClip drawSound;
    public AudioClip selectSound;

    public bool IsMuted = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        PlayMusic(backgroundMusic);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    public void PlayClick() => PlaySFX(clickSound);
    public void PlaySelect() => PlaySFX(selectSound);
    public void PlayWin() => PlaySFX(winSound);
    public void PlayLose() => PlaySFX(loseSound);
    public void PlayDraw() => PlaySFX(drawSound);

    public void ToggleMute()
    {
        IsMuted = !IsMuted;
        AudioListener.volume = IsMuted ? 0 : 1;
    }
}