using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DPN.Models.Enums
{
    [Flags]
    public enum MarkingComparisonResult
    {
        Incomparable = 1,
        LessThan = 2,
        Equal = 4,
        GreaterThan = 8
    }
}
