using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.Enums;
using Microsoft.Z3;
using System;
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

        public bool Equals(IConstraintExpression other)
        {
            var otherExpression = other as ConstraintVOCExpression<T>;
            if (otherExpression == null)
            {
                return false;
            }

            return ConstraintVariable == otherExpression.ConstraintVariable &&
                Predicate == otherExpression.Predicate &&
                Constant.Equals(otherExpression.Constant);
        }

        public IConstraintExpression GetInvertedExpression()
        {
            var expression = new ConstraintVOCExpression<T>();
            expression.Constant = Constant;
            expression.Predicate = (BinaryPredicate)(-(long)Predicate);
            expression.LogicalConnective = (LogicalConnective)(-(long)LogicalConnective);
            expression.ConstraintVariable = ConstraintVariable;

            return expression;
        }

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
    }
}
