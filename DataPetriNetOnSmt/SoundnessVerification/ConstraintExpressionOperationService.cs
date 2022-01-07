using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.SoundnessVerification
{
    public class ConstraintExpressionOperationService
    {
        public ConstraintExpressionOperationService()
        {

        }
        public List<IConstraintExpression> InverseExpression(List<IConstraintExpression> expression)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var result = new List<IConstraintExpression>();
            foreach (var item in expression)
            {
                result.Add(item.GetInvertedExpression());
            }

            return result;
        }

        public List<IConstraintExpression> ShortenExpression(BoolExpr expression)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }           

            throw new NotImplementedException();
        }

        public bool CanBeSatisfied(BoolExpr expression)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            Solver s = ContextProvider.Context.MkSolver();
            s.Assert(expression);

            return s.Check() == Status.SATISFIABLE;
        }

        public bool AreEqual(BoolExpr expressionSource, BoolExpr expressionTarget)
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
            var exprWithSourceNegated = ContextProvider.Context.MkAnd(ContextProvider.Context.MkNot(expressionSource), expressionTarget);
            var exprWithTargetNegated = ContextProvider.Context.MkAnd(expressionSource, ContextProvider.Context.MkNot(expressionTarget));
            var expressionToCheck = ContextProvider.Context.MkOr(exprWithSourceNegated, exprWithTargetNegated);

            Solver s = ContextProvider.Context.MkSolver();
            s.Assert(expressionToCheck);

            return s.Check() == Status.UNSATISFIABLE;
        }

        public BoolExpr ConcatExpressions(BoolExpr source, List<IConstraintExpression> target)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (target is null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            // Presume that source does not have any 'not' expressions except for inequality
            // Use old way to get 1 lvl of OR-hierarchy
            var targetConstraintsDuringEvaluation = new List<IConstraintExpression>(target);
            BoolExpr resultExpression;
            do
            {
                BoolExpr blockExpression;
                var currentTargetBlock = CutFirstExpressionBlock(targetConstraintsDuringEvaluation);

                var expressionsWithOverwrite = target
                    .Where(x => x.ConstraintVariable.VariableType == VariableType.Written)
                    .ToList();
                var overwrittenVarNames = expressionsWithOverwrite
                    .Select(x => x.ConstraintVariable.Name + "_r")
                    .Distinct();

                var sourceExpressionBlocks = SplitSourceExpressionByOrDelimiter(source); // TODO: Check correctness
                foreach (var expressionGroup in sourceExpressionBlocks)
                {
                    var updatedExpression = new List<BoolExpr>(expressionGroup);
                    foreach (var sourceExpression in expressionGroup)
                    {
                        var expressionToInspect = sourceExpression.IsNot
                            ? sourceExpression.Args[0] as BoolExpr
                            : sourceExpression;

                        if (updatedExpression.Contains(sourceExpression) 
                            && expressionToInspect.Args.Any(x => overwrittenVarNames.Contains(x.ToString())))
                        {
                            if (expressionToInspect.Args.Any(x => x.IsConst)) // Var-Oper-Const
                            {
                                updatedExpression.Remove(sourceExpression);
                            }
                            else // Var-Oper-Var
                            {
                                if (expressionToInspect.Args.All(x => overwrittenVarNames.Contains(x.ToString())))
                                {
                                    updatedExpression.Remove(sourceExpression);
                                }

                                if (sourceExpression.IsEq) // Check - may not work!
                                {
                                    var varToOverwrite = sourceExpression.Args.FirstOrDefault(x => !overwrittenVarNames.Contains(x.ToString()));
                                    var secondVar = sourceExpression.Args.FirstOrDefault(x => !overwrittenVarNames.Contains(x.ToString()));

                                    var allBoolExpressionsWithOverwrittenVar = expressionGroup // Clarify, does it work
                                        .Where(x => (x.Args.Contains(varToOverwrite) && !expressionToInspect.Args.All(x => overwrittenVarNames.Contains(x.ToString()))) 
                                            || (x.IsNot && x.Args[0].Args.Contains(varToOverwrite) && !expressionToInspect.Args[0].Args.All(x => overwrittenVarNames.Contains(x.ToString()))))
                                        .Except(new[] { sourceExpression })
                                        .ToList();                                    

                                    foreach(var expression in allBoolExpressionsWithOverwrittenVar)
                                    {
                                        // Make a copy of expr - obliged to create new expressions
                                        
                                    }
                                }
                                if (sourceExpression.IsNot)
                                {
                                    updatedExpression.Remove(sourceExpression);
                                }
                                if (sourceExpression.IsLT)
                                {
                                    // Find max by optimization
                                }
                                if (sourceExpression.IsLE)
                                {
                                    // Find max by optimization
                                }
                                if (sourceExpression.IsGT)
                                {
                                    // Find min by optimization
                                }
                                if (sourceExpression.IsGE)
                                {
                                    // Find min by optimization
                                }
                            }
                        }
                    }
                }

            } while (targetConstraintsDuringEvaluation.Count > 0);

            throw new NotImplementedException();
        }

        private static IEnumerable<BoolExpr[]> SplitSourceExpressionByOrDelimiter(BoolExpr source)
        {
            var appliedTactic = ContextProvider.Context.MkTactic("split-clause");

            var goalToMakeOrSplit = ContextProvider.Context.MkGoal(true);
            goalToMakeOrSplit.Assert(source);
            var applyResult = appliedTactic.Apply(goalToMakeOrSplit);

            var sourceExpressionBlocks = applyResult.Subgoals.Select(x => x.Formulas);
            return sourceExpressionBlocks;
        }

        private static List<IConstraintExpression> CutFirstExpressionBlock(List<IConstraintExpression> sourceConstraintsDuringEvaluation)
        {
            if (sourceConstraintsDuringEvaluation.Count == 0)
            {
                return new List<IConstraintExpression>();
            }

            List<IConstraintExpression> currentBlock;
            var delimiter = Guard.GetDelimiter(sourceConstraintsDuringEvaluation);

            currentBlock = new List<IConstraintExpression>(sourceConstraintsDuringEvaluation.GetRange(0, delimiter));
            sourceConstraintsDuringEvaluation.RemoveRange(0, delimiter);
            return currentBlock;
        }
    }
}
