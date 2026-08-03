using UnityEngine;

namespace MobileCore
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private const string SfxVolumeKey = "sfxVolume";
        private const string MusicVolumeKey = "musicVolume";

        [Header("Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;

        [Header("Gameplay Clips")]
        [SerializeField] private AudioClip cubeBreakClip;
        [SerializeField] private AudioClip shooterLaunchClip;

        [Header("Stingers — short pieces played over the win/lose panels")]
        [SerializeField] private AudioClip levelCompletedClip;
        [SerializeField] private AudioClip levelFailedClip;

        [Header("UI")]
        [SerializeField] private AudioClip uiClickClip;

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

            sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, sfxVolume);
            musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, musicVolume);
            ApplyVolumes();
        }

        private void OnEnable()
        {
            GameEvents.OnRemainingCubesChanged += HandleCubeBroken;
            GameEvents.OnShooterLaunched += HandleShooterLaunched;
            GameEvents.OnLevelCompleted += HandleLevelCompleted;
            GameEvents.OnLevelFailed += HandleLevelFailed;
        }

        private void OnDisable()
        {
            GameEvents.OnRemainingCubesChanged -= HandleCubeBroken;
            GameEvents.OnShooterLaunched -= HandleShooterLaunched;
            GameEvents.OnLevelCompleted -= HandleLevelCompleted;
            GameEvents.OnLevelFailed -= HandleLevelFailed;
        }

        // Slight pitch scatter so a fast chain of breaks doesn't sound like a machine.
        private void HandleCubeBroken(int remaining) =>
            PlaySfx(cubeBreakClip, Random.Range(0.94f, 1.06f));

        private void HandleShooterLaunched() => PlaySfx(shooterLaunchClip);
        private void HandleLevelCompleted() => PlayStinger(levelCompletedClip);
        private void HandleLevelFailed() => PlayStinger(levelFailedClip);

        public void PlayUiClick() => PlaySfx(uiClickClip);

        /// <summary>
        /// Win / lose pieces. Skips the anti-machine-gun throttle, which would
        /// otherwise swallow the sting when it lands right after a cube break.
        /// </summary>
        public void PlayStinger(AudioClip clip)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.pitch = 1f;
            sfxSource.PlayOneShot(clip, sfxVolume);
        }


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
            PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        }

        public void SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            if (musicSource != null) musicSource.volume = musicVolume;
            PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
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