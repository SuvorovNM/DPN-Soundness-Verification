using DPN.Models.DPNElements;
using DPN.Models.Extensions;
using Microsoft.Z3;
using System.Diagnostics;
using DPN.Models.Enums;

namespace DPN.Models.Abstractions
{
    public class ConstraintExpressionService
    {
        public Context Context { get; private set; }
        public ConstraintExpressionService(Context context)
        {
            Context = context;
        }

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

            Solver s = Context.MkSimpleSolver();
            s.Assert(expressionToCheck);

            var result = s.Check() == Status.UNSATISFIABLE;

            return result;
        }
        
        public bool DoesTargetCoverSource(BoolExpr? expressionSource, BoolExpr? expressionTarget)
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

            Solver s = Context.MkSimpleSolver();
            s.Assert(exprWithSourceNegated);

            var result = s.Check() == Status.UNSATISFIABLE;

            return result;
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

                Goal g = Context.MkGoal(true, true, false);
                g.Assert(existsExpression);
                Tactic tac = Context.MkTactic("qe");
                ApplyResult a = tac.Apply(g);
                var expressionWithRemovedOverwrittenVars = a.Subgoals[0].AsBoolExpr();


                foreach (var keyValuePair in overwrittenVars)
                {
                    var sourceVar = Context.GenerateExpression(keyValuePair.Key, keyValuePair.Value, VariableType.Written);
                    var targetVar = Context.GenerateExpression(keyValuePair.Key, keyValuePair.Value, VariableType.Read);

                    expressionWithRemovedOverwrittenVars = (BoolExpr)expressionWithRemovedOverwrittenVars.Substitute(sourceVar, targetVar);
                }
                resultBlockExpression = expressionWithRemovedOverwrittenVars;
            }

            var tactic = Context.MkTactic("ctx-simplify");

            var goal = Context.MkGoal();
            goal.Assert(resultBlockExpression);

            var result = tactic.Apply(goal);

            resultBlockExpression = (BoolExpr)result.Subgoals[0].Simplify().AsBoolExpr();

            return resultBlockExpression;
        }
    }
}
