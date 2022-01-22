using DataPetriNetOnSmt.Enums;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.Extensions
{
    public static class BoolExprExtensions
    {
        public static BoolExpr? GetExpressionWithoutNotClause(this BoolExpr sourceExpression)
        {
            if (sourceExpression == null)
            {
                throw new ArgumentNullException(nameof(sourceExpression));
            }

            return sourceExpression.IsNot
                    ? sourceExpression.Args[0] as BoolExpr
                    : sourceExpression;
        }

        public static LogicalConnective GetLogicalConnective(this BoolExpr sourceExpression)
        {
            if (sourceExpression.IsAnd)
            {
                return LogicalConnective.And;
            }
            if (sourceExpression.IsOr)
            {
                return LogicalConnective.Or;
            }

            return LogicalConnective.Empty;
        }

        public static BinaryPredicate GetBinaryPredicate(this BoolExpr sourceExpression)
        {
            if (sourceExpression.IsNot && sourceExpression.Args[0].IsEq)
            {
                return BinaryPredicate.Unequal;
            }
            if (sourceExpression.IsEq)
            {
                return BinaryPredicate.Equal;
            }
            if (sourceExpression.IsLE)
            {
                return BinaryPredicate.LessThanOrEqual;
            }
            if (sourceExpression.IsGE)
            {
                return BinaryPredicate.GreaterThanOrEqual;
            }
            if (sourceExpression.IsLT)
            {
                return BinaryPredicate.LessThan;
            }
            if (sourceExpression.IsGT)
            {
                return BinaryPredicate.GreaterThan;
            }

            else throw new ArgumentException("No corresponding predicate is found");
        }
    }
}
