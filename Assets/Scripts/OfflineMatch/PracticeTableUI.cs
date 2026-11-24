using System;
using System.Linq;
using System.Text;
using DonmniCrash.Game;
using TMPro;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    /// <summary>Run a local round and give each bot only its own view.</summary>
    public sealed partial class PracticeTableUI : PlayableTableUI
    {
        DominoBot bot;
        protected override void LeaveTable() => GoTo("OfflineSetting","BotConfigurationScreen");
        protected override void OnScreenShown(string name)
        {
            if (Match != null) return;
            int seed = Guid.NewGuid().GetHashCode();
            bot = new DominoBot(~seed);
            Begin(new MatchEngine(PracticeSettings.Current,seed));
            ShowDice();
        }

        protected override void RunBot() => SubmitMove(bot.Decide(Match.GetActorView(),Match.Config.Level));

        // Include all tie-break rolls in their original order.
        void ShowDice()
        {
            var labelText = new StringBuilder();
            for (int i=0;i<Match.Rolls.Count;i++)
            {
                var row = Match.Rolls[i];
                labelText.AppendLine(T(i < Match.NeighborRollStart ? "game.roll" : "game.direction") + " · " +
                    string.Join(" / ",Enumerable.Range(0,row.Length/2).Select(n=>DisplayName(row[n*2])+" "+row[n*2+1])));
            }
            labelText.Append(T("game.first",DisplayName(Match.ActiveSeat),DisplayName(Match.Neighbor(Match.Direction))));
            var label = ComponentOf<TMP_Text>("DescriptionLabel","MessageDialog");
            WriteLabel(label,labelText.ToString());
            label.GetComponent<LayoutElement>().preferredHeight = label.GetPreferredValues(label.text,400,0).y + 12;
            SetLabel("TitleLabel",T("game.dice"),"MessageDialog");
            Open("MessageDialog");
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
                // Several bots can survive when the human loses. Report health instead of assigning an imaginary ranking.
                int actor = Match.Order[i];
                WriteLabel(FindNode("RankLabel",currentRow).GetComponent<TMP_Text>(),"");
                WriteLabel(FindNode("PlayerLabel",currentRow).GetComponent<TMP_Text>(),DisplayName(actor));
                WriteLabel(FindNode("ScoreLabel",currentRow).GetComponent<TMP_Text>(),T("game.health",Match.Health(actor)));
            }
            Open("ResultsDialog");
        }
    }
}
