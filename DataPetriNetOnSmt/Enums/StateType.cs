using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.Enums
{
    public enum StateType
    {
        Initial = 0,
        SoundIntermediate = 1,
        CleanFinal = 2,
        UncleanFinal = 3,
        Deadlock = 4
    }
}
