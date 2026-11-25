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

        protected override void Finish()
        {
            bool victory = Match.Health(0) > 0;
            SetLabel("WinnerLabel",T(victory ? "game.win" : "game.loss"),"ResultsDialog");
            var row = FindNode("ResultRow1",FindNode("ResultsDialog"));
            for (int i=0;i<6;i++)
            {
                string id = "ResultRow"+(i+1);
                var currentRow = FindNode(id,FindNode("ResultsDialog"));
                if (!currentRow && i < Match.Order.Count)
                {
                    currentRow = Instantiate(row,row.parent);
                    currentRow.name = id;
                    currentRow.SetSiblingIndex(row.GetSiblingIndex()+i);
                }
                if (!currentRow) continue;
                currentRow.gameObject.SetActive(i < Match.Order.Count);
                if (!currentRow.gameObject.activeSelf) continue;
                int actor = Match.Order[i];
                WriteLabel(FindNode("RankLabel",currentRow).GetComponent<TMP_Text>(),"");
                WriteLabel(FindNode("PlayerLabel",currentRow).GetComponent<TMP_Text>(),DisplayName(actor));
                WriteLabel(FindNode("ScoreLabel",currentRow).GetComponent<TMP_Text>(),T("game.health",Match.Health(actor)));
            }
            Open("ResultsDialog");
        }
    }
}
