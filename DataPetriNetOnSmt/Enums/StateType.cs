using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.Enums
{
    public enum StateType // Value is priority - the higher priority, the greater is number
    {
        Initial = 0,
        SoundIntermediate = 1,
        NoWayToFinalMarking = 2,
        CleanFinal = 3,
        UncleanFinal = 4,
        Deadlock = 5
    }
}
