using UnityEngine;

namespace DonmniCrash.UI
{
    /// <summary>Reproductor único de sesión. Asigna los clips en el prefab reutilizable de audio.</summary>
    public sealed partial class AudioJuego : MonoBehaviour
    {
        public AudioSource musica, efectos;
        public static AudioJuego Actual { get; private set; }
        public float General { get; private set; }
        public float Musica { get; private set; }
        public float Efectos { get; private set; }

        // Un arrastre puede producir decenas de cambios. El plazo usa tiempo real también en pausa.
        const float DemoraGuardado = .4f;
        bool pendiente;
        float guardarDesde;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Preparar()
        {
            if (!Actual) Instantiate(Resources.Load<GameObject>("Prefabs/Audio/AudioJuego"));
        }

        void Awake()
        {
            if (Actual && Actual != this) { Destroy(gameObject); return; }
            Actual = this;
            DontDestroyOnLoad(gameObject);
            Aplicar(Preferencias.General, Preferencias.Musica, Preferencias.Efectos);
            if (musica.clip) musica.Play();
        }

        /// <summary>El volumen general multiplica ambos canales aunque aún no tengan clip.</summary>
        public void Mezclar(float general, float pista, float sonidos)
        {
            Aplicar(general, pista, sonidos);
            Preferencias.ActualizarVolumen(General, Musica, Efectos);
            pendiente = true;
            guardarDesde = Time.unscaledTime + DemoraGuardado;
        }

        // Restaurar desde PlayerPrefs usa esta misma mezcla sin iniciar otra escritura.
        void Aplicar(float general, float pista, float sonidos)
        {
            General = Mathf.Clamp01(general);
            Musica = Mathf.Clamp01(pista);
            Efectos = Mathf.Clamp01(sonidos);
            musica.volume = General * Musica;
            efectos.volume = General * Efectos;
        }

        void Update()
        {
            if (pendiente && Time.unscaledTime >= guardarDesde) ConfirmarCambios();
        }

        /// <summary>Al cerrar Ajustes o abandonar la aplicación se guarda el último valor del gesto.</summary>
        public void ConfirmarCambios()
        {
            if (!pendiente) return;
            Preferencias.ConfirmarVolumen();
            pendiente = false;
        }

        void OnApplicationPause(bool pausa) { if (pausa) ConfirmarCambios(); }
        void OnApplicationFocus(bool tieneFoco) { if (!tieneFoco) ConfirmarCambios(); }
        void OnApplicationQuit() => ConfirmarCambios();
    }
}
