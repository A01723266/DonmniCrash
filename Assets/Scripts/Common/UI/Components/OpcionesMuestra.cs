using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    /// <summary>Valores de formulario para el recorrido visual. La futura partida podrá leer su selección al confirmar.</summary>
    public sealed partial class OpcionesMuestra : MonoBehaviour
    {
        public string[] opciones;
        public int indice;
        // La etiqueta puede ser un número literal o una opción traducible.
        public string Valor => opciones[indice];
        void Awake()
        {
            foreach (var boton in GetComponentsInChildren<Button>(true))
            {
                int delta = boton.name == "NextButton" ? 1 : -1;
                boton.onClick.AddListener(() => Elegir(indice + delta));
            }
            Elegir(indice);
        }
        public void Elegir(int valor)
        {
            indice = (valor % opciones.Length + opciones.Length) % opciones.Length;
            var texto = GetComponentsInChildren<TextoIdioma>(true).First(t => t.name == "ValueLabel");
            var clave = TextoIdioma.ClaveOriginal(Valor);
            if (clave == null) texto.Literal(Valor);
            else texto.Traducir(clave);
        }
    }
}
