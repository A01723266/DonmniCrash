using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    /// <summary>Shared settings panel used by the start menu and pause dialogs.</summary>
    public sealed partial class SettingsPanel : MonoBehaviour
    {
        public Slider general, placement;
        public Button playAssist;
        [UnityEngine.Serialization.FormerlySerializedAs("musica")] public Slider music;
        [UnityEngine.Serialization.FormerlySerializedAs("efectos")] public Slider effects;
        [UnityEngine.Serialization.FormerlySerializedAs("anterior")] public Button previous;
        [UnityEngine.Serialization.FormerlySerializedAs("siguiente")] public Button next;
        [UnityEngine.Serialization.FormerlySerializedAs("idioma")] public TMP_Text language;
        [UnityEngine.Serialization.FormerlySerializedAs("paletas")] public Button[] palettes;
        [UnityEngine.Serialization.FormerlySerializedAs("losas")] public Button[] slabs;
        [UnityEngine.Serialization.FormerlySerializedAs("marcasPaleta")] public GameObject[] paletteMarkers;
        [UnityEngine.Serialization.FormerlySerializedAs("marcasLosa")] public GameObject[] slabMarkers;

        void Awake()
        {
            previous.onClick.AddListener(SwitchLanguage);
            next.onClick.AddListener(SwitchLanguage);
            // Capture a distinct choice for each button.
            for (int i = 0; i < palettes.Length; i++)
            {
                int index = i;
                palettes[i].onClick.AddListener(() => SessionAppearance.Choose(index, SessionAppearance.Slab, SessionAppearance.English));
            }
            for (int i = 0; i < slabs.Length; i++)
            {
                int index = i;
                slabs[i].onClick.AddListener(() => SessionAppearance.Choose(SessionAppearance.Palette, index, SessionAppearance.English));
            }
            general.onValueChanged.AddListener(Mix);
            music.onValueChanged.AddListener(Mix);
            effects.onValueChanged.AddListener(Mix);
            placement.onValueChanged.AddListener(Mix);
            playAssist.onClick.AddListener(() => UserSettings.SavePlayAssist(!UserSettings.PlayAssist));
        }

        void OnEnable()
        {
            SessionAppearance.Changed += Refresh;
            UserSettings.PlayAssistChanged += RefreshAssist;
            Refresh();
        }
        void OnDisable()
        {
            SessionAppearance.Changed -= Refresh;
            UserSettings.PlayAssistChanged -= RefreshAssist;
            if (GameAudio.Current) GameAudio.Current.FlushChanges();
        }
        void SwitchLanguage() => SessionAppearance.Choose(SessionAppearance.Palette, SessionAppearance.Slab, !SessionAppearance.English);
        void Mix(float value) => GameAudio.Current.Mix(general.value, music.value, effects.value, placement.value);

        void RefreshAssist()
        {
            var label = playAssist.GetComponentInChildren<LocalizedText>();
            label.Localize(UserSettings.PlayAssist ? "settings.assist.on" : "settings.assist.off");
        }

        // Restore the saved mix before displaying the sliders.
        void Refresh()
        {
            language.text = SessionAppearance.English ? "English" : "Español";
            RefreshAssist();
            for (int i = 0; i < paletteMarkers.Length; i++) paletteMarkers[i].SetActive(i == SessionAppearance.Palette);
            for (int i = 0; i < slabMarkers.Length; i++) slabMarkers[i].SetActive(i == SessionAppearance.Slab);
            var audio = GameAudio.Current;
            // Update controls silently to avoid saving during panel initialization.
            general.SetValueWithoutNotify(audio.General);
            music.SetValueWithoutNotify(audio.Music);
            effects.SetValueWithoutNotify(audio.Effects);
            placement.SetValueWithoutNotify(audio.Placement);
        }
    }
}
