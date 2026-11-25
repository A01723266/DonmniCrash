using UnityEngine;

namespace DonmniCrash.UI
{
    // Recover each channel independently if a saved value is invalid.
    public static partial class UserSettings
    {
        public static float General => ReadVolume("audio.general", .8f);
        public static float Music => ReadVolume("audio.musica", .55f);
        public static float Effects => ReadVolume("audio.efectos", .75f);

        public static float Placement => ReadVolume("audio.fichas", .75f);

        static float ReadVolume(string key, float initial)
        {
            float value = PlayerPrefs.GetFloat(KeyPrefix + key, initial);
            return value >= 0f && value <= 1f ? value : initial;
        }

        /// <summary>Update the volume channels in memory. GameAudio flushes the final drag value to disk.</summary>
        internal static void UpdateVolume(float general, float music, float effects, float placement)
        {
            PlayerPrefs.SetFloat(KeyPrefix + "audio.general", general);
            PlayerPrefs.SetFloat(KeyPrefix + "audio.musica", music);
            PlayerPrefs.SetFloat(KeyPrefix + "audio.efectos", effects);
            PlayerPrefs.SetFloat(KeyPrefix + "audio.fichas", placement);
        }

        internal static void FlushVolume() => PlayerPrefs.Save();
    }
}
