using System.ComponentModel;

namespace DataPetriNetOnSmt.Enums
{
    public enum StateType // Value is priority - the higher priority, the greater is number
    {
        [Description("Initial state")]
        Initial = 0,
        [Description("Sound intermediate state")]
        SoundIntermediate = 1,
        [Description("State with no way to final states")]
        NoWayToFinalMarking = 2,
        [Description("Clean final state")]
        CleanFinal = 3,
        [Description("Unclean final state")]
        UncleanFinal = 4,
        [Description("Deadlock state")]
        Deadlock = 5
    }
}
