using System;
using System.Collections.Generic;
using System.IO;

namespace DonmniCrash.UI
{
    /// <summary>Copies stable spans from the acknowledged backup even when a draw shifts their offsets.</summary>
    internal static class CheckpointDelta
    {
        const int Block = 16;
        const byte Literal = 0, Copy = 1;

        public static byte[] Encode(byte[] before, byte[] after)
        {
            if (before.Length == 0) return FullImage(after);
            var offsets = new Dictionary<ulong,int>();
            for (int i = 0; i <= before.Length - Block; i += Block)
                offsets[BitConverter.ToUInt64(before,i)] = i;

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(after.Length);
            int cursor = 0, literalStart = 0;
            while (cursor <= after.Length - Block)
            {
                int match = 0, source = 0;
                if (offsets.TryGetValue(BitConverter.ToUInt64(after,cursor),out source))
                {
                    int limit = Math.Min(before.Length-source,after.Length-cursor);
                    while (match < limit && before[source+match] == after[cursor+match]) match++;
                }
                if (match < Block) { cursor++; continue; }
                WriteLiteral(writer,after,literalStart,cursor-literalStart);
                writer.Write(Copy);
                writer.Write(match);
                writer.Write(source);
                cursor += match;
                literalStart = cursor;
            }
            WriteLiteral(writer,after,literalStart,after.Length-literalStart);
            return stream.Length < after.Length + 9 ? stream.ToArray() : FullImage(after);
        }

        static byte[] FullImage(byte[] bytes)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(bytes.Length);
            WriteLiteral(writer,bytes,0,bytes.Length);
            return stream.ToArray();
        }

        static void WriteLiteral(BinaryWriter writer,byte[] data,int start,int length)
        {
            if (length == 0) return;
            writer.Write(Literal);
            writer.Write(length);
            writer.Write(data,start,length);
        }

        public static byte[] Apply(byte[] before,byte[] patch)
        {
            using var reader = new BinaryReader(new MemoryStream(patch));
            int length = reader.ReadInt32();
            using var result = new MemoryStream();
            while (reader.BaseStream.Position < patch.Length)
            {
                byte kind = reader.ReadByte();
                int count = reader.ReadInt32();
                if (count <= 0 || count > length-result.Length) throw new InvalidDataException();
                if (kind == Copy)
                {
                    int offset = reader.ReadInt32();
                    if (offset < 0 || count > before.Length-offset) throw new InvalidDataException();
                    result.Write(before,offset,count);
                }
                else if (kind == Literal)
                {
                    if (count > patch.Length-reader.BaseStream.Position) throw new InvalidDataException();
                    result.Write(patch,(int)reader.BaseStream.Position,count);
                    reader.BaseStream.Position += count;
                }
                else throw new InvalidDataException();
            }
            if (result.Length != length) throw new InvalidDataException();
            return result.ToArray();
        }
    }
}
