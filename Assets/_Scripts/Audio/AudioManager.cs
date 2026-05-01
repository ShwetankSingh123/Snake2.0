using UnityEngine;

/// <summary>
/// AudioManager: plays music and SFX with Unity's built-in AudioSource.
/// Assign clips in Inspector. No external packages needed.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameMusic;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip eatSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip shieldBreakSound;
    [SerializeField] private AudioClip comboSound;
    [SerializeField] private AudioClip uiClickSound;

    [Header("Volume")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume   = 1.0f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Load saved preferences
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        sfxVolume   = PlayerPrefs.GetFloat("SFXVolume",   1.0f);
        ApplyVolumes();
    }

    public void PlayMenuMusic() => SwitchMusic(menuMusic);
    public void PlayGameMusic() => SwitchMusic(gameMusic);

    private void SwitchMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void PlayEat()         => PlaySFX(eatSound);
    public void PlayDeath()       => PlaySFX(deathSound);
    public void PlayShieldBreak() => PlaySFX(shieldBreakSound);
    public void PlayCombo()       => PlaySFX(comboSound);
    public void PlayUIClick()     => PlaySFX(uiClickSound);

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void SetMusicVolume(float v)
    {
        musicVolume = v;
        if (musicSource) musicSource.volume = v;
        PlayerPrefs.SetFloat("MusicVolume", v);
    }

    public void SetSFXVolume(float v)
    {
        sfxVolume = v;
        PlayerPrefs.SetFloat("SFXVolume", v);
    }

    private void ApplyVolumes()
    {
        if (musicSource) musicSource.volume = musicVolume;
    }
}
