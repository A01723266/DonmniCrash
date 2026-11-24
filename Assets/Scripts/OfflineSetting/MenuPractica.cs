using DonmniCrash.Juego;

namespace DonmniCrash.UI
{
    /// <summary>Edita una configuración local en el formulario y la entrega al motor al comenzar.</summary>
    public sealed partial class MenuPractica : NavegacionUI
    {
        protected override string Inicial => "ActivitySelectionScreen";
        const string Formulario = "BotConfigurationScreen", Resumen = "ConfigurationSummaryScreen";
        ConfiguracionPartida borrador;
        OpcionesMuestra Selector(string nombre) => Pieza<OpcionesMuestra>(nombre,Formulario);

        protected override void Conectar()
        {
            Enlazar(Inicial,"BackButton", () => Ir("Start"));
            Enlazar(Inicial,"TutorialButton", () => Ir("Tutorial"));
            Enlazar(Inicial,"BotConfigurationButton", () => Mostrar(Formulario));
            Enlazar(Formulario,"BackButton", () => Mostrar(Inicial));
            Enlazar(Formulario,"ContinueButton",Resumir);
            Enlazar(Resumen,"BackButton", () => Mostrar(Formulario));
            Enlazar(Resumen,"EditConfigurationButton", () => Mostrar(Formulario));
            Enlazar(Resumen,"StartMatchButton", () => { OpcionesPractica.Confirmar(borrador); Ir("OfflineMatch"); });

            var c = OpcionesPractica.Actual;
            Selector("PlayerCountSelector").Elegir(c.Jugadores - 2);
            Selector("StartingHandSelector").Elegir(c.Mano - 2);
            Selector("TurnTimeSelector").Elegir(c.Segundos - 1);
            Selector("StartingHealthSelector").Elegir(c.Vida - 1);
            Selector("ReverseSelector").Elegir(c.Reversas);
            Selector("PassSelector").Elegir(c.Pases);
            Selector("SwapSelector").Elegir(c.Cambios);
            Selector("BotDifficultySelector").Elegir((int)c.Nivel);
        }

        // El límite es sobre la suma. Un error deja el formulario y sus valores editables.
        void Resumir()
        {
            int Numero(string id) => int.Parse(Selector(id).Valor);
            int reversas = Numero("ReverseSelector"), pases = Numero("PassSelector"), cambios = Numero("SwapSelector");
            if (reversas + pases + cambios > 8) { Avisar("game.limit"); return; }
            borrador = new ConfiguracionPartida(Numero("PlayerCountSelector"),Numero("StartingHandSelector"),
                Numero("StartingHealthSelector"),Numero("TurnTimeSelector"),reversas,pases,cambios,
                (NivelBot)Selector("BotDifficultySelector").indice);
            Mostrar(Resumen);
            Pieza<TextoIdioma>("PlayersLabel",Resumen).Traducir("game.players",borrador.Jugadores-1);
            var nivel = TextoIdioma.ClaveOriginal(Selector("BotDifficultySelector").Valor);
            Pieza<TextoIdioma>("DifficultyLabel",Resumen).Traducir("practice.level",TextoIdioma.Leer(nivel));
            Pieza<TextoIdioma>("RulesLabel",Resumen).Traducir("game.rules",borrador.Vida,borrador.Mano,
                borrador.Segundos,reversas,pases,cambios);
        }
    }
}
