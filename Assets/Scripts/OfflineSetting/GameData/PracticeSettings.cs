using UnityEngine;
using DonmniCrash.Game;

namespace DonmniCrash.UI
{
    /// <summary>Carry the confirmed rules between the form, practice table and rematch.</summary>
    public static partial class PracticeSettings
    {
        public static MatchRules Current { get; private set; } = new MatchRules();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState() => Current = new MatchRules();
        public static void Confirm(MatchRules c) => Current = c;
    }
}
