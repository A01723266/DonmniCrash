using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    /// <summary>La UI de mesa ofrece ayuda, selección visual y diálogos reutilizables para los tres modos.</summary>
    public abstract partial class MesaNavegable : NavegacionUI
    {
        protected override string Inicial => "MatchScreen";
        protected abstract void SalirMesa();
        protected virtual bool MesaInteractiva => false;
        protected virtual void Repetir() => Ir(gameObject.scene.name);
        protected virtual void RegresarResultado() => SalirMesa();

        protected override void Conectar()
        {
            Enlazar(Inicial,"ElementHelpButton", () => Abrir("ElementHelpDialog"));
            Enlazar(Inicial,"PauseButton", () => Abrir("PauseDialog"));
            Enlazar("PauseDialog","ResumeButton",Cerrar);
            Enlazar("PauseDialog","SettingsButton", () => Abrir("SettingsDialog"));
            Enlazar("PauseDialog","ElementHelpButton", () => Abrir("ElementHelpDialog"));
            Enlazar("PauseDialog","LeaveMatchButton", () => Abrir("LeaveMatchDialog"));
            Enlazar("PauseDialog","PreviewButton", () => Abrir("PreviewDialog"));
            Enlazar("SettingsDialog","CloseSettingsButton",Cerrar);
            Enlazar("LeaveMatchDialog","StayButton",Cerrar);
            Enlazar("LeaveMatchDialog","LeaveButton",SalirMesa);
            Enlazar("ResultsDialog","RematchButton",Repetir);
            Enlazar("ResultsDialog","ReturnButton",RegresarResultado);
            if (MesaInteractiva) return;
            Enlazar("TargetSelectionDialog","CancelButton",Cerrar);
            for (int i = 1; i <= 5; i++) Enlazar("TargetSelectionDialog","TargetPlayer"+i+"Button",Cerrar);
            foreach (var n in new[] {"PlayTileButton","DrawTileButton","PassTurnButton"})
                Enlazar(Inicial,n, () => Avisar("preview.game"));
            var fichas = Buscar("HandScrollView").GetComponentsInChildren<DominoTileView>(true);
            foreach (var ficha in fichas)
                ficha.GetComponent<Button>().onClick.AddListener(() => {
                    foreach (var otra in fichas) otra.selectionOutline.SetActive(otra == ficha);
                });
            Opcion("Results", () => { Mostrar(Inicial); Abrir("ResultsDialog"); });
            Opcion("Target", () => { Mostrar(Inicial); Abrir("TargetSelectionDialog"); });
        }
    }
}
