using DPN.Models.DPNElements;
using DPN.Models.Enums;
using Microsoft.Z3;

namespace DPN.Models.Abstractions
{
    public interface IConstraintExpression
    {
        LogicalConnective LogicalConnective { get; set; }
        BinaryPredicate Predicate { get; set; }
        ConstraintVariable ConstraintVariable { get; set; }

        IConstraintExpression Clone();

        BoolExpr GetSmtExpression(Context ctx);

        string ToString();
    }
}
