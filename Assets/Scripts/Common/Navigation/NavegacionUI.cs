using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    /// <summary>Controla las pantallas de un Canvas y conserva el origen de cada diálogo anidado.</summary>
    public abstract partial class NavegacionUI : MonoBehaviour
    {
        Transform pantallas, capas, transicion;
        readonly Stack<GameObject> modales = new Stack<GameObject>();
        bool cargando;
        static string siguientePanel;
        protected string Pantalla { get; private set; }
        protected abstract string Inicial { get; }
        protected abstract void Conectar();
        protected virtual void AlMostrar(string nombre) { }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRuta() => siguientePanel = null;

        protected virtual void Start()
        {
            pantallas = transform.Find("SafeArea/Screens");
            capas = transform.Find("SafeArea/Overlays");
            transicion = transform.Find("TransitionOverlay");
            foreach (var control in GetComponentsInChildren<Selectable>(true))
                control.navigation = new Navigation { mode = Navigation.Mode.None };
            if (UnityEngine.EventSystems.EventSystem.current)
                UnityEngine.EventSystems.EventSystem.current.sendNavigationEvents = false;
            ConectarComunes();
            Conectar();
            var inicial = siguientePanel ?? Inicial;
            siguientePanel = null;
            Mostrar(inicial);
        }

        // Se busca únicamente al enlazar el Canvas. El ámbito evita confundir botones con nombres repetidos.
        protected Transform Buscar(string nombre, Transform raiz = null) =>
            (raiz ? raiz : transform).GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == nombre);

        protected T Pieza<T>(string nombre, string ambito = null) where T : Component =>
            Buscar(nombre, ambito == null ? transform : Buscar(ambito)).GetComponent<T>();

        protected void Enlazar(string ambito, string nombre, Action accion)
        {
            var raiz = Buscar(ambito);
            if (!raiz) return;
            foreach (var b in raiz.GetComponentsInChildren<Button>(true).Where(b => b.name == nombre))
                b.onClick.AddListener(() => accion());
        }

        /// <summary>Abre la pantalla indicada y retira los diálogos del recorrido anterior.</summary>
        public void Mostrar(string nombre)
        {
            var destino = pantallas.Find(nombre);
            if (!destino) return;
            while (modales.Count > 0) Cerrar();
            foreach (Transform pantalla in pantallas) pantalla.gameObject.SetActive(pantalla == destino);
            Pantalla = nombre;
            AlMostrar(nombre);
        }

        public void Abrir(string nombre)
        {
            var panel = capas.Find(nombre);
            if (!panel || modales.Contains(panel.gameObject)) return;
            if (modales.Count > 0) modales.Peek().SetActive(false);
            modales.Push(panel.gameObject);
            panel.gameObject.SetActive(true);
            panel.SetAsLastSibling();
            capas.Find("OrientationOverlay").SetAsLastSibling();
        }

        public void Cerrar()
        {
            if (modales.Count == 0) return;
            modales.Pop().SetActive(false);
            if (modales.Count > 0) modales.Peek().SetActive(true);
        }

        /// <summary>Bloquea una segunda pulsación mientras se carga la escena y entrega su panel inicial.</summary>
        public void Ir(string escena, string panel = null)
        {
            if (cargando) return;
            cargando = true;
            siguientePanel = panel;
            StartCoroutine(Cargar(escena));
        }

        IEnumerator Cargar(string escena)
        {
            transicion.gameObject.SetActive(true);
            var grupo = transicion.GetComponent<CanvasGroup>();
            grupo.alpha = 0;
            while (grupo.alpha < 1)
            {
                grupo.alpha += Time.unscaledDeltaTime * 8;
                yield return null;
            }
            yield return SceneManager.LoadSceneAsync(escena);
        }

        // Acciones comunes que también se pueden invocar desde los futuros eventos de partida.
        void ConectarComunes()
        {
            foreach (var boton in new[] { "CloseButton", "AcceptButton", "CancelButton", "ConfirmButton" })
                foreach (var panel in new[] { "MessageDialog", "ElementHelpDialog", "ConfirmationDialog", "PreviewDialog" })
                    Enlazar(panel, boton, Cerrar);
            Opcion("PreviewNotice", () => Avisar("preview.common"));
            Opcion("PreviewConfirm", () => Abrir("ConfirmationDialog"));
            Opcion("PreviewLoading", () => StartCoroutine(MostrarTemporal("LoadingOverlay")));
            Opcion("PreviewToast", () => StartCoroutine(MostrarTemporal("ToastPanel")));
            Opcion("ElementGuide", () => Abrir("ElementHelpDialog"));
            Enlazar("Screens", "PreviewButton", () => Abrir("PreviewDialog"));
        }

        // Las cubiertas temporales vuelven a la vista de origen aunque el juego esté en pausa.
        IEnumerator MostrarTemporal(string nombre)
        {
            Abrir(nombre);
            yield return new WaitForSecondsRealtime(1.2f);
            if (modales.Count > 0 && modales.Peek().name == nombre) Cerrar();
        }

        protected void Opcion(string nombre, Action mostrar) =>
            Enlazar("PreviewDialog", nombre, () => { Cerrar(); mostrar(); });

        protected void Avisar(string clave)
        {
            Pieza<TextoIdioma>("DescriptionLabel", "MessageDialog").Traducir(clave);
            Abrir("MessageDialog");
        }
    }
}
