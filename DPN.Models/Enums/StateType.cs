using System.ComponentModel;

namespace DPN.Models.Enums
{
    public enum StateType // Value is priority - the higher priority, the greater is number
    {
        [Description("Initial state")]
        Initial = 0,
        [Description("Sound intermediate state")]
        SoundIntermediate = 1,
        [Description("Unfeasible (no way to final) state")]
        NoWayToFinalMarking = 2,
        [Description("Clean final state")]
        CleanFinal = 3,
        [Description("Unclean final state")]
        UncleanFinal = 4,
        [Description("Deadlock state")]
        Deadlock = 5
    }
}
