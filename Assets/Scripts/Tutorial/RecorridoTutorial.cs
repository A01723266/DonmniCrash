using DonmniCrash.Juego;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    /// <summary>Enseña con el tablero real. El usuario controla el ritmo y puede repetir cada posición.</summary>
    public sealed partial class RecorridoTutorial : MesaJugable
    {
        int paso;
        bool completo;
        protected override bool Guiada => true;
        protected override bool BloquearAcciones => completo;
        protected override void SalirMesa() => Ir("OfflineSetting");
        protected override void Conectar()
        {
            base.Conectar();
            Enlazar("TutorialWelcomeDialog","BeginTutorialButton",Cerrar);
            Enlazar("TutorialWelcomeDialog","SkipTutorialButton",SalirMesa);
            Enlazar(Inicial,"SkipTutorialButton", () => Abrir("ExitTutorialDialog"));
            Enlazar(Inicial,"PreviousStepButton", () => Paso(paso - 1));
            Enlazar(Inicial,"RetryStepButton", () => Paso(paso));
            Enlazar(Inicial,"NextStepButton", () => {
                if (!completo) return;
                if (paso+1 == PasosTutorial.Total) Abrir("TutorialCompleteDialog");
                else Paso(paso+1);
            });
            Enlazar("ExitTutorialDialog","ContinueButton",Cerrar);
            Enlazar("ExitTutorialDialog","ExitButton",SalirMesa);
            Enlazar("TutorialCompleteDialog","PracticeButton", () => Ir("OfflineSetting","BotConfigurationScreen"));
            Enlazar("TutorialCompleteDialog","RepeatTutorialButton", () => { Cerrar(); Paso(0); });
            Enlazar("TutorialCompleteDialog","BackButton",SalirMesa);
        }
        protected override void AlMostrar(string nombre)
        {
            if (Partida != null) return;
            Paso(0);
            Abrir("TutorialWelcomeDialog");
        }
        protected override bool Permitir(Accion a) => PasosTutorial.Permite(paso,Partida,a);
        protected override void AlAceptar(Accion a)
        {
            completo = Partida.Activo != 0 || a.Tipo == Movimiento.Conservar ||
                (paso == 8 && Partida.Etapa == Fase.Decision);
            PintarGuia();
        }
        void Paso(int indice)
        {
            paso = indice;
            completo = false;
            Iniciar(PasosTutorial.Preparar(paso));
            PintarGuia();
        }
        protected override void AlCambiarIdioma() => PintarGuia();
        void PintarGuia()
        {
            Etiquetar("StepTitleLabel",(paso+1)+" / "+PasosTutorial.Total+" · "+T("lesson.title."+paso));
            Etiquetar("StepDescriptionLabel",T(completo ? "lesson.done" : "lesson.body."+paso));
            Pieza<Button>("PreviousStepButton").interactable = paso > 0;
            Pieza<Button>("NextStepButton").interactable = completo;
            for (int i=0;i<PasosTutorial.Total;i++)
            {
                var marca = Pieza<UIThemeBinding>("Step"+(i+1)+"Indicator");
                marca.role = i == paso ? UIThemeRole.Accent : UIThemeRole.Border;
                marca.Apply();
            }
        }
    }
}
