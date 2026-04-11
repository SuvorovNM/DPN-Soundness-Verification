using DPN.Models.Extensions;
using Microsoft.Z3;
using DPN.Models.Enums;

namespace DPN.Models.Abstractions
{
    public class ConstraintExpressionService(Context context)
    {
        public Context Context { get; private set; } = context;

        public bool CanBeSatisfied(BoolExpr expression)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            Solver s = Context.MkSimpleSolver();
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
            var exprWithSourceNegated = Context.MkAnd(Context.MkNot(expressionSource), expressionTarget);
            var exprWithTargetNegated = Context.MkAnd(expressionSource, Context.MkNot(expressionTarget));
            var expressionToCheck = Context.MkOr(exprWithSourceNegated, exprWithTargetNegated);

            return !Context.CanBeSatisfied(expressionToCheck);
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


            var andExpression = Context.MkAnd(source, target);
            BoolExpr resultBlockExpression = andExpression;

            if (overwrittenVars.Count > 0)
            {
                var variablesToOverwrite = new Expr[overwrittenVars.Count];
                var currentArrayIndex = 0;
                foreach (var keyValuePair in overwrittenVars)
                {
	                variablesToOverwrite[currentArrayIndex++] = Context.GenerateExpression(keyValuePair.Key, keyValuePair.Value, VariableType.Read);
                }

                var existsExpression = Context.MkExists(variablesToOverwrite, andExpression);

                Goal g = Context.MkGoal(true, false, false);
                g.Assert(existsExpression);
                Params qeParams = context.MkParams();
                Tactic tac = Context.MkTactic("qe_rec");
                ApplyResult a = tac.Apply(g, qeParams);
                
                var expressionWithRemovedOverwrittenVars = a.Subgoals[0].AsBoolExpr();


                foreach (var keyValuePair in overwrittenVars)
                {
                    var sourceVar = Context.GenerateExpression(keyValuePair.Key, keyValuePair.Value, VariableType.Written);
                    var targetVar = Context.GenerateExpression(keyValuePair.Key, keyValuePair.Value, VariableType.Read);

                    expressionWithRemovedOverwrittenVars = (BoolExpr)expressionWithRemovedOverwrittenVars.Substitute(sourceVar, targetVar);
                }
                resultBlockExpression = expressionWithRemovedOverwrittenVars;
            }

            return Context.SimplifyExpression(resultBlockExpression);
        }

        private bool CheckWhetherVariableIsAlwaysTrue(KeyValuePair<string, DomainType> keyValuePair, BoolExpr andExpression)
        {
	        var writtenVariable = Context.GenerateExpression(keyValuePair.Key, keyValuePair.Value, VariableType.Written);
	        var freshVariable = Context.GenerateExpression(keyValuePair.Key+"fresh", keyValuePair.Value, VariableType.Written);
                    
	        Expr expr1 = andExpression.Substitute(writtenVariable, freshVariable);
	        // Check if expr is equivalent regardless of the variable
	        BoolExpr equiv = Context.MkEq(andExpression, expr1);
	        var solver = Context.MkSolver();
	        solver.Assert(Context.MkNot(equiv));

	        return solver.Check() == Status.SATISFIABLE;
        }
    }
}
