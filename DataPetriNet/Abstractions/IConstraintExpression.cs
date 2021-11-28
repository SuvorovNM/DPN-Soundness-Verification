using DataPetriNet.DPNElements;
using DataPetriNet.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.Abstractions
{
    public interface IConstraintExpression
    {
        LogicalConnective LogicalConnective { get; set; }
        BinaryPredicate Predicate { get; set; }
        ConstraintVariable ConstraintVariable { get; set; }
    }
}
