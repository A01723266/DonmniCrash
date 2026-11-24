namespace DonmniCrash.Game
{
    /// <summary>Single elemental matchup table for the engine, bots and help panel.</summary>
    public static partial class TileRules
    {
        public static bool Beats(Element shield, Element incomingAttack)
        {
            switch (shield)
            {
                case Element.Fire: return incomingAttack == Element.Plant || incomingAttack == Element.Wind;
                case Element.Wind: return incomingAttack == Element.Plant || incomingAttack == Element.Earth;
                case Element.Plant: return incomingAttack == Element.Water || incomingAttack == Element.Earth;
                case Element.Earth: return incomingAttack == Element.Fire || incomingAttack == Element.Water;
                case Element.Water: return incomingAttack == Element.Fire || incomingAttack == Element.Wind;
                default: return false;
            }
        }

        public static bool IsPlayable(GameTile tile, Element? incomingAttack)
        {
            if (tile.Kind == TileKind.Standard) return !incomingAttack.HasValue || Beats(tile.Shield, incomingAttack.Value);
            return incomingAttack.HasValue && (tile.Kind == TileKind.Reverse || tile.Kind == TileKind.Pass);
        }
    }
}
