using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using DonmniCrash.Game;

namespace DonmniCrash.UI
{
    // A replica has storage for one hand only. A spectator has an empty hand and no choices.
    public sealed class NetworkTableView : ITableView
    {
        readonly List<GameTile> history = new List<GameTile>(), hand = new List<GameTile>();
        int[] order, health, sizes, capacity;
        int owner=-1;
        GameTile? offer;
        GameTile?[] choices = Array.Empty<GameTile?>();
        bool[] available = Array.Empty<bool>();
        public int ActiveSeat { get; private set; }
        public TurnPhase Phase { get; private set; }
        public Element? Attack { get; private set; }
        public int StockCount { get; private set; }
        public int RemainingChoices { get; private set; }
        public bool VisibleSelection { get; private set; }
        public int Revision { get; private set; } = -1;
        public int Direction { get; private set; }
        public int OptionCount => available.Length;
        public IReadOnlyList<int> Order => order;
        public IReadOnlyList<GameTile> BoardTiles => history;
        public int Health(int seat) => health[seat];
        public int HandCount(int seat) => sizes[seat];
        public GameTile[] HandOf(int seat) => seat==owner && owner>=0 ? hand.ToArray() : Array.Empty<GameTile>();
        public GameTile? OfferFor(int seat) => seat==owner ? offer : null;
        public bool IsAvailable(int index) => available[index];
        public GameTile? InspectOption(int seat,int index) => seat==owner && index>=0 && index<choices.Length ? choices[index] : null;
        public bool CanTarget(int seat) => seat != ActiveSeat && health[seat]>0 && sizes[seat]==capacity[seat];

        // A full view is sent on admission or repair. A normal commit appends only new board tiles.
        internal static byte[] PublicFrame(MatchEngine game,int start)
        {
            using var stream = new MemoryStream();
            using var w = new BinaryWriter(stream);
            w.Write(game.Revision); w.Write(game.ActiveSeat); w.Write(game.Direction); w.Write((int)game.Phase);
            w.Write(game.Attack.HasValue ? (int)game.Attack.Value : -1); w.Write(game.StockCount);
            w.Write((byte)game.Order.Count);
            foreach (int seat in game.Order)
            {
                w.Write((byte)seat); w.Write((byte)game.Health(seat)); w.Write(game.HandCount(seat)); w.Write(game.Capacity(seat));
            }
            w.Write(start);
            MatchEngine.WriteTiles(w,game.BoardTiles.Skip(start).ToArray());
            return stream.ToArray();
        }

        internal static byte[] PrivateFrame(MatchEngine game,int seat)
        {
            using var stream = new MemoryStream();
            using var w = new BinaryWriter(stream);
            MatchEngine.WriteTiles(w,seat < 0 ? Array.Empty<GameTile>() : game.HandOf(seat));
            var drawn = game.OfferFor(seat);
            w.Write(drawn.HasValue);
            if (drawn.HasValue) MatchEngine.WriteTile(w,drawn.Value);
            bool selecting = game.ActiveSeat == seat && (game.Phase == TurnPhase.Refill || game.Phase == TurnPhase.SwapSelection);
            w.Write(selecting ? game.OptionCount : 0);
            if (selecting)
            {
                w.Write(game.RemainingChoices); w.Write(game.VisibleSelection);
                for (int i=0;i<game.OptionCount;i++)
                {
                    bool enabled = game.IsAvailable(i);
                    w.Write(enabled);
                    if (enabled && game.VisibleSelection) MatchEngine.WriteTile(w,game.InspectOption(seat,i).Value);
                }
            }
            return stream.ToArray();
        }

        internal void Apply(byte[] frame,byte[] privateData,bool full,int recipientSeat)
        {
            owner=recipientSeat;
            using var r = new BinaryReader(new MemoryStream(frame));
            Revision = r.ReadInt32(); ActiveSeat = r.ReadInt32(); Direction = r.ReadInt32(); Phase = (TurnPhase)r.ReadInt32();
            int attack = r.ReadInt32(); Attack = attack < 0 ? (Element?)null : (Element)attack;
            StockCount = r.ReadInt32();
            int n=r.ReadByte(); order=new int[n]; health=new int[n]; sizes=new int[n]; capacity=new int[n];
            for (int i=0;i<n;i++)
            {
                int seat=order[i]=r.ReadByte(); health[seat]=r.ReadByte(); sizes[seat]=r.ReadInt32(); capacity[seat]=r.ReadInt32();
            }
            int start = r.ReadInt32();
            if (full) history.Clear();
            if (history.Count != start) throw new InvalidDataException();
            var added=new List<GameTile>(); MatchEngine.ReadTiles(r,added); history.AddRange(added);
            if (privateData.Length == 0) return;
            using var p = new BinaryReader(new MemoryStream(privateData));
            MatchEngine.ReadTiles(p,hand);
            offer = p.ReadBoolean() ? MatchEngine.ReadTile(p) : (GameTile?)null;
            n=p.ReadInt32(); choices=new GameTile?[n]; available=new bool[n];
            RemainingChoices=0; VisibleSelection=false;
            if (n==0) return;
            RemainingChoices=p.ReadInt32(); VisibleSelection=p.ReadBoolean();
            for(int i=0;i<n;i++)
                if ((available[i]=p.ReadBoolean()) && VisibleSelection) choices[i]=MatchEngine.ReadTile(p);
        }
    }
}
