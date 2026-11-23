using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    /// <summary>Un mismo prefab edita los ajustes desde Start o desde cualquier menú de pausa.</summary>
    public sealed partial class PanelAjustes : MonoBehaviour
    {
        public Slider general, musica, efectos;
        public Button anterior, siguiente;
        public TMP_Text idioma;
        public Button[] paletas, losas;
        public GameObject[] marcasPaleta, marcasLosa;

        void Awake()
        {
            anterior.onClick.AddListener(CambiarIdioma);
            siguiente.onClick.AddListener(CambiarIdioma);
            // Se captura un índice distinto por botón.
            for (int i = 0; i < paletas.Length; i++)
            {
                int indice = i;
                paletas[i].onClick.AddListener(() => AspectoSesion.Elegir(indice, AspectoSesion.Losa, AspectoSesion.Ingles));
            }
            for (int i = 0; i < losas.Length; i++)
            {
                int indice = i;
                losas[i].onClick.AddListener(() => AspectoSesion.Elegir(AspectoSesion.Paleta, indice, AspectoSesion.Ingles));
            }
            general.onValueChanged.AddListener(Mezclar);
            musica.onValueChanged.AddListener(Mezclar);
            efectos.onValueChanged.AddListener(Mezclar);
        }

        void OnEnable() { AspectoSesion.Cambio += Refrescar; Refrescar(); }
        void OnDisable()
        {
            AspectoSesion.Cambio -= Refrescar;
            if (AudioJuego.Actual) AudioJuego.Actual.ConfirmarCambios();
        }
        void CambiarIdioma() => AspectoSesion.Elegir(AspectoSesion.Paleta, AspectoSesion.Losa, !AspectoSesion.Ingles);
        void Mezclar(float valor) => AudioJuego.Actual.Mezclar(general.value, musica.value, efectos.value);

        // Reflejar los valores al reabrir el panel evita escribir la mezcla con los valores del prefab.
        void Refrescar()
        {
            idioma.text = AspectoSesion.Ingles ? "English" : "Español";
            for (int i = 0; i < marcasPaleta.Length; i++) marcasPaleta[i].SetActive(i == AspectoSesion.Paleta);
            for (int i = 0; i < marcasLosa.Length; i++) marcasLosa[i].SetActive(i == AspectoSesion.Losa);
            var audio = AudioJuego.Actual;
            // Restaurar el panel usa notificaciones silenciosas para conservar el guardado automático.
            general.SetValueWithoutNotify(audio.General);
            musica.SetValueWithoutNotify(audio.Musica);
            efectos.SetValueWithoutNotify(audio.Efectos);
        }
    }
}
