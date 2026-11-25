using System;
using DonmniCrash.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    public sealed partial class OpeningRollView : MonoBehaviour
    {
        public TMP_Text title, summary;
        public GameObject diceRow;
        public Image[] dice;
        public TMP_Text[] names;
        public Sprite[] redFaces, whiteFaces;

        OpeningRollSequence lastSequence;
        int lastStage = int.MinValue, lastFirst, lastNext, lastLocalSeat;
        bool lastLanguage;

        public void Show(OpeningRollSequence sequence,int elapsedMilliseconds,int localSeat,int firstSeat,int nextSeat,Func<int,string> name)
        {
            if (sequence == null || elapsedMilliseconds < 0)
            {
                ShowWaiting(sequence == null ? "game.dice.waiting" : "game.dice.ready");
                return;
            }
            if (elapsedMilliseconds >= sequence.DurationMilliseconds)
            {
                Hide();
                return;
            }

            EnsureVisible();
            int stage = elapsedMilliseconds / OpeningRollSequence.StageMilliseconds;
            bool english = SessionAppearance.English;
            if (lastSequence != sequence || lastStage != stage || lastFirst != firstSeat ||
                lastNext != nextSeat || lastLocalSeat != localSeat || lastLanguage != english)
            {
                lastSequence = sequence;
                lastStage = stage;
                lastFirst = firstSeat;
                lastNext = nextSeat;
                lastLocalSeat = localSeat;
                lastLanguage = english;
                if (stage == sequence.Rolls.Count) ShowSummary(firstSeat,nextSeat,name);
                else ShowDice(sequence,stage,localSeat,name);
            }

            if (stage < sequence.Rolls.Count)
            {
                float settle = Mathf.Clamp01((elapsedMilliseconds % OpeningRollSequence.StageMilliseconds) / 180f);
                float scale = Mathf.Lerp(.8f,1f,Mathf.SmoothStep(0,1,settle));
                foreach (var die in dice)
                    if (die.gameObject.activeInHierarchy) die.rectTransform.localScale = Vector3.one * scale;
            }
        }

        void ShowWaiting(string key)
        {
            EnsureVisible();
            lastStage = int.MinValue;
            title.text = LocalizedText.Read("game.dice");
            summary.text = LocalizedText.Read(key);
            summary.gameObject.SetActive(true);
            diceRow.SetActive(false);
        }

        void ShowDice(OpeningRollSequence sequence,int stage,int localSeat,Func<int,string> name)
        {
            string key = stage == 0 ? "game.dice.first" :
                stage == sequence.DirectionRollStart ? "game.dice.direction" :
                stage < sequence.DirectionRollStart ? "game.dice.tie.first" : "game.dice.tie.direction";
            title.text = LocalizedText.Read(key);
            summary.gameObject.SetActive(false);
            diceRow.SetActive(true);
            var row = sequence.Rolls[stage];
            for (int i = 0; i < dice.Length; i++)
            {
                bool visible = i * 2 < row.Length;
                dice[i].transform.parent.gameObject.SetActive(visible);
                if (!visible) continue;
                int seat = row[2 * i];
                int face = row[2 * i + 1] - 1;
                bool own = seat == localSeat;
                dice[i].sprite = (own ? redFaces : whiteFaces)[face];
                names[i].text = own ? LocalizedText.Read("game.seat.you") : name(seat);
            }
        }

        void ShowSummary(int first,int next,Func<int,string> name)
        {
            title.text = LocalizedText.Read("game.dice.result");
            summary.text = string.Format(LocalizedText.Read("game.dice.next"),name(first),name(next));
            summary.gameObject.SetActive(true);
            diceRow.SetActive(false);
        }

        void EnsureVisible()
        {
            if (gameObject.activeSelf) return;
            transform.SetAsLastSibling();
            var orientation = transform.parent.Find("OrientationOverlay");
            if (orientation) orientation.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            lastStage = int.MinValue;
        }
    }
}
