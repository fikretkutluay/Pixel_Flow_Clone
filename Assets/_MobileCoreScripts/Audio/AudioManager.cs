using UnityEngine;

namespace MobileCore
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;

        [Header("Gameplay Clips")]
        [SerializeField] private AudioClip cubeBreakClip;
        [SerializeField] private AudioClip rescueWarningClip;
        [SerializeField] private AudioClip levelCompletedClip;
        [SerializeField] private AudioClip levelFailedClip;

        [Header("Music")]
        [SerializeField] private AudioClip backgroundMusic;

        [Header("Settings")]
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;
        [SerializeField] private float minSfxInterval = 0.04f;

        private float lastSfxTime = -999f;

        public float SfxVolume => sfxVolume;
        public float MusicVolume => musicVolume;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ApplyVolumes();
        }

        private void OnEnable()
        {
            GameEvents.OnRemainingCubesChanged += HandleCubeBroken;
            GameEvents.OnRescueStarted += HandleRescueStarted;
            GameEvents.OnLevelCompleted += HandleLevelCompleted;
            GameEvents.OnLevelFailed += HandleLevelFailed;
        }

        private void OnDisable()
        {
            GameEvents.OnRemainingCubesChanged -= HandleCubeBroken;
            GameEvents.OnRescueStarted -= HandleRescueStarted;
            GameEvents.OnLevelCompleted -= HandleLevelCompleted;
            GameEvents.OnLevelFailed -= HandleLevelFailed;
        }

        private void Start()
        {
            PlayMusic(backgroundMusic);
        }


        private void HandleCubeBroken(int remaining) => PlaySfx(cubeBreakClip);
        private void HandleRescueStarted() => PlaySfx(rescueWarningClip);
        private void HandleLevelCompleted() => PlaySfx(levelCompletedClip);
        private void HandleLevelFailed() => PlaySfx(levelFailedClip);


        public void PlaySfx(AudioClip clip, float pitch = 1f)
        {
            if (clip == null || sfxSource == null) return;

            if (Time.unscaledTime - lastSfxTime < minSfxInterval) return;
            lastSfxTime = Time.unscaledTime;

            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, sfxVolume);
        }

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null || musicSource == null) return;
            if (musicSource.clip == clip && musicSource.isPlaying) return;

            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource != null) musicSource.Stop();
        }

        public void SetSfxVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
        }

        public void SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            if (musicSource != null) musicSource.volume = musicVolume;
        }

        private void ApplyVolumes()
        {
            if (musicSource != null) musicSource.volume = musicVolume;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}