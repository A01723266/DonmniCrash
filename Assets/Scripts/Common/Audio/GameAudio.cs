using UnityEngine;

namespace DonmniCrash.UI
{
    /// <summary>Owns the two session audio sources and survives scene changes.</summary>
    public sealed partial class GameAudio : MonoBehaviour
    {
        [UnityEngine.Serialization.FormerlySerializedAs("musica")] public AudioSource music;
        [UnityEngine.Serialization.FormerlySerializedAs("efectos")] public AudioSource effects;
        public AudioClip clickClip, turnClip, resultClip;

        // Cues are local feedback. A confirmed network action never sends an audio packet.
        public void PlayCue(AudioClip clip)
        {
            if (clip) effects.PlayOneShot(clip);
        }

        public void OnButtonGesture()
        {
            if (waitingForGesture)
            {
                waitingForGesture = false;
                if (playlist.Count > 0) PlayNext();
            }
            PlayCue(clickClip);
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
            ApplyValues(UserSettings.General, UserSettings.Music, UserSettings.Effects);
            InitializeMusic();
        }

        /// <summary>Apply master volume to both independently adjustable channels.</summary>
        public void Mix(float general, float musicLevel, float soundLevel)
        {
            ApplyValues(general, musicLevel, soundLevel);
            UserSettings.UpdateVolume(General, Music, Effects);
            pendingSave = true;
            saveAt = Time.unscaledTime + SaveDelay;
        }

        // Use the same mix when restoring preferences without scheduling another save.
        void ApplyValues(float general, float musicLevel, float soundLevel)
        {
            General = Mathf.Clamp01(general);
            Music = Mathf.Clamp01(musicLevel);
            Effects = Mathf.Clamp01(soundLevel);
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
