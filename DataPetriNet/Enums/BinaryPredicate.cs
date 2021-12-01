using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.Enums
{
    public enum BinaryPredicate
    {
        Equal = 1,
        Unequal = -1,

        GreaterThan = 2,
        LessThan = -2,

        GreaterThenOrEqual = 3,
        LessThanOrEqual = -3
    }
}
