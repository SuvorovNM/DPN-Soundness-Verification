using DPN.Models.Extensions;
using Microsoft.Z3;
using DPN.Models.Enums;

namespace DPN.Models.Abstractions
{
	public class ConstraintExpressionService(Context context)
	{
		public bool CanBeSatisfied(BoolExpr expression)
		{
			if (expression is null)
			{
				throw new ArgumentNullException(nameof(expression));
			}

			var s = context.MkSimpleSolver();
			s.Assert(expression);

			var result = s.Check() == Status.SATISFIABLE;

			return result;
		}

		public bool AreEqual(BoolExpr? expressionSource, BoolExpr? expressionTarget)
		{
			if (expressionSource is null)
			{
				throw new ArgumentNullException(nameof(expressionSource));
			}

			if (expressionTarget is null)
			{
				throw new ArgumentNullException(nameof(expressionTarget));
			}

			// 2 expressions are equal if [(not(x) and y) or (x and not(y))] is not satisfiable
			var exprWithSourceNegated = context.MkAnd(context.MkNot(expressionSource), expressionTarget);
			var exprWithTargetNegated = context.MkAnd(expressionSource, context.MkNot(expressionTarget));
			var expressionToCheck = context.MkOr(exprWithSourceNegated, exprWithTargetNegated);

			return !context.CanBeSatisfied(expressionToCheck);
		}

		public BoolExpr ConcatExpressions(
			BoolExpr? source,
			BoolExpr? target,
			Dictionary<string, DomainType> overwrittenVars)
		{
			if (source is null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (target is null)
			{
				throw new ArgumentNullException(nameof(target));
			}


			var andExpression = context.MkAnd(source, target);
			var resultBlockExpression = andExpression;

			if (overwrittenVars.Count > 0)
			{
				var variablesToOverwrite = new Expr[overwrittenVars.Count];
				var currentArrayIndex = 0;
				foreach (var keyValuePair in overwrittenVars)
				{
					variablesToOverwrite[currentArrayIndex++] = context.GenerateExpression(keyValuePair.Key, keyValuePair.Value, VariableType.Read);
				}

				var existsExpression = context.MkExists(variablesToOverwrite, andExpression);

				var g = context.MkGoal(true, false, false);
				g.Assert(existsExpression);
				var qeParams = context.MkParams();
				var tac = context.MkTactic("qe_rec");
				var a = tac.Apply(g, qeParams);

				var expressionWithRemovedOverwrittenVars = a.Subgoals[0].AsBoolExpr();


				foreach (var keyValuePair in overwrittenVars)
				{
					var sourceVar = context.GenerateExpression(keyValuePair.Key, keyValuePair.Value, VariableType.Written);
					var targetVar = context.GenerateExpression(keyValuePair.Key, keyValuePair.Value, VariableType.Read);

					expressionWithRemovedOverwrittenVars = (BoolExpr)expressionWithRemovedOverwrittenVars.Substitute(sourceVar, targetVar);
				}

				resultBlockExpression = expressionWithRemovedOverwrittenVars;
			}

			return context.SimplifyExpression(resultBlockExpression);
		}

		// Check if expr is equivalent regardless of the variable
		private bool CheckWhetherVariableIsAlwaysTrue(KeyValuePair<string, DomainType> keyValuePair, BoolExpr andExpression)
		{
			var writtenVariable = context.GenerateExpression(keyValuePair.Key, keyValuePair.Value, VariableType.Written);
			var freshVariable = context.GenerateExpression(keyValuePair.Key + "fresh", keyValuePair.Value, VariableType.Written);

			var expressionWithFreshVariable = andExpression.Substitute(writtenVariable, freshVariable);
			var solver = context.MkSolver();
			solver.Assert(context.MkNot(context.MkEq(andExpression, expressionWithFreshVariable)));

			return solver.Check() == Status.SATISFIABLE;
		}
	}
}