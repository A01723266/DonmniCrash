using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace DonmniCrash.Game
{
    public sealed partial class MatchEngine
    {
        // A seat can disappear between the two confirmations of a swap. Complete that atomic
        // exchange first, then move the departing hand back to stock exactly once.
        public bool RemoveSeat(int seat)
        {
            if (seat < 0 || seat >= players.Length || Health(seat) == 0 || Phase == TurnPhase.Finished) return false;
            if (Phase == TurnPhase.SwapSelection && (seat == ActiveSeat || seat == recipient)) Expire();
            if (seat == ActiveSeat && offer.HasValue)
            {
                players[seat].Hand.Add(offer.Value);
                offer = null;
            }
            Eliminate(seat);
            Revision++;
            return true;
        }

        // The backup is an authority-only format. Client views have a separate encoder.
        public byte[] SaveAuthority()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(Revision); writer.Write(ActiveSeat); writer.Write(Direction); writer.Write((int)Phase);
            writer.Write(Attack.HasValue ? (int)Attack.Value : -1);
            writer.Write(SelectionId); writer.Write(recipient); writer.Write(selectionCount);
            writer.Write(offer.HasValue);
            if (offer.HasValue) WriteTile(writer,offer.Value);
            foreach (int seat in orden) writer.Write(seat);
            foreach (var seat in players)
            {
                writer.Write(seat.Rank); writer.Write(seat.Health); writer.Write(seat.Capacity); WriteTiles(writer,seat.Hand);
            }
            WriteTiles(writer,board); WriteTiles(writer,candidates);
            writer.Write(picks.Count);
            foreach (int option in picks) writer.Write(option);
            stock.WriteBackup(writer);
            return stream.ToArray();
        }

        public void LoadAuthority(byte[] bytes)
        {
            using var reader = new BinaryReader(new MemoryStream(bytes));
            Revision = reader.ReadInt32(); ActiveSeat = reader.ReadInt32(); Direction = reader.ReadInt32();
            Phase = (TurnPhase)reader.ReadInt32();
            int attack = reader.ReadInt32(); Attack = attack < 0 ? (Element?)null : (Element)attack;
            SelectionId = reader.ReadInt32(); recipient = reader.ReadInt32(); selectionCount = reader.ReadInt32();
            offer = reader.ReadBoolean() ? ReadTile(reader) : (GameTile?)null;
            for (int i=0;i<orden.Count;i++) orden[i] = reader.ReadInt32();
            foreach (var seat in players)
            {
                seat.Rank = reader.ReadInt32(); seat.Health = reader.ReadInt32(); seat.Capacity = reader.ReadInt32(); ReadTiles(reader,seat.Hand);
            }
            ReadTiles(reader,board); ReadTiles(reader,candidates);
            picks.Clear();
            int count = reader.ReadInt32();
            for (int i=0;i<count;i++) picks.Add(reader.ReadInt32());
            stock.ReadBackup(reader);
        }

        internal static void WriteTile(BinaryWriter w,GameTile f)
        {
            w.Write(f.Id); w.Write((byte)f.Kind); w.Write((byte)f.Sword); w.Write((byte)f.Shield);
        }

        internal static GameTile ReadTile(BinaryReader r) =>
            new GameTile(r.ReadInt32(),(TileKind)r.ReadByte(),(Element)r.ReadByte(),(Element)r.ReadByte());

        internal static void WriteTiles(BinaryWriter w,IReadOnlyList<GameTile> tiles)
        {
            w.Write(tiles.Count);
            foreach (var tile in tiles) WriteTile(w,tile);
        }

        internal static void ReadTiles(BinaryReader r,List<GameTile> target)
        {
            target.Clear();
            int count = r.ReadInt32();
            if (count < 0 || count > (r.BaseStream.Length-r.BaseStream.Position)/7) throw new InvalidDataException();
            for (int i=0;i<count;i++) target.Add(ReadTile(r));
        }
    }
}
