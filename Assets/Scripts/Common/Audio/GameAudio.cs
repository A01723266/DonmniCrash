using UnityEngine;

namespace DonmniCrash.UI
{
    /// <summary>Owns the session audio sources and survives scene changes.</summary>
    public sealed partial class GameAudio : MonoBehaviour
    {
        [UnityEngine.Serialization.FormerlySerializedAs("musica")] public AudioSource music;
        [UnityEngine.Serialization.FormerlySerializedAs("efectos")] public AudioSource effects;
        public AudioSource placements;
        public AudioClip clickClip, turnClip, resultClip, placementClip;
        public float Placement { get; private set; }

        public void PlayPlacement() => placements.PlayOneShot(placementClip);

        // Cues are local feedback. A confirmed network action never sends an audio packet.
        public void PlayCue(AudioClip clip)
        {
            if (clip) effects.PlayOneShot(clip);
        }

        public void OnButtonGesture()
        {
            UnlockMusic();
            PlayCue(clickClip);
        }

        public void UnlockMusic()
        {
            if (waitingForGesture)
            {
                waitingForGesture = false;
                if (playlist.Count > 0) PlayNext();
            }
        }
        public static GameAudio Current { get; private set; }
        public float General { get; private set; }
        public float Music { get; private set; }
        public float Effects { get; private set; }

        // Debounce slider drags using real time so pausing cannot delay a save indefinitely.
        const float SaveDelay = .4f;
        bool pendingSave;
        float saveAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Prepare()
        {
            if (!Current) Instantiate(Resources.Load<GameObject>("Prefabs/Audio/AudioJuego"));
        }

        void Awake()
        {
            if (Current && Current != this) { Destroy(gameObject); return; }
            Current = this;
            DontDestroyOnLoad(gameObject);
            ApplyValues(UserSettings.General, UserSettings.Music, UserSettings.Effects, UserSettings.Placement);
            InitializeMusic();
        }

        /// <summary>Apply master volume to both independently adjustable channels.</summary>
        public void Mix(float general, float musicLevel, float soundLevel, float placementLevel)
        {
            ApplyValues(general, musicLevel, soundLevel, placementLevel);
            UserSettings.UpdateVolume(General, Music, Effects, Placement);
            pendingSave = true;
            saveAt = Time.unscaledTime + SaveDelay;
        }

        // Use the same mix when restoring preferences without scheduling another save.
        void ApplyValues(float general, float musicLevel, float soundLevel, float placementLevel)
        {
            General = Mathf.Clamp01(general);
            Music = Mathf.Clamp01(musicLevel);
            Effects = Mathf.Clamp01(soundLevel);
            Placement = Mathf.Clamp01(placementLevel);
            placements.volume = General * Placement;
            music.volume = General * Music;
            effects.volume = General * Effects;
        }

        void Update()
        {
            if (pendingSave && Time.unscaledTime >= saveAt) FlushChanges();
            UpdateMusic();
        }

        void OnDestroy()
        {
            if (Current != this) return;
            ReleaseMusic();
            Current = null;
        }

        /// <summary>Flush the final slider value when settings close or the application exits.</summary>
        public void FlushChanges()
        {
            if (!pendingSave) return;
            UserSettings.FlushVolume();
            pendingSave = false;
        }

        void OnApplicationPause(bool paused) { if (paused) FlushChanges(); }
        void OnApplicationFocus(bool focused) { if (!focused) FlushChanges(); }
        void OnApplicationQuit() => FlushChanges();
    }
}
