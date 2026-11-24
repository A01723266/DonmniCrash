using System.Collections.Generic;
using System.Linq;

namespace DonmniCrash.Game
{
    /// <summary>Public board, private hand and legal choices visible to one seat.</summary>
    public sealed partial class ActorView
    {
        public GameTile[] Hand, Board;
        public GameTile? Offer;
        public Element? Attack;
        public int Actor, NextSeat, PreviousSeat;
        public int[] HealthBySeat, HandSizes;
        public MoveCommand[] Options;
        public GameTile?[] Selecting;
    }
}
