using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DonmniCrash.UI
{
    /// <summary>Una clave estable por etiqueta. Los nombres escritos por el jugador se muestran como texto literal.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed partial class TextoIdioma : MonoBehaviour
    {
        public string clave;
        static readonly Dictionary<string, string[]> textos = new Dictionary<string, string[]>();
        static readonly Dictionary<string, string> porOriginal = new Dictionary<string, string>();
        object[] argumentos;
        string literal;

        // El TSV conserva juntos los dos idiomas. Se carga una sola vez por dominio.
        static void Cargar()
        {
            if (textos.Count > 0) return;
            foreach (var linea in Resources.Load<TextAsset>("UI/Idiomas").text.Split('\n'))
            {
                var c = linea.TrimEnd('\r').Split('\t');
                if (c.Length != 3) continue;
                var es = c[1].Replace("\\n", "\n");
                textos[c[0]] = new[] { es, c[2].Replace("\\n", "\n") };
                porOriginal[es] = c[0];
            }
        }

        /// <summary>La herramienta de autoría resuelve la clave una vez y la guarda en la escena.</summary>
        public static string ClaveOriginal(string texto)
        {
            Cargar();
            return texto != null && porOriginal.TryGetValue(texto, out var id) ? id : null;
        }

        public static string Leer(string id)
        {
            Cargar();
            return id != null && textos.TryGetValue(id, out var t) ? t[AspectoSesion.Ingles ? 1 : 0] : id;
        }

        public void Traducir(string id, params object[] valores)
        {
            clave = id;
            literal = null;
            argumentos = valores;
            Pintar();
        }

        public void Literal(string valor)
        {
            literal = valor;
            Pintar();
        }

        void OnEnable() { AspectoSesion.Cambio += Pintar; Pintar(); }
        void OnDisable() { AspectoSesion.Cambio -= Pintar; }
        void Pintar()
        {
            if (literal == null && string.IsNullOrEmpty(clave)) return;
            var plantilla = Leer(clave);
            GetComponent<TMP_Text>().text = literal ?? (argumentos == null || argumentos.Length == 0 ? plantilla : string.Format(plantilla, argumentos));
        }
    }
}
