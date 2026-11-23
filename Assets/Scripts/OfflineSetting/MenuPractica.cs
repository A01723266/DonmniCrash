using UnityEngine;

namespace DonmniCrash.UI
{
    /// <summary>Conserva los selectores al ir al resumen o al volver desde la mesa de muestra.</summary>
    public sealed partial class MenuPractica : NavegacionUI
    {
        protected override string Inicial => "ActivitySelectionScreen";
        const string Formulario = "BotConfigurationScreen", Resumen = "ConfigurationSummaryScreen";
        static int[] ultimaSeleccion;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar() => ultimaSeleccion = null;

        protected override void Conectar()
        {
            Enlazar(Inicial,"BackButton", () => Ir("Start"));
            Enlazar(Inicial,"TutorialButton", () => Ir("Tutorial"));
            Enlazar(Inicial,"BotConfigurationButton", () => Mostrar(Formulario));
            Enlazar(Formulario,"BackButton", () => Mostrar(Inicial));
            Enlazar(Formulario,"ContinueButton", () => Mostrar(Resumen));
            Enlazar(Resumen,"BackButton", () => Mostrar(Formulario));
            Enlazar(Resumen,"EditConfigurationButton", () => Mostrar(Formulario));
            Enlazar(Resumen,"StartMatchButton", () => {
                var opciones = Buscar(Formulario).GetComponentsInChildren<OpcionesMuestra>(true);
                ultimaSeleccion = System.Array.ConvertAll(opciones, o => o.indice);
                Ir("OfflineMatch");
            });
            var controles = Buscar(Formulario).GetComponentsInChildren<OpcionesMuestra>(true);
            if (ultimaSeleccion?.Length == controles.Length)
                for (int i = 0; i < controles.Length; i++) controles[i].Elegir(ultimaSeleccion[i]);
        }

        protected override void AlMostrar(string panel)
        {
            if (panel != Resumen) return;
            string Valor(string n) => Pieza<OpcionesMuestra>(n).Valor;
            Pieza<TextoIdioma>("PlayersLabel",Resumen).Traducir("practice.players", Valor("PlayerCountSelector"));
            Pieza<TextoIdioma>("DifficultyLabel",Resumen).Traducir("practice.level", TextoIdioma.Leer(TextoIdioma.ClaveOriginal(Valor("BotDifficultySelector"))));
            Pieza<TextoIdioma>("RulesLabel",Resumen).Traducir("practice.rules",Valor("StartingHealthSelector"),Valor("StartingHandSelector"),Valor("TurnTimeSelector"),TextoIdioma.Leer(TextoIdioma.ClaveOriginal(Valor("EffectsSelector"))));
        }
    }
}
