using DPN.Models.Abstractions;
using DPN.Models.Enums;
using Microsoft.Z3;

namespace DPN.Models.DPNElements
{
    public class ConstraintVOVExpression : IConstraintExpression
    {
        public LogicalConnective LogicalConnective { get; set; }
        public ConstraintVariable ConstraintVariable { get; set; }
        public BinaryPredicate Predicate { get; set; }

        public ConstraintVariable VariableToCompare { get; set; }

        public IConstraintExpression Clone()
        {
            return new ConstraintVOVExpression
            {
                LogicalConnective = this.LogicalConnective,
                Predicate = this.Predicate,
                ConstraintVariable = this.ConstraintVariable,
                VariableToCompare = this.VariableToCompare
            };
        }

        public BoolExpr GetSmtExpression(Context ctx)
        {
            Sort sort = ConstraintVariable.Domain switch
            {
                DomainType.Integer => ctx.MkIntSort(),
                DomainType.Boolean => ctx.MkBoolSort(),
                DomainType.Real => ctx.MkRealSort(),
                _ => throw new NotImplementedException("This type is not supported yet")
            };

            var variable = ctx.MkConst(ctx.MkSymbol(ConstraintVariable.Name + (ConstraintVariable.VariableType == VariableType.Read ? "_r" : "_w")), sort);
            var variableToCompare = ctx.MkConst(ctx.MkSymbol(VariableToCompare.Name + (VariableToCompare.VariableType == VariableType.Read ? "_r" : "_w")), sort);

            return Predicate switch
            {
                BinaryPredicate.Equal => ctx.MkEq(variable, variableToCompare),
                BinaryPredicate.Unequal => ctx.MkNot(ctx.MkEq(variable, variableToCompare)),
                BinaryPredicate.LessThan => ctx.MkLt((ArithExpr)variable, (ArithExpr)variableToCompare),
                BinaryPredicate.LessThanOrEqual => ctx.MkLe((ArithExpr)variable, (ArithExpr)variableToCompare),
                BinaryPredicate.GreaterThan => ctx.MkGt((ArithExpr)variable, (ArithExpr)variableToCompare),
                BinaryPredicate.GreaterThanOrEqual => ctx.MkGe((ArithExpr)variable, (ArithExpr)variableToCompare),
            };
        }

        public override string ToString()
        {
            var predicate = Predicate switch
            {
                BinaryPredicate.GreaterThan => " > ",
                BinaryPredicate.LessThan => " < ",
                BinaryPredicate.LessThanOrEqual => " <= ",
                BinaryPredicate.GreaterThanOrEqual => " >= ",
                BinaryPredicate.Equal => " == ",
                BinaryPredicate.Unequal => " != "
            };
            var logicalConnective = LogicalConnective switch
            {
                LogicalConnective.And => "∧",
                LogicalConnective.Or => "∨",
                LogicalConnective.Empty => string.Empty
            };
            var firstVariableSuffix = ConstraintVariable.VariableType switch
            {
                VariableType.Read => "_r",
                VariableType.Written => "_w"
            };
            var secondVariableSuffix = VariableToCompare.VariableType switch
            {
                VariableType.Read => "_r",
                VariableType.Written => "_w"
            };

            return logicalConnective + " " + ConstraintVariable.Name + firstVariableSuffix + predicate + VariableToCompare.Name + secondVariableSuffix;
        }
    }
}
