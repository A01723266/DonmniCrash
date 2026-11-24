using System.Collections.Generic;
using System.Linq;

namespace DonmniCrash.Game
{
    // Expose only information visible to the active seat.
    public sealed partial class MatchEngine
    {
        public ActorView GetActorView()
        {
            var hand = HandOf(ActiveSeat);
            var options = new List<MoveCommand>();
            GameTile?[] selection = null;
            if (Phase == TurnPhase.Decision)
            {
                options.Add(new MoveCommand(MoveKind.Draw));
                foreach (var tile in hand.Where(f => TileRules.IsPlayable(f,Attack)))
                {
                    options.Add(new MoveCommand(MoveKind.Play,tile.Id));
                    if (tile.Kind == TileKind.Standard && hand.Any(f => f.Kind == TileKind.Swap))
                        foreach (int id in orden.Where(CanTarget))
                            options.Add(new MoveCommand(MoveKind.Play,tile.Id,id));
                }
            }
            else if (Phase == TurnPhase.Offer)
            {
                options.Add(new MoveCommand(MoveKind.Play,offer.Value.Id));
                options.Add(new MoveCommand(MoveKind.Keep));
            }
            else if (Phase == TurnPhase.Refill || Phase == TurnPhase.SwapSelection)
            {
                selection = new GameTile?[OptionCount];
                for (int i = 0; i < selection.Length; i++)
                    if (IsAvailable(i))
                    {
                        selection[i] = InspectOption(ActiveSeat,i);
                        options.Add(new MoveCommand(MoveKind.Choose,i));
                    }
            }
            return new ActorView {
                Actor=ActiveSeat, NextSeat=Neighbor(Direction), PreviousSeat=Neighbor(-Direction), Attack=Attack,
                Hand=hand, Board=board.ToArray(), Offer=offer, Options=options.ToArray(), Selecting=selection,
                HealthBySeat=players.Select(j=>j.Health).ToArray(), HandSizes=players.Select(j=>j.Hand.Count).ToArray()
            };
        }
    }
}
