using System.Linq;

namespace DonmniCrash.Game
{
    public sealed partial class MatchEngine
    {
        /// <summary>Create a fixed tutorial position. Subsequent moves use the regular engine.</summary>
        internal static MatchEngine FromPosition(GameTile[][] hands,int[] capacities,Element? incomingAttack,GameTile? lessonDraw, GameTile[] history = null)
        {
            var engine = new MatchEngine(new MatchRules(players:hands.Length),1234);
            engine.orden.Clear();
            engine.orden.AddRange(Enumerable.Range(0,hands.Length));
            engine.ActiveSeat = 0;
            engine.Direction = 1;
            engine.Attack = incomingAttack;
            if (history != null) engine.board.AddRange(history);
            else if (incomingAttack.HasValue) engine.board.Add(new GameTile(-10,TileKind.Standard,incomingAttack.Value,Element.Plant));
            for (int i=0;i<hands.Length;i++)
            {
                engine.players[i].Hand.Clear();
                engine.players[i].Hand.AddRange(hands[i]);
                engine.players[i].Capacity = capacities[i];
            }
            if (lessonDraw is GameTile next)
                engine.stock.SetScenarioStock(Enumerable.Range(0,12).Select(i => new GameTile(next.Id+i,next.Kind,next.Sword,next.Shield)));
            if (engine.MissingCount > 0) engine.BeginStock(TurnPhase.Refill,engine.MissingCount);
            return engine;
        }
    }
}
