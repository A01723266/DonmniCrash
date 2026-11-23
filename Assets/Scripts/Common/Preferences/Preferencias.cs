using System;
using UnityEngine;

namespace DonmniCrash.UI
{
    /// <summary>Preferencias locales compartidas por todas las escenas. Leerlas tampoco requiere abrir Start.</summary>
    public static partial class Preferencias
    {
        const string Prefijo = "DonmniCrash.v1.";
        const int LimiteNombre = 28;
        const string PaletaInicial = "SlateGold";
        const string LosaInicial = "runeGrey_slabOutline_036_0";
        static readonly string[] emblemas = { "Earth", "Water", "Wind", "Fire", "Plant" };

        public static System.Collections.Generic.IReadOnlyList<string> Emblemas { get; } = Array.AsReadOnly(emblemas);

        public static string Nombre => NormalizarNombre(PlayerPrefs.GetString(Prefijo + "perfil.nombre", ""));
        public static string Emblema => ValidarEmblema(PlayerPrefs.GetString(Prefijo + "perfil.emblema", emblemas[0]));
        public static bool Ingles => PlayerPrefs.GetString(Prefijo + "idioma", "es") == "en";

        // El nombre vacío conserva la traducción de «Jugador» que ya muestra la vista.
        static string NormalizarNombre(string nombre)
        {
            string limpio = (nombre ?? "").Trim();
            return limpio.Length <= LimiteNombre ? limpio : "";
        }

        static string ValidarEmblema(string id) => Array.IndexOf(emblemas, id) >= 0 ? id : emblemas[0];

        /// <summary>Confirma las dos partes del borrador de perfil en el mismo guardado.</summary>
        public static void GuardarPerfil(string nombre, string emblema)
        {
            PlayerPrefs.SetString(Prefijo + "perfil.nombre", NormalizarNombre(nombre));
            PlayerPrefs.SetString(Prefijo + "perfil.emblema", ValidarEmblema(emblema));
            PlayerPrefs.Save();
        }

        // Los índices sirven a los botones. El almacenamiento identifica cada recurso por su nombre técnico.
        public static int Paleta => IndiceGuardado("paleta", PaletaInicial, AspectoSesion.Catalogo.paletas);
        public static int Losa => IndiceGuardado("losa", LosaInicial, AspectoSesion.Catalogo.losas);

        static int IndiceGuardado<T>(string clave, string inicial, T[] opciones) where T : UnityEngine.Object
        {
            string id = PlayerPrefs.GetString(Prefijo + clave, inicial);
            int indice = Array.FindIndex(opciones, opcion => opcion != null && opcion.name == id);
            if (indice >= 0) return indice;
            return Mathf.Max(0, Array.FindIndex(opciones, opcion => opcion != null && opcion.name == inicial));
        }

        internal static void GuardarAspecto(int paleta, int losa, bool ingles)
        {
            var catalogo = AspectoSesion.Catalogo;
            PlayerPrefs.SetString(Prefijo + "paleta", catalogo.paletas[paleta].name);
            PlayerPrefs.SetString(Prefijo + "losa", catalogo.losas[losa].name);
            PlayerPrefs.SetString(Prefijo + "idioma", ingles ? "en" : "es");
            PlayerPrefs.Save();
        }
    }
}
