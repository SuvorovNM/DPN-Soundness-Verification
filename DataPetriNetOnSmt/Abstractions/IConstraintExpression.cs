using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using Microsoft.Z3;

namespace DataPetriNetOnSmt.Abstractions
{
    public interface IConstraintExpression
    {
        LogicalConnective LogicalConnective { get; set; }
        BinaryPredicate Predicate { get; set; }
        ConstraintVariable ConstraintVariable { get; set; }

        bool Equals(IConstraintExpression other);
        IConstraintExpression GetInvertedExpression();
        IConstraintExpression Clone();
        IConstraintExpression CloneAsReadExpression();

        BoolExpr GetSmtExpression(Context ctx);

        string ToString();
    }
}
