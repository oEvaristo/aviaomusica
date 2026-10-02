using UnityEngine;

namespace ProximoVoo
{
    [DefaultExecutionOrder(30)]
    public sealed class FlightMusic : MonoBehaviour
    {
        [SerializeField] private FlightController flight;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip musicClip;
        [Header("Volume da música (%)")]
        [Tooltip("Volume inicial e atual da música. Pode ser ajustado durante o Play ou por SetVolumePercent.")]
        [Range(0f, 100f)]
        [SerializeField] private float volumePercent = 30f;

        private bool started, wasPaused;
        private float initialVolumePercent;

        public float VolumePercent => volumePercent;

        public void Configure(FlightController controller, AudioSource source, AudioClip clip)
        {
            flight = controller;
            musicSource = source;
            musicClip = clip;
            ConfigureSource();
        }

        public void SetVolumePercent(float percentage)
        {
            volumePercent = Mathf.Clamp(percentage, 0f, 100f);
            if (musicSource != null) musicSource.volume = volumePercent / 100f;
        }

        private void Awake()
        {
            initialVolumePercent = volumePercent;
            if (flight == null || musicSource == null || musicClip == null)
            {
                Debug.LogError("Configure a música de fundo, a fonte de áudio e o avião.", this);
                enabled = false;
                return;
            }
            ConfigureSource();
            musicSource.Stop();
        }

        private void ConfigureSource()
        {
            if (musicSource == null) return;
            musicSource.playOnAwake = false;
            musicSource.loop = false;
            musicSource.spatialBlend = 0f;
            musicSource.clip = musicClip;
            musicSource.volume = Mathf.Clamp01(volumePercent / 100f);
        }

        private void Update()
        {
            musicSource.volume = Mathf.Clamp01(volumePercent / 100f);
            if (flight.Phase == FlightPhase.Ready || flight.Phase == FlightPhase.StartingEngine ||
                flight.Phase == FlightPhase.Rolling)
            {
                if (started)
                {
                    musicSource.Stop();
                    SetVolumePercent(initialVolumePercent);
                }
                started = false;
                wasPaused = false;
                return;
            }

            // Start when the aircraft leaves the runway, then let the song accompany the tribute.
            if (!started)
            {
                musicSource.Play();
                started = true;
            }
            if (flight.IsPaused == wasPaused) return;
            if (flight.IsPaused) musicSource.Pause(); else musicSource.UnPause();
            wasPaused = flight.IsPaused;
        }

        private void OnDisable()
        {
            if (musicSource != null) musicSource.Stop();
            started = false;
            wasPaused = false;
        }
    }
}
