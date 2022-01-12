using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataPetriNetOnSmt.Extensions;

namespace DataPetriNetOnSmt.SoundnessVerification
{
    public class ConstraintExpressionOperationService
    {
        private BoolExprImplicationService implicationService;
        public ConstraintExpressionOperationService()
        {
            implicationService = new BoolExprImplicationService();
        }

        public List<IConstraintExpression> GetInvertedReadExpression(List<IConstraintExpression> sourceExpression)
        {
            if (sourceExpression is null)
            {
                throw new ArgumentNullException(nameof(sourceExpression));
            }
            if (sourceExpression.Count == 0)
            {
                return sourceExpression;
            }

            var blocks = new List<List<IConstraintExpression>>();
            var expressionDuringExecution = new List<IConstraintExpression>(sourceExpression);
            do
            {
                var expressionBlock = CutFirstExpressionBlock(expressionDuringExecution)
                    .GetExpressionsOfType(VariableType.Read)
                    .Select(x => x.GetInvertedExpression())
                    .ToList();

                if (expressionBlock.Count > 0)
                {
                    blocks.Add(expressionBlock);
                }
            } while (expressionDuringExecution.Count > 0);

            var allCombinations = GetAllPossibleCombos(blocks);
            return MakeSingleExpressionListFromMultipleLists(allCombinations);
        }

        private static List<IConstraintExpression> MakeSingleExpressionListFromMultipleLists(List<List<IConstraintExpression>> allCombinations)
        {
            var resultExpression = new List<IConstraintExpression>();

            foreach (var combination in allCombinations.Where(x => x.Count > 0))
            {
                var firstExpressionInBlock = combination[0].Clone();
                firstExpressionInBlock.LogicalConnective = LogicalConnective.Or;
                resultExpression.Add(firstExpressionInBlock);

                for (int i = 1; i < combination.Count; i++)
                {
                    var currentExpression = combination[i].Clone();
                    currentExpression.LogicalConnective = LogicalConnective.And;
                    resultExpression.Add(currentExpression);
                }
            }
            if (resultExpression.Count > 0)
            {
                resultExpression[0].LogicalConnective = LogicalConnective.Empty;
            }

            return resultExpression;
        }

        private static List<List<IConstraintExpression>> GetAllPossibleCombos(List<List<IConstraintExpression>> expressions)
        {
            IEnumerable<List<IConstraintExpression>> combos = new[] { new List<IConstraintExpression>() };

            foreach (var inner in expressions)
                combos = from c in combos
                         from i in inner
                         select c.Union(new List<IConstraintExpression> { i }).ToList();

            return combos.ToList();
        }

        public BoolExpr ShortenExpression(BoolExpr expression)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return (BoolExpr)expression.Simplify();
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
            if (target.Count == 0)
            {
                return source;
            }

            // Presume that source does not have any 'not' expressions except for inequality
            var targetConstraintsDuringEvaluation = new List<IConstraintExpression>(target);
            var andBlockExpressions = new List<BoolExpr>();
            do
            {
                var currentTargetBlock = CutFirstExpressionBlock(targetConstraintsDuringEvaluation);

                var expressionsWithOverwrite = currentTargetBlock
                    .GetExpressionsOfType(VariableType.Written);
                var overwrittenVarNames = expressionsWithOverwrite
                    .Select(x => x.ConstraintVariable.Name + "_r")
                    .Distinct();

                // Presume that there is no OR-hierarcy: all ORs are only on the first level
                foreach (var sourceExpressionGroup in SplitSourceExpressionByOrDelimiter(source))
                {
                    // All source constraints + read constraints from target ones
                    var concatenatedExpressionGroup = sourceExpressionGroup
                        .Union(currentTargetBlock.Except(expressionsWithOverwrite)
                        .Select(x => x.GetSmtExpression(ContextProvider.Context)));

                    var expressionGroupWithImplications = new List<BoolExpr>(concatenatedExpressionGroup);
                    var bothOverwrittenExpressions = expressionsWithOverwrite.GetVOVExpressionsWithBothVarsOverwrittenByTransitionFiring();

                    // Firstly, it is needed to examine VoV-constraints where both vars are overwritten by a transition firing
                    AddImplicationsBasedOnWriteExpressions(concatenatedExpressionGroup, expressionGroupWithImplications, bothOverwrittenExpressions);

                    // Secondly, go by read expressions and make implications based on those which contain a variable overwritten by a transition firing
                    UpdateImplicationsBasedOnReadExpressions(overwrittenVarNames, concatenatedExpressionGroup, expressionGroupWithImplications);

                    andBlockExpressions.Add(GenerateAndBlockExpression(expressionsWithOverwrite.Except(bothOverwrittenExpressions), expressionGroupWithImplications));
                }
            } while (targetConstraintsDuringEvaluation.Count > 0);

