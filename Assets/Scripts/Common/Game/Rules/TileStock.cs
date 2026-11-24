using System;
using System.Collections.Generic;

namespace DonmniCrash.Game
{
    /// <summary>Hidden stock sampled without replacement. Indices identify selectable backs.</summary>
    internal sealed partial class TileStock
    {
        readonly List<GameTile> tiles = new List<GameTile>();
        readonly MatchRules rules;
        readonly Random random;
        int nextId = 1;
        public int Count => tiles.Count;

        public TileStock(MatchRules c, Random random)
        {
            rules = c; this.random = random;
            int perSet = 25 + c.Reverses + c.Passes + c.Swaps;
            int sets = (c.Players * c.Hand + perSet - 1) / perSet + 1;
            for (int i = 0; i < sets; i++) AddSet();
            Mix();
        }

        void AddSet()
        {
            foreach (Element sword in Enum.GetValues(typeof(Element)))
                foreach (Element shield in Enum.GetValues(typeof(Element)))
                    tiles.Add(new GameTile(nextId++, TileKind.Standard, sword, shield));
            AddWilds(TileKind.Reverse, rules.Reverses);
            AddWilds(TileKind.Pass, rules.Passes);
            AddWilds(TileKind.Swap, rules.Swaps);
        }
        void AddWilds(TileKind kind, int amount)
        {
            for (int i = 0; i < amount; i++) tiles.Add(new GameTile(nextId++, kind));
        }
        void Mix()
        {
            for (int i = tiles.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (tiles[i], tiles[j]) = (tiles[j], tiles[i]);
            }
        }
        public GameTile TakeTile(int index = -1)
        {
            if (index < 0) index = tiles.Count - 1;
            GameTile tile = tiles[index];
            tiles.RemoveAt(index);
            if (tiles.Count == 0) { AddSet(); Mix(); }
            return tile;
        }
        public void ReturnTiles(List<GameTile> hand) { tiles.AddRange(hand); hand.Clear(); Mix(); }

        // Only the designated successor receives the ordered stock and the next unused identity.
        internal void WriteBackup(System.IO.BinaryWriter writer)
        {
            writer.Write(nextId);
            MatchEngine.WriteTiles(writer,tiles);
        }
        internal void ReadBackup(System.IO.BinaryReader reader)
        {
            nextId = reader.ReadInt32();
            MatchEngine.ReadTiles(reader,tiles);
        }

        // Tutorial fixtures can fix the next draw without changing the rules.
        internal void SetNextDraw(GameTile tile) => tiles.Add(tile);
    }
}
