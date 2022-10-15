using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.Enums
{
    [Flags]
    public enum ConstraintStateType
    {
        [Description("Sound intermediate state")]
        Default = 0,
        [Description("Initial state")]
        Initial = 1,
        [Description("Final state")]
        Final = 2,
        [Description("State with no way to final states")]
        NoWayToFinalMarking = 4,
        [Description("Unclean final state")]
        UncleanFinal = 8,
        [Description("Deadlock")]
        Deadlock = 16
    }
}
