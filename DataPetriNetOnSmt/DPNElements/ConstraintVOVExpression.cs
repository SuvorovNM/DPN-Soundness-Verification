using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.Enums;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.DPNElements
{
    public class ConstraintVOVExpression : IConstraintExpression
    {
        public LogicalConnective LogicalConnective { get; set; }
        public ConstraintVariable ConstraintVariable { get; set; }
        public BinaryPredicate Predicate { get; set; }

        public ConstraintVariable VariableToCompare { get; set; }

        public bool Equals(IConstraintExpression other)
        {
            var otherExpression = other as ConstraintVOVExpression;
            if (otherExpression == null)
            {
                return false;
            }

            return ConstraintVariable == otherExpression.ConstraintVariable &&
                Predicate == otherExpression.Predicate &&
                VariableToCompare == otherExpression.VariableToCompare;
        }

        public IConstraintExpression GetInvertedExpression()
        {
            var expression = new ConstraintVOVExpression();
            expression.VariableToCompare = VariableToCompare;
            expression.Predicate = (BinaryPredicate)(-(long)Predicate);
            expression.LogicalConnective = (LogicalConnective)(-(long)LogicalConnective);
            expression.ConstraintVariable = ConstraintVariable;

            return expression;
        }

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

        public IConstraintExpression CloneAsReadExpression()
        {
            return new ConstraintVOVExpression
            {
                LogicalConnective = this.LogicalConnective,
                Predicate = this.Predicate,
                ConstraintVariable = new ConstraintVariable
                {
                    VariableType = VariableType.Read,
                    Domain = this.ConstraintVariable.Domain,
                    Name = this.ConstraintVariable.Name,
                },
                VariableToCompare = this.VariableToCompare
            };
        }
    }
}
