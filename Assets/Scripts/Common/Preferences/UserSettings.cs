using System;
using UnityEngine;

namespace DonmniCrash.UI
{
    /// <summary>Persistent preferences accessible even when a match scene is opened directly.</summary>
    public static partial class UserSettings
    {
        const string KeyPrefix = "DonmniCrash.v1.";
        const int NameLimit = 28;
        const string DefaultPalette = "SlateGold";
        const string DefaultSlab = "runeGrey_slabOutline_036_0";
        static readonly string[] emblems = { "Earth", "Water", "Wind", "Fire", "Plant" };

        public static System.Collections.Generic.IReadOnlyList<string> Emblems { get; } = Array.AsReadOnly(emblems);

        public static string DisplayName => NormalizeName(PlayerPrefs.GetString(KeyPrefix + "perfil.nombre", ""));
        public static string Emblem => ValidateEmblem(PlayerPrefs.GetString(KeyPrefix + "perfil.emblema", emblems[0]));
        public static bool English => PlayerPrefs.GetString(KeyPrefix + "idioma", "es") == "en";

        // An empty name uses the current translation of Player.
        static string NormalizeName(string name)
        {
            string clean = (name ?? "").Trim();
            return clean.Length <= NameLimit ? clean : "";
        }

        static string ValidateEmblem(string id) => Array.IndexOf(emblems, id) >= 0 ? id : emblems[0];

        /// <summary>Commit both profile fields in one save.</summary>
        public static void SaveProfile(string name, string emblem)
        {
            PlayerPrefs.SetString(KeyPrefix + "perfil.nombre", NormalizeName(name));
            PlayerPrefs.SetString(KeyPrefix + "perfil.emblema", ValidateEmblem(emblem));
            PlayerPrefs.Save();
        }

        // Buttons use indices, while persistence uses stable resource identifiers.
        public static int Palette => SavedIndex("paleta", DefaultPalette, SessionAppearance.Catalog.palettes);
        public static int Slab => SavedIndex("losa", DefaultSlab, SessionAppearance.Catalog.slabs);

        static int SavedIndex<T>(string key, string initial, T[] options) where T : UnityEngine.Object
        {
            string id = PlayerPrefs.GetString(KeyPrefix + key, initial);
            int index = Array.FindIndex(options, option => option != null && option.name == id);
            if (index >= 0) return index;
            return Mathf.Max(0, Array.FindIndex(options, option => option != null && option.name == initial));
        }

        internal static void SaveAppearance(int paletteIndex, int slabIndex, bool english)
        {
            var catalog = SessionAppearance.Catalog;
            PlayerPrefs.SetString(KeyPrefix + "paleta", catalog.palettes[paletteIndex].name);
            PlayerPrefs.SetString(KeyPrefix + "losa", catalog.slabs[slabIndex].name);
            PlayerPrefs.SetString(KeyPrefix + "idioma", english ? "en" : "es");
            PlayerPrefs.Save();
        }
    }
}
