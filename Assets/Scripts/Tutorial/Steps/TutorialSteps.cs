using System.Linq;
using DonmniCrash.Game;

namespace DonmniCrash.UI
{
    /// <summary>Repeatable tutorial positions and objectives for the shared engine.</summary>
    internal static partial class TutorialSteps
    {
        public const int Total = 9;
        public static MatchEngine Prepare(int stepIndex)
        {
            // Fixture identities occupy a separate range from the generated sets.
            int id = 100000;
            GameTile Normal(Element shield) => new GameTile(id++,TileKind.Standard,Element.Earth,shield);
            GameTile WildTile(TileKind kind) => new GameTile(id++,kind);
            var ownHand = new[]{ Normal(Element.Water), Normal(Element.Fire), Normal(Element.Plant) }.ToList();
            var other = new[]{ Normal(Element.Plant), Normal(Element.Water), Normal(Element.Earth) }.ToList();
            if (stepIndex == 4) ownHand[0] = WildTile(TileKind.Reverse);
            if (stepIndex == 5) ownHand[0] = WildTile(TileKind.Pass);
            if (stepIndex == 6 || stepIndex == 7)
            {
                ownHand[1] = WildTile(TileKind.Swap);
                if (stepIndex == 6) ownHand.Add(Normal(Element.Water));
                else other.Add(Normal(Element.Fire));
            }
            if (stepIndex == 8) ownHand.RemoveRange(1,2);
            var hands = new[]{ownHand.ToArray(),other.ToArray(),new[]{Normal(Element.Water),Normal(Element.Wind),Normal(Element.Plant)}};
            return MatchEngine.FromPosition(hands,new[]{stepIndex==8 ? 3 : ownHand.Count,other.Count,3},
                stepIndex==0 ? (Element?)null : Element.Fire,Normal(Element.Water));
        }

        public static bool Allows(int stepIndex,MatchEngine engine,MoveCommand a)
        {
            if (engine.Phase == TurnPhase.SwapSelection || engine.Phase == TurnPhase.Refill) return a.Kind == MoveKind.Choose;
            if (stepIndex == 2 || stepIndex == 3)
                return engine.Phase == TurnPhase.Decision ? a.Kind == MoveKind.Draw :
                    a.Kind == (stepIndex == 2 ? MoveKind.Play : MoveKind.Keep);
            if (a.Kind != MoveKind.Play) return false;
            var tile = engine.HandOf(0).FirstOrDefault(f=>f.Id == a.Value);
            if (stepIndex == 4) return tile.Kind == TileKind.Reverse;
            if (stepIndex == 5) return tile.Kind == TileKind.Pass;
            if (stepIndex == 6 || stepIndex == 7) return a.Target == 1;
            return tile.Kind == TileKind.Standard;
        }
    }
}
