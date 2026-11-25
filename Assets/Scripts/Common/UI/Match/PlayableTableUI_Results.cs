using System.Linq;
using TMPro;
using UnityEngine;

namespace DonmniCrash.UI
{
    public abstract partial class PlayableTableUI
    {
        bool standingsShown;

        protected void ShowStandings(string headline)
        {
            standingsShown = true;
            var panel = FindNode("ResultsDialog");
            SetLabel("WinnerLabel",headline,"ResultsDialog");
            var seats = Display.Order.OrderBy(Display.Rank).ToArray();
            var first = FindNode("ResultRow1",panel);
            for (int i = 0; i < 6; i++)
            {
                string name = "ResultRow"+(i+1);
                var row = FindNode(name,panel);
                if (i >= seats.Length)
                {
                    if (row) row.gameObject.SetActive(false);
                    continue;
                }
                if (!row)
                {
                    row = Instantiate(first,first.parent);
                    row.name = name;
                    row.SetSiblingIndex(first.GetSiblingIndex()+i);
                }
                int seat = seats[i];
                row.gameObject.SetActive(true);
                WriteLabel(FindNode("RankLabel",row).GetComponent<TMP_Text>(),T("results.rank",Display.Rank(seat)));
                WriteLabel(FindNode("PlayerLabel",row).GetComponent<TMP_Text>(),DisplayName(seat));
                WriteLabel(FindNode("ScoreLabel",row).GetComponent<TMP_Text>(),T("game.health",Display.Health(seat)));
            }
            Open("ResultsDialog");
        }
    }
}
