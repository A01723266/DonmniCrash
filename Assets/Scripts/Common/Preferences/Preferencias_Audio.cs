using UnityEngine;

namespace DonmniCrash.UI
{
    // La mezcla se consulta por canal para recuperar los demás aunque uno tenga un valor inválido.
    public static partial class Preferencias
    {
        public static float General => LeerVolumen("audio.general", .8f);
        public static float Musica => LeerVolumen("audio.musica", .55f);
        public static float Efectos => LeerVolumen("audio.efectos", .75f);

        static float LeerVolumen(string clave, float inicial)
        {
            float valor = PlayerPrefs.GetFloat(Prefijo + clave, inicial);
            return valor >= 0f && valor <= 1f ? valor : inicial;
        }

        /// <summary>Actualiza los tres canales en memoria. AudioJuego consolida el arrastre en disco.</summary>
        internal static void ActualizarVolumen(float general, float musica, float efectos)
        {
            PlayerPrefs.SetFloat(Prefijo + "audio.general", general);
            PlayerPrefs.SetFloat(Prefijo + "audio.musica", musica);
            PlayerPrefs.SetFloat(Prefijo + "audio.efectos", efectos);
        }

        internal static void ConfirmarVolumen() => PlayerPrefs.Save();
    }
}