            return andBlockExpressions.Count() == 1
                ? andBlockExpressions[0]
                : ContextProvider.Context.MkOr(andBlockExpressions);
        }

        private void UpdateImplicationsBasedOnReadExpressions(IEnumerable<string> overwrittenVarNames, IEnumerable<BoolExpr> concatenatedExpressionGroup, List<BoolExpr> expressionGroupWithImplications)
        {
            foreach (var sourceExpression in concatenatedExpressionGroup)
            {
                var expressionToInspect = sourceExpression.GetExpressionWithoutNotClause();

                if (expressionGroupWithImplications.Contains(sourceExpression) // Нужна ли эта проверка?
                    && expressionToInspect.Args.Any(x => overwrittenVarNames.Contains(x.ToString())))
                {
                    if (!expressionToInspect.Args.All(x => overwrittenVarNames.Contains(x.ToString()))
                        && !expressionToInspect.Args.Any(x => x.IsConst))
                    {
                        if (sourceExpression.IsEq) // Check - may not work!
                        {
                            UpdateExpressionsBasedOnReadEquality(
                                overwrittenVarNames,
                                concatenatedExpressionGroup,
                                expressionGroupWithImplications,
                                sourceExpression,
                                expressionToInspect);
                        }
                        if (sourceExpression.IsNot)
                        {
                            UpdateExpressionsBasedOnReadUnequality(
                                overwrittenVarNames,
                                concatenatedExpressionGroup,
                                expressionGroupWithImplications,
                                sourceExpression);
                        }
                        // Maybe we can add inequality when only one value is possible...
                        if (sourceExpression.IsLT || sourceExpression.IsLE)
                        {
                            // Find max by optimization
                            UpdateExpressionsBasedOnReadLessExpression(
                                overwrittenVarNames,
                                concatenatedExpressionGroup,
                                expressionGroupWithImplications,
                                sourceExpression);
                        }
                        if (sourceExpression.IsGT || sourceExpression.IsGE)
                        {
                            // Find min by optimization
                            UpdateExpressionsBasedOnReadGreaterExpression(
                                overwrittenVarNames,
                                concatenatedExpressionGroup,
                                expressionGroupWithImplications,
                                sourceExpression);
                        }
                    }
                    expressionGroupWithImplications.Remove(sourceExpression);
                }
            }
        }

        private void AddImplicationsBasedOnWriteExpressions(IEnumerable<BoolExpr> concatenatedExpressionGroup, List<BoolExpr> expressionGroupWithImplications, IEnumerable<IConstraintExpression> bothOverwrittenExpressions)
        {
            foreach (ConstraintVOVExpression overwriteExpr in bothOverwrittenExpressions)
            {
                var overwriteExpressionWithReadVars = overwriteExpr
                    .CloneAsReadExpression()
                    .GetSmtExpression(ContextProvider.Context);

                if (overwriteExpr.Predicate == BinaryPredicate.Equal)
                {
                    UpdateExpressionsBasedOnWrittenEquality(expressionGroupWithImplications, overwriteExpr, overwriteExpressionWithReadVars);
                }
                if (overwriteExpr.Predicate == BinaryPredicate.Unequal)
                {
                    UpdateExpressionsBasedOnWrittenUnequality(expressionGroupWithImplications, overwriteExpressionWithReadVars);
                }
                if (overwriteExpr.Predicate == BinaryPredicate.LessThan || overwriteExpr.Predicate == BinaryPredicate.LessThanOrEqual)
                {
                    UpdateExpressionsBasedOnWrittenLessThan(concatenatedExpressionGroup, expressionGroupWithImplications, overwriteExpr, overwriteExpressionWithReadVars);
                }
                if (overwriteExpr.Predicate == BinaryPredicate.GreaterThan || overwriteExpr.Predicate == BinaryPredicate.GreaterThanOrEqual)
                {
                    UpdateExpressionsBasedOnWrittenGreaterThan(concatenatedExpressionGroup, expressionGroupWithImplications, overwriteExpr, overwriteExpressionWithReadVars);
                }
            }
        }

