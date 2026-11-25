using System;
using System.Collections.Generic;
using System.Linq;

namespace DonmniCrash.Game
{
    public sealed partial class MatchEngine
    {
        int recipient, selectionCount;
        readonly List<GameTile> candidates = new List<GameTile>();
        readonly HashSet<int> picks = new HashSet<int>();
        public bool VisibleSelection => Phase == TurnPhase.SwapSelection && Capacity(ActiveSeat) > Capacity(recipient);
        public int SelectionId { get; private set; }
        public int RequiredChoices => selectionCount;
        public int RemainingChoices => selectionCount - picks.Count;
        public int OptionCount => candidates.Count;
        public bool IsAvailable(int index) => index >= 0 && index < candidates.Count;
        public bool IsMarked(int index) => picks.Contains(index);
        public GameTile? InspectOption(int actor,int index) =>
            actor == ActiveSeat && VisibleSelection && IsAvailable(index) ? candidates[index] : (GameTile?)null;

        void BeginStock(TurnPhase phase,int count)
        {
            Phase = phase;
            SelectionId = Revision + 1;
            selectionCount = count;
            picks.Clear();
            candidates.Clear();
            candidates.AddRange(stock.OpenSelection(count));
        }

        // The acting player keeps their visible excess or draws their capacity from the recipient's hidden hand.
        void BeginSwap(int destination)
        {
            recipient = destination;
            SelectionId = Revision + 1;
            candidates.Clear(); picks.Clear();
            int difference = Capacity(ActiveSeat) - Capacity(recipient);
            Phase = TurnPhase.SwapSelection;
            selectionCount = difference >= 0 ? difference : Capacity(ActiveSeat);
            candidates.AddRange(players[difference >= 0 ? ActiveSeat : recipient].Hand);
            if (difference < 0)
                for (int i = candidates.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
                }
            if (selectionCount == 0) FinishSwap();
        }

        bool ApplySelection(MoveCommand command)
        {
            if (command.Kind == MoveKind.Choose) return Choose(command.Value);
            bool confirm = command.Kind == MoveKind.Confirm;
            if (!confirm && command.Kind != MoveKind.Mark) return false;
            if (Phase.UsesStock() && selectionCount == 1) return false;
            if (command.Marks != null)
            {
                // Continuar incluye la selección completa aunque una marca anterior siga en tránsito.
                var marks = command.Marks;
                if (marks.Length > selectionCount || (confirm && marks.Length != selectionCount) ||
                    marks.Any(i => !IsAvailable(i)) || marks.Distinct().Count() != marks.Length) return false;
                picks.Clear();
                picks.UnionWith(marks);
            }
            return confirm ? ConfirmSelection() : command.Marks != null;
        }

        bool Choose(int index)
        {
            if (!IsAvailable(index)) return false;
            if (picks.Remove(index)) return true;
            if (RemainingChoices <= 0) return false;
            picks.Add(index);
            if (Phase.UsesStock() && selectionCount == 1) return ConfirmSelection();
            return true;
        }

        bool ConfirmSelection()
        {
            if (!Phase.IsSelection() || selectionCount == 0 || RemainingChoices != 0) return false;
            if (Phase == TurnPhase.SwapSelection) FinishSwap();
            else
            {
                var chosen = picks.OrderBy(i => i).Select(i => stock.TakeSelected(candidates[i])).ToArray();
                picks.Clear();
                candidates.Clear();
                if (Phase == TurnPhase.DrawSelection) RevealDraw(chosen[0]);
                else
                {
                    players[ActiveSeat].Hand.AddRange(chosen);
                    if (Phase == TurnPhase.Replacement) Advance();
                    else Phase = TurnPhase.Decision;
                }
            }
            return true;
        }

        void FinishSwap()
        {
            var a = players[ActiveSeat].Hand;
            var b = players[recipient].Hand;
            var taken = picks.Select(i => candidates[i]).ToList();
            if (Capacity(ActiveSeat) >= Capacity(recipient))
            {
                var givenTiles = a.Except(taken).ToArray();
                a.Clear(); a.AddRange(taken); a.AddRange(b);
                b.Clear(); b.AddRange(givenTiles);
            }
            else
            {
                b.RemoveAll(f => taken.Contains(f));
                b.AddRange(a); a.Clear(); a.AddRange(taken);
            }
            candidates.Clear(); picks.Clear();
            Advance();
        }

        /// <summary>Keep the offer on expiry. For selections, fill only the outstanding choices.</summary>
        public void Expire()
        {
            if (Phase == TurnPhase.Finished) return;
            if (Phase == TurnPhase.Decision) BeginStock(TurnPhase.DrawSelection,1);
            else if (Phase == TurnPhase.Offer) Keep();
            else
            {
                var available = Enumerable.Range(0,OptionCount).Where(i => !picks.Contains(i)).ToList();
                while (RemainingChoices > 0)
                {
                    int position = random.Next(available.Count);
                    picks.Add(available[position]);
                    available.RemoveAt(position);
                }
                ConfirmSelection();
            }
            Revision++;
        }
    }
}
