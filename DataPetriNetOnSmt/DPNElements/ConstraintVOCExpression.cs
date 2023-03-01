using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.Enums;
using Microsoft.Z3;
using System.Globalization;

namespace DataPetriNetOnSmt.DPNElements
{
    // Разные классы наследования
    public class ConstraintVOCExpression<T> : IConstraintExpression
        where T : IEquatable<T>, IComparable<T>
    {
        public LogicalConnective LogicalConnective { get; set; }
        public ConstraintVariable ConstraintVariable { get; set; }
        public BinaryPredicate Predicate { get; set; }
        public DefinableValue<T> Constant { get; set; }


        public IConstraintExpression Clone()
        {
            return new ConstraintVOCExpression<T>
            {
                Constant = this.Constant,
                LogicalConnective = this.LogicalConnective,
                Predicate = this.Predicate,
                ConstraintVariable = this.ConstraintVariable
            };
        }

        public BoolExpr GetSmtExpression(Context ctx) // TODO: По возможности отрефакторить, добавить возможность сравнения переменных
        {
            Sort sort = ConstraintVariable.Domain switch
            {
                DomainType.Integer => ctx.MkIntSort(),
                DomainType.Boolean => ctx.MkBoolSort(),
                DomainType.Real => ctx.MkRealSort(),
                _ => throw new NotImplementedException("This type is not supported yet")
            };

            Expr constToCompare = ConstraintVariable.Domain switch
            {
                DomainType.Integer => ctx.MkInt(Constant.Value.ToString()),
                DomainType.Boolean => ctx.MkBool(Constant.Value.ToString() == "True"),
                DomainType.Real => ctx.MkReal((Constant.Value as double?).Value.ToString(CultureInfo.InvariantCulture)),
                _ => throw new NotImplementedException("This type is not supported yet")
            };

            var variable = ctx.MkConst(ctx.MkSymbol(ConstraintVariable.Name + (ConstraintVariable.VariableType == VariableType.Read ? "_r" : "_w")), sort);

            return Predicate switch
            {
                BinaryPredicate.Equal => ctx.MkEq(variable, constToCompare),
                BinaryPredicate.Unequal => ctx.MkNot(ctx.MkEq(variable, constToCompare)),
                BinaryPredicate.LessThan => ctx.MkLt((ArithExpr)variable, (ArithExpr)constToCompare),
                BinaryPredicate.LessThanOrEqual => ctx.MkLe((ArithExpr)variable, (ArithExpr)constToCompare),
                BinaryPredicate.GreaterThan => ctx.MkGt((ArithExpr)variable, (ArithExpr)constToCompare),
                BinaryPredicate.GreaterThanOrEqual => ctx.MkGe((ArithExpr)variable, (ArithExpr)constToCompare),
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
            var variableSuffix = ConstraintVariable.VariableType switch
            {
                VariableType.Read => "_r",
                VariableType.Written => "_w"
            };

            return logicalConnective + " " + ConstraintVariable.Name + variableSuffix + predicate + Constant.Value.ToString();
        }
    }
}