        private void UpdateExpressionsBasedOnWrittenUnequality(List<BoolExpr> concatenatedExpressionGroup, BoolExpr overwriteExpressionWithReadVars)
        {
            var varToOverwrite = overwriteExpressionWithReadVars.Args[1];
            var secondVar = overwriteExpressionWithReadVars.Args[0];

            var newExpression = implicationService.GetImplicationOfInequalityExpression(concatenatedExpressionGroup,
                varToOverwrite,
                secondVar);

            if (newExpression != null)
            {
                concatenatedExpressionGroup.Add(newExpression);
            }
        }

        private void UpdateExpressionsBasedOnWrittenGreaterThan(IEnumerable<BoolExpr> concatenatedExpressionGroup, List<BoolExpr> expressionGroupWithImplications, ConstraintVOVExpression overwriteExpr, BoolExpr overwriteExpressionWithReadVars)
        {
            var varToOverwrite = overwriteExpressionWithReadVars.Args[1];
            var secondVar = overwriteExpressionWithReadVars.Args[0];

            var newExpression = implicationService.GetImplicationOfGreaterExpression(concatenatedExpressionGroup,
                overwriteExpr.Predicate == BinaryPredicate.GreaterThanOrEqual,
                varToOverwrite,
                secondVar);

            expressionGroupWithImplications.Add(newExpression);
        }

        private void UpdateExpressionsBasedOnWrittenLessThan(IEnumerable<BoolExpr> concatenatedExpressionGroup, List<BoolExpr> expressionGroupWithImplications, ConstraintVOVExpression overwriteExpr, BoolExpr overwriteExpressionWithReadVars)
        {
            var varToOverwrite = overwriteExpressionWithReadVars.Args[1];
            var secondVar = overwriteExpressionWithReadVars.Args[0];

            var newExpression = implicationService.GetImplicationOfLessExpression(
                concatenatedExpressionGroup,
                overwriteExpr.Predicate == BinaryPredicate.LessThanOrEqual,
                varToOverwrite,
                secondVar);

            expressionGroupWithImplications.Add(newExpression);
        }

        private void UpdateExpressionsBasedOnWrittenEquality(List<BoolExpr> expressionGroupWithImplications, ConstraintVOVExpression overwriteExpr, BoolExpr overwriteExpressionWithReadVars)
        {
            var addedExpressions = new List<BoolExpr>();
            foreach (var readExpression in expressionGroupWithImplications)
            {
                var expressionToInspect = readExpression.GetExpressionWithoutNotClause();

                if (expressionToInspect.Args.Any(x => x.ToString() == overwriteExpr.VariableToCompare.Name))
                {
                    var oldValue = expressionToInspect.Args.FirstOrDefault(x => overwriteExpr.VariableToCompare.Name != x.ToString());
                    BoolExpr newExpression = implicationService.GetImplicationOfEqualityExpression(
                        overwriteExpressionWithReadVars.Args[0],
                        readExpression.IsNot,
                        expressionToInspect,
                        oldValue);

                    addedExpressions.Add(newExpression);
                }
            }
            expressionGroupWithImplications.AddRange(addedExpressions);
        }

        private void UpdateExpressionsBasedOnReadGreaterExpression(
            IEnumerable<string> overwrittenVarNames,
            IEnumerable<BoolExpr> concatenatedExpressionGroup,
            List<BoolExpr> updatedExpression,
            BoolExpr sourceExpression)
        {
            var varToOverwrite = sourceExpression.Args.FirstOrDefault(x => overwrittenVarNames.Contains(x.ToString()));
            var secondVar = sourceExpression.Args.FirstOrDefault(x => !overwrittenVarNames.Contains(x.ToString()));

            var newExpression = implicationService.GetImplicationOfGreaterExpression(
                concatenatedExpressionGroup,
                sourceExpression.IsGE,
                varToOverwrite,
                secondVar);

            updatedExpression.Add(newExpression);
        }

