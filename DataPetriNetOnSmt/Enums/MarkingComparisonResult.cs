using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.Enums
{
    public enum MarkingComparisonResult
    {
        Incomparable = -2,
        LessThan = -1,
        Equal = 0,
        GreaterThan = 1
    }
}
