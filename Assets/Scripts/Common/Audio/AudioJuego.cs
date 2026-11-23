using UnityEngine;

namespace DonmniCrash.UI
{
    /// <summary>Reproductor único de sesión. Asigna los clips en el prefab reutilizable de audio.</summary>
    public sealed partial class AudioJuego : MonoBehaviour
    {
        public AudioSource musica, efectos;
        public static AudioJuego Actual { get; private set; }
        public float General { get; private set; } = .8f;
        public float Musica { get; private set; } = .55f;
        public float Efectos { get; private set; } = .75f;

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
            Mezclar(General, Musica, Efectos);
            if (musica.clip) musica.Play();
        }

        /// <summary>El volumen general multiplica ambos canales aunque aún no tengan clip.</summary>
        public void Mezclar(float general, float pista, float sonidos)
        {
            General = Mathf.Clamp01(general);
            Musica = Mathf.Clamp01(pista);
            Efectos = Mathf.Clamp01(sonidos);
            musica.volume = General * Musica;
            efectos.volume = General * Efectos;
        }
    }
}
