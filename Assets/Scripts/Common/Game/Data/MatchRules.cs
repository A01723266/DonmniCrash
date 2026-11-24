using System;

namespace DonmniCrash.Game
{
    public enum BotLevel { Easy, Normal, Hard }
    public enum Element { Earth, Water, Wind, Fire, Plant }
    public enum TileKind { Standard, Reverse, Pass, Swap }
    public enum TurnPhase { Decision, Offer, SwapSelection, Refill, Finished }
    public enum MoveKind { Play, Draw, Keep, Choose }

    /// <summary>Immutable round settings shared by the local and network engines.</summary>
    public sealed partial class MatchRules
    {
        public readonly int Players, Hand, Health, Seconds, Reverses, Passes, Swaps;
        public readonly BotLevel Level;
        public MatchRules(int players = 4, int hand = 5, int health = 3, int seconds = 5,
            int reversas = 1, int pases = 1, int swaps = 1, BotLevel level = BotLevel.Normal)
        {
            if (players < 2 || players > 6 || hand < 2 || hand > 8 || health < 1 || health > 10 ||
                seconds < 1 || seconds > 10 || reversas < 0 || pases < 0 || swaps < 0 ||
                (long)reversas + pases + swaps > 8 || !Enum.IsDefined(typeof(BotLevel), level))
                throw new ArgumentOutOfRangeException(nameof(players), "Invalid match rules.");
            Players = players; Hand = hand; Health = health; Seconds = seconds;
            Reverses = reversas; Passes = pases; Swaps = swaps; Level = level;
        }
    }

    /// <summary>Unique identity distinguishes equivalent tiles drawn from different sets.</summary>
    public readonly struct GameTile
    {
        public readonly int Id;
        public readonly TileKind Kind;
        public readonly Element Sword, Shield;
        public GameTile(int id, TileKind kind, Element sword = default, Element shield = default)
        { Id = id; Kind = kind; Sword = sword; Shield = shield; }
    }

    /// <summary>A requested move with an optional swap recipient selected before committing.</summary>
    public readonly struct MoveCommand
    {
        public readonly MoveKind Kind;
        public readonly int Value, Target;
        public MoveCommand(MoveKind kind, int value = -1, int target = -1)
        { Kind = kind; Value = value; Target = target; }
    }
}
