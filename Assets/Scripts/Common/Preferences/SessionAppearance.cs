using System;
using UnityEngine;

namespace DonmniCrash.UI
{
    /// <summary>Editable theme catalog and session appearance.</summary>
    public sealed partial class SessionAppearance : ScriptableObject
    {
        [UnityEngine.Serialization.FormerlySerializedAs("paletas")] public UITheme[] palettes;
        [UnityEngine.Serialization.FormerlySerializedAs("losas")] public Sprite[] slabs;
        public static SessionAppearance Catalog => catalog ? catalog : catalog = Resources.Load<SessionAppearance>("UI/Aspecto");
        static SessionAppearance catalog;
        public static int Palette { get; private set; }
        public static int Slab { get; private set; }
        public static bool English { get; private set; } = true;
        public static event Action Changed;

        // Also reset static state when entering a scene directly from the editor.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState()
        {
            catalog = null;
            Palette = Slab = 0;
            English = true;
            Changed = null;
        }

        // Resolve the catalog before the first scene enables its tiles and labels.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Restore()
        {
            Palette = UserSettings.Palette;
            Slab = UserSettings.Slab;
            English = UserSettings.English;
        }

        /// <summary>Apply the three preferences together so open panels refresh consistently.</summary>
        public static void Choose(int paletteIndex, int slabIndex, bool english)
        {
            paletteIndex = Mathf.Clamp(paletteIndex, 0, Catalog.palettes.Length - 1);
            slabIndex = Mathf.Clamp(slabIndex, 0, Catalog.slabs.Length - 1);
            if (Palette == paletteIndex && Slab == slabIndex && English == english) return;
            UserSettings.SaveAppearance(paletteIndex, slabIndex, english);
            Palette = paletteIndex;
            Slab = slabIndex;
            English = english;
            Changed?.Invoke();
        }
    }
}
