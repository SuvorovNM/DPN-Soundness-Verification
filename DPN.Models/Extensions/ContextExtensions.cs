using Microsoft.Z3;
using DPN.Models.Abstractions;
using DPN.Models.Enums;

namespace DPN.Models.Extensions
{
	public static class ContextExtensions
	{
		private delegate BoolExpr ConnectExpressions(params BoolExpr[] expr);

		public static bool CanBeSatisfied(this Context context, BoolExpr expression)
		{
			if (expression is null)
			{
				throw new ArgumentNullException(nameof(expression));
			}

			Solver s = context.MkSimpleSolver();
			s.Assert(expression);

			var result = s.Check() == Status.SATISFIABLE;

			return result;
		}

		public static bool AreEqual(this Context context, BoolExpr? expr1, BoolExpr? expr2)
		{
			if (expr1 is null)
			{
				throw new ArgumentNullException(nameof(expr1));
			}

			if (expr2 is null)
			{
				throw new ArgumentNullException(nameof(expr2));
			}

			// 2 expressions are equal if [(not(x) and y) or (x and not(y))] is not satisfiable
			var exprWithSourceNegated = context.MkAnd(context.MkNot(expr1), expr2);
			var exprWithTargetNegated = context.MkAnd(expr1, context.MkNot(expr2));
			var expressionToCheck = context.MkOr(exprWithSourceNegated, exprWithTargetNegated);

			Solver s = context.MkSimpleSolver();
			s.Assert(expressionToCheck);

			var result = s.Check() == Status.UNSATISFIABLE;

			return result;
		}

		public static BoolExpr GetExistsExpression(
			this Context context,
			BoolExpr smtExpression,
			Dictionary<string, DomainType> overwrittenVarNames,
			VariableType existsVariableType = VariableType.Written)
		{
			var variablesToOverwrite = new Expr[overwrittenVarNames.Count];
			var currentArrayIndex = 0;
			foreach (var keyValuePair in overwrittenVarNames)
			{
				variablesToOverwrite[currentArrayIndex++] = context.GenerateExpression(keyValuePair.Key, keyValuePair.Value, existsVariableType);
			}

			if (variablesToOverwrite.Length > 0)
			{
				var existsExpression = context.MkExists(variablesToOverwrite, smtExpression);

				Goal g = context.MkGoal(true, false, false);
				g.Assert((BoolExpr)existsExpression);
				Tactic tac = context.MkTactic("qe_rec");
				ApplyResult a = tac.Apply(g);

				return context.SimplifyExpression(a.Subgoals[0].AsBoolExpr());
			}
			else
			{
				return smtExpression;
			}
		}

		public static Expr GenerateExpression(this Context context, string variableName, DomainType domain, VariableType varType)
		{
			var nameSuffix = varType == VariableType.Written
				? "_w"
				: "_r";

			return domain switch
			{
				DomainType.Integer => context.MkIntConst(variableName + nameSuffix),
				DomainType.Real => context.MkRealConst(variableName + nameSuffix),
				DomainType.Boolean => context.MkBoolConst(variableName + nameSuffix),
				_ => throw new NotImplementedException("Domain type is not supported yet"),
			};
		}

		private static BoolExpr Simplify(Context context, List<BoolExpr> simplifiedExpressions, ConnectExpressions connectAction)
		{
			var index = 0;

			while (index < simplifiedExpressions.Count)
			{
				var totalExpression = connectAction(simplifiedExpressions.ToArray());
				var cutExpression = connectAction(simplifiedExpressions.Except(new[] { simplifiedExpressions[index] }).ToArray());

				if (context.AreEqual(totalExpression, cutExpression))
				{
					simplifiedExpressions.RemoveAt(index);
				}
				else
				{
					index++;
				}
			}

			return connectAction(simplifiedExpressions.ToArray());
		}

		private static BoolExpr SimplifyDisjunction(Context context, List<BoolExpr> simplifiedExpressions)
		{
			return Simplify(context, simplifiedExpressions, context.MkOr);
		}

		private static BoolExpr SimplifyConjunction(Context context, List<BoolExpr> simplifiedExpressions)
		{
			return Simplify(context, simplifiedExpressions, context.MkAnd);
		}

		public static BoolExpr SimplifyRecursive(this Context context, BoolExpr expr)
		{
			var nnfTactic = context.MkTactic("nnf");
			var goal = context.MkGoal();
			goal.Assert(expr);
			expr = nnfTactic.Apply(goal).Subgoals[0].AsBoolExpr();
			
			return SimplifyRecursion(context, expr);
		}

		private static BoolExpr SimplifyRecursion(Context context, BoolExpr expr)
		{
			if (expr.IsAnd || expr.IsOr)
			{
				var simplifiedExpressions = new List<BoolExpr>(expr.Args.Length);
				foreach (BoolExpr arg in expr.Args)
				{
					var simplifiedArgExpression = SimplifyRecursion(context, arg);
					simplifiedExpressions.Add(simplifiedArgExpression);
				}

				return expr.IsAnd
					? SimplifyConjunction(context, simplifiedExpressions)
					: SimplifyDisjunction(context, simplifiedExpressions);
			}

			return expr;
		}

		public static BoolExpr SimplifyExpression(this Context context, BoolExpr expr)
		{
			var simplifyTactic =  context.MkTactic("ctx-simplify"); 

			var goal = context.MkGoal();
			goal.Assert(expr);
			var conditionToSet = simplifyTactic.Apply(goal).Subgoals[0].Simplify().AsBoolExpr();
			return conditionToSet;
		}
	}
}