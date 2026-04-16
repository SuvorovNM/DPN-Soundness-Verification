using DPN.Models.DPNElements;
using DPN.Models.Enums;
using Microsoft.Z3;

namespace DPN.Models.Extensions
{
	public static class BoolExprExtensions
	{
		public static IEnumerable<string> GetVariablesComparedTo(
			this BoolExpr boolExpr, 
			VariableType sourceVarType, 
			string sourceVarName,  
			VariableType targetVarType,
			VariablesStore variables)
		{
			var sourceVarTypePostfix = sourceVarType == VariableType.Written
				? "_w"
				: "_r";
			var targetVarTypePostfix = targetVarType == VariableType.Written
				? "_w"
				: "_r";

			var expressionsToConsider = new Stack<Expr>();
			expressionsToConsider.Push(boolExpr);

			var variablesSet = variables.GetAllVariables().Select(v => v.name).ToHashSet();

			while (expressionsToConsider.Count > 0)
			{
				var expressionToConsider = expressionsToConsider.Pop();

				if ((IsComparisonOperator(expressionToConsider) || IsArithmeticOperation(expressionToConsider)))
				{
					var searchedVariableIndex = boolExpr.ToString().IndexOf(sourceVarName + sourceVarTypePostfix, StringComparison.Ordinal);
					if (searchedVariableIndex >= 0)
					{
						foreach (var variable in variablesSet)
						{
							var comparedVarIndex = boolExpr.ToString().LastIndexOf(variable+targetVarTypePostfix, StringComparison.Ordinal);

							if (comparedVarIndex >= 0 && comparedVarIndex != searchedVariableIndex)
							{
								yield return variable;
								break;
							}
						}
					}
				}
				else
				{
					foreach (var expressionArg in expressionToConsider.Args)
					{
						expressionsToConsider.Push(expressionArg);
					}
				}
			}
		}

		private static bool IsComparisonOperator(Expr expr)
		{
			var kind = expr.FuncDecl.DeclKind;
			return kind == Z3_decl_kind.Z3_OP_EQ ||
			       kind == Z3_decl_kind.Z3_OP_DISTINCT ||
			       kind == Z3_decl_kind.Z3_OP_LE ||
			       kind == Z3_decl_kind.Z3_OP_LT ||
			       kind == Z3_decl_kind.Z3_OP_GE ||
			       kind == Z3_decl_kind.Z3_OP_GT;
		}

		private static bool IsArithmeticOperation(Expr expr)
		{
			var kind = expr.FuncDecl.DeclKind;
			return kind == Z3_decl_kind.Z3_OP_ADD ||
			       kind == Z3_decl_kind.Z3_OP_SUB ||
			       kind == Z3_decl_kind.Z3_OP_MUL ||
			       kind == Z3_decl_kind.Z3_OP_DIV ||
			       kind == Z3_decl_kind.Z3_OP_MOD ||
			       kind == Z3_decl_kind.Z3_OP_POWER;
		}

		public static Dictionary<string, DomainType> GetTypedVarsDict(this BoolExpr expression, VariableType varType, VariablesStore? variables = null)
		{
			var varToType = (variables?.GetAllVariables() ?? Array.Empty<(DomainType domain, string name)>())
				.ToDictionary(x => x.name, x => x.domain);

			var postfix = varType == VariableType.Written
				? "_w"
				: "_r";

			// Not sure what would be faster - go through string(2) or through the leaves(1)
			var expressionsToConsider = new Stack<Expr>();
			expressionsToConsider.Push(expression);

			var vars = new HashSet<Expr>();

			while (expressionsToConsider.Count > 0)
			{
				var expressionToConsider = expressionsToConsider.Pop();
				if (expressionToConsider.Args.Length >= 1)
				{
					foreach (var expressionArg in expressionToConsider.Args)
					{
						expressionsToConsider.Push(expressionArg);
					}
				}
				else
				{
					if (expressionToConsider.ToString().EndsWith(postfix))
					{
						vars.Add(expressionToConsider);
					}
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
	}
}