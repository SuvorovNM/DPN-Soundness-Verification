using DPN.Models.DPNElements;
using DPN.Models.Enums;
using Microsoft.Z3;

namespace DPN.Models.Extensions
{
    public static class BoolExprExtensions
    {
        public static Dictionary<string, DomainType> GetTypedVarsDict(this BoolExpr expression, VariableType varType, VariablesStore? variables = null)
        {
	        var varToType = (variables?.GetAllVariables() ?? Array.Empty<(DomainType domain, string name)>())
		        .ToDictionary(x => x.name, x => x.domain);
	        
            var postfix = varType == VariableType.Written
                ? "_w"
                : "_r";

            // Not sure what would be faster - go through string(2) or through the leaves(1)
            Stack<Expr> expressionsToConsider = new Stack<Expr>();
            expressionsToConsider.Push(expression);

            HashSet<Expr> vars = new HashSet<Expr>();

            while (expressionsToConsider.Count > 0)
            {
                var expressionToConsider = expressionsToConsider.Pop();
                /*if (expressionToConsider.IsAnd || expressionToConsider.IsOr || expressionToConsider.IsNot)
                {
                    foreach(var expressionArg in expressionToConsider.Args)
                    {
                        expressionsToConsider.Push(expressionArg);
                    }
                }*/
                if (expressionToConsider.Args.Length >= 1)
                {
	                foreach(var expressionArg in expressionToConsider.Args)
	                {
		                expressionsToConsider.Push(expressionArg);
	                }
                }
                else
                {
	                if (expressionToConsider.ToString().EndsWith(postfix) )
	                {
		                vars.Add(expressionToConsider);
	                }
	                
                    /*if (!expressionToConsider.IsTrue && !expressionToConsider.IsFalse)
                    {
	                    if ((expressionToConsider.IsConst) && expressionToConsider.ToString().EndsWith(postfix) )
	                    {
		                    variables.Add(expressionToConsider);
	                    }
	                    else
	                    {
		                    foreach (var expressionArg in expressionToConsider.Args)
		                    {
			                    if (!expressionArg.IsNumeral && expressionArg.ToString().EndsWith(postfix))
			                    {
				                    variables.Add(expressionArg);
			                    }
		                    }
	                    }
                    }*/
                }
            }

            var result = new Dictionary<string, DomainType>();
            foreach (var variable in vars)
            {
	            var varName = variable.ToString()[..^2];
	            if (varToType.TryGetValue(varName, out var typeOfVariable))
	            {
		            result[varName] = typeOfVariable;
	            }
	            else
	            {
		            if (variable.IsInt)
		            {
			            result.TryAdd(varName, DomainType.Integer);
		            }
		            else if (variable.IsReal)
		            {
			            result.TryAdd(varName, DomainType.Real);
		            }
		            else if (variable.IsBool)
		            {
			            result.TryAdd(varName, DomainType.Boolean);
		            }
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
