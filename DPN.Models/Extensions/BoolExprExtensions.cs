using DPN.Models.Abstractions;
using DPN.Models.Enums;
using Microsoft.Z3;

namespace DPN.Models.Extensions
{
    public static class BoolExprExtensions
    {
        public static Dictionary<string, DomainType> GetTypedVarsDict(this BoolExpr expression, VariableType varType)
        {
            var postfix = varType == VariableType.Written
                ? "_w"
                : "_r";

            // Not sure what would be faster - go through string(2) or through the leafs(1)
            Stack<Expr> expressionsToConsider = new Stack<Expr>();
            expressionsToConsider.Push(expression);

            HashSet<Expr> variables = new HashSet<Expr>();

            while (expressionsToConsider.Count > 0)
            {
                var expressionToConsider = expressionsToConsider.Pop();
                if (expressionToConsider.IsAnd || expressionToConsider.IsOr || expressionToConsider.IsNot)
                {
                    foreach(var expressionArg in expressionToConsider.Args)
                    {
                        expressionsToConsider.Push(expressionArg);
                    }
                }
                else
                {
                    if (!expressionToConsider.IsTrue && !expressionToConsider.IsFalse)
                    {
                        foreach (var expressionArg in expressionToConsider.Args)
                        {
                            if (!expressionArg.IsNumeral && expressionArg.ToString().EndsWith(postfix))
                            {
                                variables.Add(expressionArg);
                            }
                        }
                    }
                }
            }

            var result = new Dictionary<string, DomainType>();
            foreach (var variable in variables)
            {
                if (variable.IsBool)
                {
                    result.Add(variable.ToString()[..^2], DomainType.Boolean);
                }
                if (variable.IsInt)
                {
                    result.Add(variable.ToString()[..^2], DomainType.Integer);
                }
                if (variable.IsReal)
                {
                    result.Add(variable.ToString()[..^2], DomainType.Real);
                }
            }

            return result;
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
