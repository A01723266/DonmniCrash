using UnityEngine;

namespace DonmniCrash.UI
{
    /// <summary>Explicaciones de lectura libre. Los pasos futuros podrán abrir las mismas vistas desde una jugada.</summary>
    public sealed partial class RecorridoTutorial : MesaNavegable
    {
        int paso;
        bool bienvenida = true;
        protected override void SalirMesa() => Ir("OfflineSetting");
        protected override void Conectar()
        {
            base.Conectar();
            Enlazar("TutorialWelcomeDialog","BeginTutorialButton", () => { Cerrar(); Paso(0); });
            Enlazar("TutorialWelcomeDialog","SkipTutorialButton",SalirMesa);
            Enlazar(Inicial,"SkipTutorialButton", () => Abrir("ExitTutorialDialog"));
            Enlazar(Inicial,"PreviousStepButton", () => Paso(paso - 1));
            Enlazar(Inicial,"NextStepButton", () => Paso(paso + 1));
            Enlazar("TutorialHelpDialog","CloseButton",Cerrar);
            Enlazar("ExitTutorialDialog","ContinueButton",Cerrar);
            Enlazar("ExitTutorialDialog","ExitButton",SalirMesa);
            Enlazar("TutorialCompleteDialog","PracticeButton", () => Ir("OfflineSetting","BotConfigurationScreen"));
            Enlazar("TutorialCompleteDialog","RepeatTutorialButton", () => { Cerrar(); Paso(0); });
            Enlazar("TutorialCompleteDialog","BackButton",SalirMesa);
            Opcion("Help", () => { Mostrar(Inicial); Abrir("TutorialHelpDialog"); });
        }
        protected override void AlMostrar(string nombre)
        {
            if (!bienvenida) return;
            bienvenida = false;
            Paso(0);
            Abrir("TutorialWelcomeDialog");
        }
        void Paso(int indice)
        {
            if (indice == 5) { Abrir("TutorialCompleteDialog"); return; }
            paso = Mathf.Clamp(indice,0,4);
            Pieza<TextoIdioma>("StepTitleLabel").Traducir("step.title."+paso);
            Pieza<TextoIdioma>("StepDescriptionLabel").Traducir("step.body."+paso);
            Pieza<UnityEngine.UI.Button>("PreviousStepButton").interactable = paso > 0;
            Buscar("TutorialHighlightOverlay").gameObject.SetActive(paso == 1);
            for (int i=0;i<5;i++)
            {
                var marca = Pieza<UIThemeBinding>("Step"+(i+1)+"Indicator");
                marca.role = i == paso ? UIThemeRole.Accent : UIThemeRole.Border;
                marca.Apply();
            }
        }
    }
}
