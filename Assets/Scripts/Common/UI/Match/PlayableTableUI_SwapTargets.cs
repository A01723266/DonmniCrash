using System.Linq;
using DonmniCrash.Game;
using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    public abstract partial class PlayableTableUI
    {
        bool swapEnabled, choosingTarget;
        GameObject targetPanel;
        readonly PlayerHudCard[] targetCards = new PlayerHudCard[5];
        readonly int[] targetSeats = new int[5];
        int targetCount;

        void BindSwapTargets()
        {
            targetPanel = FindNode("TargetSelectionDialog").gameObject;
            for (int i=0;i<targetCards.Length;i++)
            {
                int slot = i;
                var button = ComponentOf<Button>("TargetPlayer"+(i+1)+"Button",targetPanel.name);
                targetCards[i] = button.GetComponentInChildren<PlayerHudCard>(true);
                button.onClick.AddListener(() => SelectTarget(slot));
            }
            BindButton(targetPanel.name,"CancelButton",() => { choosingTarget=false; RenderActions(); });
            BindButton(targetPanel.name,"RandomRivalButton",() =>
            {
                if (targetCount > 0) SelectTarget(Random.Range(0,targetCount));
            });
        }

        void ResetSelection()
        {
            selectedId = -1;
            swapEnabled = choosingTarget = false;
        }

        void PlaceSelected()
        {
            if (!IsLocalTurn || InteractionBlocked) return;
            if (!swapEnabled) SubmitMove(new MoveCommand(MoveKind.Play,selectedId));
            else { choosingTarget = true; RenderActions(); }
        }

        void SelectTarget(int slot)
        {
            if (!choosingTarget || slot >= targetCount || !IsLocalTurn || InteractionBlocked) return;
            int seat = targetSeats[slot];
            if (!Display.CanTarget(seat)) { RenderActions(); return; }
            choosingTarget = false;
            targetPanel.SetActive(false);
            SubmitMove(new MoveCommand(MoveKind.Play,selectedId,seat));
        }

        void RefreshSwapTargets(bool available)
        {
            if (!available) swapEnabled = choosingTarget = false;
            targetPanel.SetActive(choosingTarget);
            if (!choosingTarget) return;
            targetCount = 0;
            foreach (int seat in Display.Order.Where(Display.CanTarget))
            {
                if (!AllowMove(new MoveCommand(MoveKind.Play,selectedId,seat))) continue;
                targetSeats[targetCount++] = seat;
            }
            if (targetCount == 0) { choosingTarget=false; targetPanel.SetActive(false); return; }
            for (int i=0;i<targetCards.Length;i++)
            {
                var card = targetCards[i];
                card.transform.parent.gameObject.SetActive(i < targetCount);
                if (i >= targetCount) continue;
                int seat = targetSeats[i];
                card.Show(DisplayName(seat),symbols[Emblem(seat)],Display.Health(seat),StartingHealth,
                    Display.HandCount(seat),false,false,false);
            }
        }
    }
}
