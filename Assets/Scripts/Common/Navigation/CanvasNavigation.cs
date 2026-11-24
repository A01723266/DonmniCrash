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
    /// <summary>Manage one canvas and restore the parent dialog after closing a nested panel.</summary>
    public abstract partial class CanvasNavigation : MonoBehaviour
    {
        Transform screens, overlays, transition;
        readonly Stack<GameObject> dialogsStack = new Stack<GameObject>();
        bool loadingScene;
        protected bool InteractionBlocked => loadingScene || dialogsStack.Count > 0;
        static string nextScreen;
        protected string ScreenName { get; private set; }
        protected abstract string InitialScreen { get; }
        protected abstract void BindControls();
        protected virtual void OnScreenShown(string name) { }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRoute() => nextScreen = null;

        protected virtual void Start()
        {
            screens = transform.Find("SafeArea/Screens");
            overlays = transform.Find("SafeArea/Overlays");
            transition = transform.Find("TransitionOverlay");
            foreach (var control in GetComponentsInChildren<Selectable>(true))
                control.navigation = new Navigation { mode = Navigation.Mode.None };
            if (UnityEngine.EventSystems.EventSystem.current)
                UnityEngine.EventSystems.EventSystem.current.sendNavigationEvents = false;
            foreach (var button in GetComponentsInChildren<Button>(true))
                button.onClick.AddListener(() => GameAudio.Current?.OnButtonGesture());
            BindShared();
            BindControls();
            var initial = nextScreen ?? InitialScreen;
            nextScreen = null;
            Show(initial);
        }

        // Restrict lookups to the owning panel when button names are shared.
        protected Transform FindNode(string name, Transform root = null) =>
            (root ? root : transform).GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);

        protected T ComponentOf<T>(string name, string scope = null) where T : Component =>
            FindNode(name, scope == null ? transform : FindNode(scope)).GetComponent<T>();

        protected void BindButton(string scope, string name, Action move)
        {
            var root = FindNode(scope);
            if (!root) return;
            foreach (var b in root.GetComponentsInChildren<Button>(true).Where(b => b.name == name))
                b.onClick.AddListener(() => move());
        }

        /// <summary>Show a screen and unwind dialogs belonging to the previous screen.</summary>
        public void Show(string name)
        {
            var destination = screens.Find(name);
            if (!destination) return;
            while (dialogsStack.Count > 0) Close();
            foreach (Transform screen in screens) screen.gameObject.SetActive(screen == destination);
            ScreenName = name;
            OnScreenShown(name);
        }

        public void Open(string name)
        {
            var panel = overlays.Find(name);
            if (!panel || dialogsStack.Contains(panel.gameObject)) return;
            if (dialogsStack.Count > 0) dialogsStack.Peek().SetActive(false);
            dialogsStack.Push(panel.gameObject);
            panel.gameObject.SetActive(true);
            panel.SetAsLastSibling();
            overlays.Find("OrientationOverlay").SetAsLastSibling();
        }

        public void Close()
        {
            if (dialogsStack.Count == 0) return;
            dialogsStack.Pop().SetActive(false);
            if (dialogsStack.Count > 0) dialogsStack.Peek().SetActive(true);
        }

        /// <summary>Load a scene once and deliver its optional initial screen.</summary>
        public void GoTo(string loadedScene, string panel = null)
        {
            if (loadingScene) return;
            loadingScene = true;
            nextScreen = panel;
            StartCoroutine(Load(loadedScene));
        }

        IEnumerator Load(string loadedScene)
        {
            transition.gameObject.SetActive(true);
            var group = transition.GetComponent<CanvasGroup>();
            group.alpha = 0;
            while (group.alpha < 1)
            {
                group.alpha += Time.unscaledDeltaTime * 8;
                yield return null;
            }
            yield return SceneManager.LoadSceneAsync(loadedScene);
        }

        // Bind shared dialogs within their own scope so repeated button names remain independent.
        void BindShared()
        {
            foreach (var button in new[] { "CloseButton", "AcceptButton", "CancelButton", "ConfirmButton" })
                foreach (var panel in new[] { "MessageDialog", "ElementHelpDialog", "ConfirmationDialog", "PreviewDialog" })
                    BindButton(panel, button, Close);
            PreviewOption("PreviewNotice", () => NotifyUser("preview.common"));
            PreviewOption("PreviewConfirm", () => Open("ConfirmationDialog"));
            PreviewOption("PreviewLoading", () => StartCoroutine(ShowTemporary("LoadingOverlay")));
            PreviewOption("PreviewToast", () => StartCoroutine(ShowTemporary("ToastPanel")));
            PreviewOption("ElementGuide", () => Open("ElementHelpDialog"));
            BindButton("Screens", "PreviewButton", () => Open("PreviewDialog"));
        }

        // Use real time so transient overlays can close while gameplay is paused.
        IEnumerator ShowTemporary(string name)
        {
            Open(name);
            yield return new WaitForSecondsRealtime(1.2f);
            if (dialogsStack.Count > 0 && dialogsStack.Peek().name == name) Close();
        }

        protected void PreviewOption(string name, Action show) =>
            BindButton("PreviewDialog", name, () => { Close(); show(); });

        protected void NotifyUser(string key)
        {
            ComponentOf<LocalizedText>("DescriptionLabel", "MessageDialog").Localize(key);
            Open("MessageDialog");
        }
    }
}
