using System;
using UnityEngine;

namespace DonmniCrash.UI
{
    /// <summary>Catálogo editable y selección local de aspecto e idioma de la interfaz.</summary>
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

        // Se resuelve el catálogo antes de que se activen las fichas y etiquetas de la primera escena.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Recuperar()
        {
            Paleta = Preferencias.Paleta;
            Losa = Preferencias.Losa;
            Ingles = Preferencias.Ingles;
        }

        /// <summary>Aplica las tres preferencias juntas para refrescar también paneles abiertos.</summary>
        public static void Elegir(int paleta, int losa, bool ingles)
        {
            paleta = Mathf.Clamp(paleta, 0, Catalogo.paletas.Length - 1);
            losa = Mathf.Clamp(losa, 0, Catalogo.losas.Length - 1);
            if (Paleta == paleta && Losa == losa && Ingles == ingles) return;
            Preferencias.GuardarAspecto(paleta, losa, ingles);
            Paleta = paleta;
            Losa = losa;
            Ingles = ingles;
            Cambio?.Invoke();
        }
    }
}
