using System;
using System.Collections.Generic;
using System.Linq;

namespace DonmniCrash.Game
{
    /// <summary>Owns game transitions. Validate requests before mutating authoritative state.</summary>
    public sealed partial class MatchEngine : ITableView
    {
        sealed class SeatState
        {
            public int Health, Capacity;
            public readonly List<GameTile> Hand = new List<GameTile>();
        }
        readonly Random random;
        readonly TileStock stock;
        readonly SeatState[] players;
        readonly List<int> orden;
        readonly List<GameTile> board = new List<GameTile>();
        readonly List<int[]> rolls = new List<int[]>();
        public readonly MatchRules Config;
        public int ActiveSeat { get; private set; }
        public int Direction { get; private set; } = 1;
        public Element? Attack { get; private set; }
        public TurnPhase Phase { get; private set; }
        public int Revision { get; private set; }
        public int StockCount => stock.Count;
        public int MissingCount => players[ActiveSeat].Capacity - players[ActiveSeat].Hand.Count;
        public IReadOnlyList<GameTile> BoardTiles => board.AsReadOnly();
        public IReadOnlyList<int> Order => orden.AsReadOnly();
        // Rows contain seat/die pairs. NeighborRollStart separates the direction rolls.
        public IReadOnlyList<int[]> Rolls => rolls.AsReadOnly();
        public int NeighborRollStart { get; private set; }
        public int Health(int actor) => players[actor].Health;
        public int Capacity(int actor) => players[actor].Capacity;
        public int HandCount(int actor) => players[actor].Hand.Count;
        public GameTile[] HandOf(int actor) => players[actor].Hand.ToArray();
        GameTile? offer;
        public GameTile? OfferFor(int actor) => actor == ActiveSeat ? offer : null;

        public MatchEngine(MatchRules config, int seed)
        {
            Config = config; random = new Random(seed);
            stock = new TileStock(config, random);
            players = new SeatState[config.Players];
            orden = Enumerable.Range(0, config.Players).ToList();
            for (int i = orden.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (orden[i], orden[j]) = (orden[j], orden[i]);
            }
            foreach (int id in orden)
            {
                players[id] = new SeatState { Health = config.Health, Capacity = config.Hand };
                for (int i = 0; i < config.Hand; i++) players[id].Hand.Add(stock.TakeTile());
            }
            ActiveSeat = RollOff(orden);
            NeighborRollStart = rolls.Count;
            if (orden.Count > 2)
            {
                int rightSeat = Neighbor(1), leftSeat = Neighbor(-1);
                Direction = RollOff(new[]{ rightSeat, leftSeat }) == rightSeat ? -1 : 1;
            }
        }

        int RollOff(IEnumerable<int> contenders)
        {
            var ids = contenders.ToList();
            while (ids.Count > 1)
            {
                int[] dice = ids.Select(_ => random.Next(1,7)).ToArray();
                rolls.Add(ids.SelectMany((id,i) => new[]{ id, dice[i] }).ToArray());
                int highest = dice.Max();
                ids = ids.Where((id,i) => dice[i] == highest).ToList();
            }
            return ids[0];
        }

        public int Neighbor(int direction)
        {
            int i = orden.IndexOf(ActiveSeat);
            do { i = (i + direction + orden.Count) % orden.Count; } while (Health(orden[i]) == 0);
            return orden[i];
        }

        public bool CanTarget(int id) => id >= 0 && id < players.Length && id != ActiveSeat &&
            Health(id) > 0 && HandCount(id) == Capacity(id);

        /// <summary>Reject stale revisions, wrong actors and moves incompatible with the current phase.</summary>
        public bool TryApply(int actor, int revision, MoveCommand a)
        {
            if (actor != ActiveSeat || revision != Revision || Phase == TurnPhase.Finished) return false;
            bool accepted;
            switch (Phase)
            {
                case TurnPhase.Decision:
                    if (a.Kind == MoveKind.Draw) { Draw(); accepted = true; }
                    else accepted = a.Kind == MoveKind.Play && Play(a);
                    break;
                case TurnPhase.Offer:
                    if (a.Kind == MoveKind.Keep) { Keep(); accepted = true; }
                    else accepted = a.Kind == MoveKind.Play && Play(a);
                    break;
                default:
                    accepted = a.Kind == MoveKind.Choose && Choose(a.Value);
                    break;
            }
            if (accepted) Revision++;
            return accepted;
        }

        void Advance()
        {
            ActiveSeat = Neighbor(Direction);
            Phase = MissingCount > 0 ? TurnPhase.Refill : TurnPhase.Decision;
        }

        void Draw()
        {
            offer = stock.TakeTile();
            if (TileRules.IsPlayable(offer.Value, Attack)) Phase = TurnPhase.Offer;
            else Keep();
        }

        void Keep()
        {
            var playerState = players[ActiveSeat];
            playerState.Hand.Add(offer.Value);
            offer = null;
            playerState.Capacity++;
            Phase = TurnPhase.Decision;
            if (--playerState.Health > 0) return;
            stock.ReturnTiles(playerState.Hand);
            if (players.Count(j => j.Health > 0) == 1) Phase = TurnPhase.Finished;
            else Advance();
        }

        bool Play(MoveCommand a)
        {
            bool fromOffer = Phase == TurnPhase.Offer;
            var hand = players[ActiveSeat].Hand;
            int index = hand.FindIndex(f => f.Id == a.Value);
            if (fromOffer ? offer?.Id != a.Value : index < 0) return false;
            var tile = fromOffer ? offer.Value : hand[index];
            if (!TileRules.IsPlayable(tile,Attack)) return false;
            int wildIndex = hand.FindIndex(f => f.Kind == TileKind.Swap);
            bool combo = a.Target != -1;
            if (combo && (fromOffer || tile.Kind != TileKind.Standard || wildIndex < 0 || !CanTarget(a.Target))) return false;

            board.Add(tile);
            if (tile.Kind == TileKind.Standard) Attack = tile.Sword;
            if (tile.Kind == TileKind.Reverse) Direction *= -1;
            if (fromOffer) offer = null;
            else hand.RemoveAt(index);
            if (combo)
            {
                int pos = hand.FindIndex(f => f.Kind == TileKind.Swap);
                board.Add(hand[pos]); hand.RemoveAt(pos);
                BeginSwap(a.Target);
            }
            else
            {
                // Playing the offered tile leaves hand capacity unchanged.
                if (!fromOffer) hand.Add(stock.TakeTile());
                Advance();
            }
            return true;
        }
    }
}