        private void UpdateExpressionsBasedOnReadUnequality(
            IEnumerable<string> overwrittenVarNames,
            IEnumerable<BoolExpr> concatenatedExpressionGroup,
            List<BoolExpr> updatedExpression,
            BoolExpr sourceExpression)
        {
            var varToOverwrite = sourceExpression.Args[0].Args.FirstOrDefault(x => overwrittenVarNames.Contains(x.ToString()));
            var secondVar = sourceExpression.Args[0].Args.FirstOrDefault(x => !overwrittenVarNames.Contains(x.ToString()));

            var newExpression = implicationService.GetImplicationOfInequalityExpression(
                concatenatedExpressionGroup,
                varToOverwrite,
                secondVar);

            if (newExpression != null)
            {
                updatedExpression.Add(newExpression);
            }
        }

        private void UpdateExpressionsBasedOnReadLessExpression(
            IEnumerable<string> overwrittenVarNames,
            IEnumerable<BoolExpr> concatenatedExpressionGroup,
            List<BoolExpr> updatedExpression,
            BoolExpr sourceExpression)
        {
            var varToOverwrite = sourceExpression.Args.FirstOrDefault(x => overwrittenVarNames.Contains(x.ToString()));
            var secondVar = sourceExpression.Args.FirstOrDefault(x => !overwrittenVarNames.Contains(x.ToString()));

            var newExpression = implicationService.GetImplicationOfLessExpression(
                concatenatedExpressionGroup,
                sourceExpression.IsLE,
                varToOverwrite,
                secondVar);

            updatedExpression.Add(newExpression);
        }

        private void UpdateExpressionsBasedOnReadEquality(
            IEnumerable<string> overwrittenVarNames,
            IEnumerable<BoolExpr> concatenatedExpressionGroup,
            List<BoolExpr> updatedExpression,
            BoolExpr sourceExpression,
            BoolExpr? expressionToInspect)
        {
            var varToOverwrite = expressionToInspect.Args.FirstOrDefault(x => overwrittenVarNames.Contains(x.ToString()));
            var secondVar = expressionToInspect.Args.FirstOrDefault(x => !overwrittenVarNames.Contains(x.ToString()));

            var allBoolExpressionsWithOverwrittenVar = concatenatedExpressionGroup // Clarify, does it work
                .Where(x => (x.Args.Contains(varToOverwrite) && !expressionToInspect.Args.All(x => overwrittenVarNames.Contains(x.ToString())))
                    || (x.IsNot && x.Args[0].Args.Contains(varToOverwrite) && !expressionToInspect.Args[0].Args.All(x => overwrittenVarNames.Contains(x.ToString()))))
                .Except(new[] { sourceExpression })
                .ToList();

            foreach (var expression in allBoolExpressionsWithOverwrittenVar)
            {
                // Make a copy of expr - obliged to create new expressions
                var expressionToReplace = expression.GetExpressionWithoutNotClause();

                var operandToSave = expressionToReplace.Args[0] == varToOverwrite ? 1 : 0;// Take var/const opposite to varToOverwrite

                var newExpression = implicationService.GetImplicationOfEqualityExpression(
                                        secondVar,
                                        expression.IsNot,
                                        expressionToInspect,
                                        expressionToReplace.Args[operandToSave]);

                updatedExpression.Add(newExpression);

                if (updatedExpression.Contains(expression))
                {
                    updatedExpression.Remove(expression);
                }
            }
        }

        private static BoolExpr GenerateAndBlockExpression(IEnumerable<IConstraintExpression> expressionsWithOverwrite, IEnumerable<BoolExpr> updatedExpression)
        {
            var targetExprList = new List<BoolExpr>();
            foreach (var targetExpr in expressionsWithOverwrite)
            {
                // Write vars must become read ones
                targetExprList.Add(targetExpr.CloneAsReadExpression().GetSmtExpression(ContextProvider.Context));
            }
            targetExprList.AddRange(updatedExpression);

            return ContextProvider.Context.MkAnd(targetExprList);
        }

        private static IEnumerable<BoolExpr[]> SplitSourceExpressionByOrDelimiter(BoolExpr source)
        {
            if (!source.IsOr)
            {
                var expressions = source.Args.Select(x => x as BoolExpr).ToArray();
                return new List<BoolExpr[]> { expressions };
            }

            var appliedTactic = ContextProvider.Context.MkTactic("split-clause");

            var goalToMakeOrSplit = ContextProvider.Context.MkGoal(true);
            goalToMakeOrSplit.Assert(source);
            var applyResult = appliedTactic.Apply(goalToMakeOrSplit);

            return applyResult.Subgoals.Select(x => x.Formulas);
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
