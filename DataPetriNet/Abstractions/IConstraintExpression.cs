using DataPetriNet.DPNElements;
using DataPetriNet.Enums;

namespace DataPetriNet.Abstractions
{
    public interface IConstraintExpression
    {
        LogicalConnective LogicalConnective { get; set; }
        BinaryPredicate Predicate { get; set; }
        ConstraintVariable ConstraintVariable { get; set; }

        IConstraintExpression GetInvertedExpression();
    }
}
