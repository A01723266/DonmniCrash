using System;
using UnityEngine;

namespace DonmniCrash.UI
{
    /// <summary>Catálogo editable y selección temporal de aspecto e idioma de la interfaz.</summary>
    public sealed partial class AspectoSesion : ScriptableObject
    {
        public UITheme[] paletas;
        public Sprite[] losas;
        public static AspectoSesion Catalogo => catalogo ? catalogo : catalogo = Resources.Load<AspectoSesion>("UI/Aspecto");
        static AspectoSesion catalogo;
        public static int Paleta { get; private set; }
        public static int Losa { get; private set; }
        public static bool Ingles { get; private set; }
        public static event Action Cambio;

        // Unity también invoca este reinicio al entrar directamente en una escena desde el editor.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar()
        {
            catalogo = null;
            Paleta = Losa = 0;
            Ingles = false;
            Cambio = null;
        }

        /// <summary>Aplica las tres preferencias juntas para refrescar también paneles abiertos.</summary>
        public static void Elegir(int paleta, int losa, bool ingles)
        {
            Paleta = Mathf.Clamp(paleta, 0, Catalogo.paletas.Length - 1);
            Losa = Mathf.Clamp(losa, 0, Catalogo.losas.Length - 1);
            Ingles = ingles;
            Cambio?.Invoke();
        }
    }
}
