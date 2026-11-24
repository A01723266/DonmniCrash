using System;
using System.Linq;
using DonmniCrash.Game;

namespace DonmniCrash.UI
{
    /// <summary>Choose from the same information and legal actions available to the current seat.</summary>
    public sealed partial class DominoBot
    {
        readonly Random random;
        public DominoBot(int seed) { random = new Random(seed); }
        public MoveCommand Decide(ActorView v, BotLevel level)
        {
            if (level == BotLevel.Easy) return v.Options[random.Next(v.Options.Length)];
            // Break ties between equivalent moves and indistinguishable hidden tiles.
            return v.Options.OrderByDescending(a => Score(v,a,level) + random.NextDouble() * .8).First();
        }
        double Score(ActorView v, MoveCommand a, BotLevel level)
        {
            if (a.Kind == MoveKind.Draw || a.Kind == MoveKind.Keep) return -10;
            if (a.Kind == MoveKind.Choose)
                return level == BotLevel.Hard && v.Selecting[a.Value]?.Kind == TileKind.Swap ? 1 : 0;
            if (level == BotLevel.Normal) return a.Target < 0 ? 2 : 1;
            var f = v.Offer ?? v.Hand.First(x => x.Id == a.Value);
            int rival = f.Kind == TileKind.Reverse ? v.PreviousSeat : v.NextSeat;
            double points = 4.0 / Math.Max(1,v.HealthBySeat[rival]);
            if (f.Kind == TileKind.Standard)
                points += v.Board.Count(t => t.Kind == TileKind.Standard && TileRules.Beats(t.Shield,f.Sword)) * .05;
            if (a.Target >= 0) points += (v.HandSizes[a.Target] - v.Hand.Length) * .4;
            return points;
        }
    }
}
