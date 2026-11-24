using DonmniCrash.Game;

namespace DonmniCrash.UI
{
    /// <summary>Edit local rules and deliver the confirmed configuration at match start.</summary>
    public sealed partial class PracticeMenu : CanvasNavigation
    {
        protected override string InitialScreen => "ActivitySelectionScreen";
        const string SetupForm = "BotConfigurationScreen", Summary = "ConfigurationSummaryScreen";
        MatchRules draft;
        OptionSelector Selector(string name) => ComponentOf<OptionSelector>(name,SetupForm);

        protected override void BindControls()
        {
            BindButton(InitialScreen,"BackButton", () => GoTo("Start"));
            BindButton(InitialScreen,"TutorialButton", () => GoTo("Tutorial"));
            BindButton(InitialScreen,"BotConfigurationButton", () => Show(SetupForm));
            BindButton(SetupForm,"BackButton", () => Show(InitialScreen));
            BindButton(SetupForm,"ContinueButton",Summarize);
            BindButton(Summary,"BackButton", () => Show(SetupForm));
            BindButton(Summary,"EditConfigurationButton", () => Show(SetupForm));
            BindButton(Summary,"StartMatchButton", () => { PracticeSettings.Confirm(draft); GoTo("OfflineMatch"); });

            var c = PracticeSettings.Current;
            Selector("PlayerCountSelector").Choose(c.Players - 2);
            Selector("StartingHandSelector").Choose(c.Hand - 2);
            Selector("TurnTimeSelector").Choose(c.Seconds - 1);
            Selector("StartingHealthSelector").Choose(c.Health - 1);
            Selector("ReverseSelector").Choose(c.Reverses);
            Selector("PassSelector").Choose(c.Passes);
            Selector("SwapSelector").Choose(c.Swaps);
            Selector("BotDifficultySelector").Choose((int)c.Level);
        }

        // The limit applies to the combined wild tile count. Keep invalid input editable.
        void Summarize()
        {
            int Number(string id) => int.Parse(Selector(id).ValueText);
            int reversas = Number("ReverseSelector"), pases = Number("PassSelector"), swaps = Number("SwapSelector");
            if (reversas + pases + swaps > 8) { NotifyUser("game.limit"); return; }
            draft = new MatchRules(Number("PlayerCountSelector"),Number("StartingHandSelector"),
                Number("StartingHealthSelector"),Number("TurnTimeSelector"),reversas,pases,swaps,
                (BotLevel)Selector("BotDifficultySelector").index);
            Show(Summary);
            ComponentOf<LocalizedText>("PlayersLabel",Summary).Localize("game.players",draft.Players-1);
            var level = LocalizedText.KeyForText(Selector("BotDifficultySelector").ValueText);
            ComponentOf<LocalizedText>("DifficultyLabel",Summary).Localize("practice.level",LocalizedText.Read(level));
            ComponentOf<LocalizedText>("RulesLabel",Summary).Localize("game.rules",draft.Health,draft.Hand,
                draft.Seconds,reversas,pases,swaps);
        }
    }
}
