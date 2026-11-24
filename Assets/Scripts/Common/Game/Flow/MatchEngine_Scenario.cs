using System.Linq;

namespace DonmniCrash.Game
{
    public sealed partial class MatchEngine
    {
        /// <summary>Create a fixed tutorial position. Subsequent moves use the regular engine.</summary>
        internal static MatchEngine FromPosition(GameTile[][] hands,int[] capacities,Element? incomingAttack,GameTile next)
        {
            var engine = new MatchEngine(new MatchRules(players:hands.Length),1234);
            engine.orden.Clear();
            engine.orden.AddRange(Enumerable.Range(0,hands.Length));
            engine.ActiveSeat = 0;
            engine.Direction = 1;
            engine.Attack = incomingAttack;
            if (incomingAttack.HasValue) engine.board.Add(new GameTile(-10,TileKind.Standard,incomingAttack.Value,Element.Plant));
            for (int i=0;i<hands.Length;i++)
            {
                engine.players[i].Hand.Clear();
                engine.players[i].Hand.AddRange(hands[i]);
                engine.players[i].Capacity = capacities[i];
            }
            engine.stock.SetNextDraw(next);
            engine.Phase = engine.MissingCount > 0 ? TurnPhase.Refill : TurnPhase.Decision;
            return engine;
        }
    }
}
