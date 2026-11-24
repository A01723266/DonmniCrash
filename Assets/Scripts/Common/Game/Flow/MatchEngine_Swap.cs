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
        public int RemainingChoices => Phase == TurnPhase.Refill ? MissingCount : selectionCount - picks.Count;
        public int OptionCount => Phase == TurnPhase.Refill ? stock.Count : candidates.Count;
        public bool IsAvailable(int index) => index >= 0 && index < OptionCount &&
            (Phase == TurnPhase.Refill || !picks.Contains(index));
        public GameTile? InspectOption(int actor,int index) =>
            actor == ActiveSeat && VisibleSelection && IsAvailable(index) ? candidates[index] : (GameTile?)null;

        // The acting player keeps their visible excess or draws their capacity from the recipient's hidden hand.
        void BeginSwap(int destination)
        {
            recipient = destination;
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

        bool Choose(int index)
        {
            if (!IsAvailable(index)) return false;
            if (Phase == TurnPhase.Refill)
            {
                players[ActiveSeat].Hand.Add(stock.TakeTile(index));
                if (MissingCount == 0) Phase = TurnPhase.Decision;
                return true;
            }
            picks.Add(index);
            if (RemainingChoices == 0) FinishSwap();
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
            if (Phase == TurnPhase.Decision) Draw();
            else if (Phase == TurnPhase.Offer) Keep();
            else
            {
                var phase = Phase;
                int actor = ActiveSeat;
                while (Phase == phase && ActiveSeat == actor)
                {
                    var indices = Enumerable.Range(0,OptionCount).Where(IsAvailable).ToArray();
                    Choose(indices[random.Next(indices.Length)]);
                }
            }
            Revision++;
        }
    }
}
