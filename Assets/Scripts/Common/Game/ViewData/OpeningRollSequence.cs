using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DonmniCrash.Game
{
    /// <summary>Resultados públicos del sorteo, en el orden en que se presentan.</summary>
    public sealed class OpeningRollSequence
    {
        public const int StageMilliseconds = 1000;
        public int FirstSeat { get; }
        public int NextSeat { get; }
        public int DirectionRollStart { get; }
        public IReadOnlyList<int[]> Rolls { get; }
        public int DurationMilliseconds => (Rolls.Count + 1) * StageMilliseconds;

        OpeningRollSequence(int firstSeat,int nextSeat,int directionRollStart,int[][] rolls)
        {
            FirstSeat = firstSeat;
            NextSeat = nextSeat;
            DirectionRollStart = directionRollStart;
            Rolls = rolls;
        }

        public static OpeningRollSequence Capture(MatchEngine match) =>
            new OpeningRollSequence(match.ActiveSeat,match.Neighbor(match.Direction),match.NeighborRollStart,match.Rolls.ToArray());

        internal int[] Encode() => new[] { FirstSeat,NextSeat,DirectionRollStart }
            .Concat(Rolls.SelectMany(row => new[] { row.Length }.Concat(row))).ToArray();

        internal static OpeningRollSequence Decode(int[] data,int players)
        {
            if (data == null || data.Length < 3 || !ValidSeat(data[0],players) || !ValidSeat(data[1],players))
                throw new InvalidDataException("Invalid opening roll seats.");
            var rolls = new List<int[]>();
            for (int index = 3; index < data.Length;)
            {
                int length = data[index++];
                if (length < 4 || (length & 1) != 0 || length > players * 2 || length > data.Length - index)
                    throw new InvalidDataException("Invalid opening roll length.");
                for (int i = 0; i < length; i += 2)
                    if (!ValidSeat(data[index + i],players) || data[index + i + 1] < 1 || data[index + i + 1] > 6)
                        throw new InvalidDataException("Invalid opening die.");
                rolls.Add(new ArraySegment<int>(data,index,length).ToArray());
                index += length;
            }
            if (data[2] < 1 || data[2] > rolls.Count)
                throw new InvalidDataException("Invalid opening roll stages.");
            return new OpeningRollSequence(data[0],data[1],data[2],rolls.ToArray());
        }

        static bool ValidSeat(int seat,int players) => seat >= 0 && seat < players;
    }
}
