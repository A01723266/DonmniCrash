using System;
using DonmniCrash.Game;
using TMPro;
using UnityEngine;

namespace DonmniCrash.UI
{
    /// <summary>Run a local round and give each bot only its own view.</summary>
    public sealed partial class PracticeTableUI : PlayableTableUI
    {
        DominoBot bot;
        OpeningRollSequence opening;
        double openingStarted;
        protected override bool OpeningBlocksActions => opening != null;
        protected override void LeaveTable() => GoTo("OfflineSetting","BotConfigurationScreen");
        protected override void OnScreenShown(string name)
        {
            if (Match != null) return;
            int seed = Guid.NewGuid().GetHashCode();
            bot = new DominoBot(~seed);
            var match = new MatchEngine(PracticeSettings.Current,seed);
            opening = OpeningRollSequence.Capture(match);
            Begin(match);
            openingStarted = Time.unscaledTimeAsDouble;
            ShowWaitingClock();
            UpdateOpening();
        }

        protected override void RunBot() => SubmitMove(bot.Decide(Match.GetActorView(),Match.Config.Level));

        void LateUpdate()
        {
            if (opening != null) UpdateOpening();
        }

        void UpdateOpening()
        {
            int elapsed = (int)((Time.unscaledTimeAsDouble - openingStarted) * 1000);
            OpeningView.Show(opening,elapsed,LocalSeat,opening.FirstSeat,opening.NextSeat,DisplayName);
            if (elapsed < opening.DurationMilliseconds) return;

            opening = null;
            ResetClock();
            RefreshCurrentTurnCue();
            RenderActions();
        }

        protected override void ExitResults() => GoTo("Start");

        protected override void BindControls()
        {
            base.BindControls();
            BindButton("SpectatorPanel","RestartPracticeButton",Replay);
            BindButton("SpectatorPanel","PracticeMenuButton",ExitResults);
        }

        protected override void Finish() => ShowStandings(T(Match.Rank(0) == 1 ? "game.win" : "game.loss"));
    }
}
