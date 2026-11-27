using System;
using System.IO;
using DonmniCrash.Game;

namespace DonmniCrash.UI
{
    public sealed partial class OnlineSession
    {
        int motionEnd, motionRevision = -1;
        internal TileMotion[] FrameMotions { get; private set; } = Array.Empty<TileMotion>();
        internal int MotionRemaining => motionEnd == 0 ? 0 : Math.Max(0, unchecked(motionEnd - Now));
        internal float TurnSecondsLeft => MotionRemaining > 0
            ? Math.Max(0, unchecked(Deadline - motionEnd)) / 1000f : SecondsLeft(Deadline);

        void ScheduleMotions()
        {
            motionRevision = engine.Revision;
            var moves = engine.Motions;
            motionEnd = moves.Count == 0 ? 0 : unchecked(Now + (moves[moves.Count - 1].Stage + 1) * TileMotion.StageMilliseconds);
        }

        byte[] EncodeMotions(int seat)
        {
            if (engine.Revision != motionRevision || MotionRemaining == 0) return Array.Empty<byte>();
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            foreach (var movement in engine.Motions)
            {
                var m = movement.ForSeat(seat);
                writer.Write((byte)m.From);
                writer.Write((byte)m.To);
                writer.Write((byte)m.Sender);
                writer.Write((byte)m.Receiver);
                writer.Write((byte)m.Stage);
                writer.Write(m.Choice);
                writer.Write(m.Tile.HasValue);
                if (m.Tile.HasValue) MatchEngine.WriteTile(writer, m.Tile.Value);
            }
            return stream.ToArray();
        }

        static TileMotion[] DecodeMotions(byte[] data)
        {
            if (data.Length == 0) return Array.Empty<TileMotion>();
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);
            var moves = new System.Collections.Generic.List<TileMotion>();
            while (stream.Position < stream.Length)
            {
                var from = (TileZone)reader.ReadByte();
                var to = (TileZone)reader.ReadByte();
                int sender = reader.ReadByte(), receiver = reader.ReadByte(), stage = reader.ReadByte();
                int choice = reader.ReadInt32();
                GameTile? tile = reader.ReadBoolean() ? MatchEngine.ReadTile(reader) : null;
                moves.Add(new TileMotion(from, to, sender, receiver, tile, stage, choice));
            }
            return moves.ToArray();
        }
    }
}
