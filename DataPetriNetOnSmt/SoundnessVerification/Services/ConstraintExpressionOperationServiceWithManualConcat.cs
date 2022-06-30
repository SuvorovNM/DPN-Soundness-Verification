using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.Extensions;
using Microsoft.Z3;

namespace DataPetriNetOnSmt.SoundnessVerification.Services
{
    public class ConstraintExpressionOperationServiceWithManualConcat : AbstractConstraintExpressionService
    {
        private readonly BoolExprImplicationService implicationService;

        public ConstraintExpressionOperationServiceWithManualConcat()
        {
            implicationService = new BoolExprImplicationService();
        }

        public override BoolExpr ConcatExpressions(BoolExpr source, List<IConstraintExpression> target, bool removeRedundantBlocks = false)
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

                    var andBlockExpression = GenerateAndBlockExpression(expressionsWithOverwrite.Except(bothOverwrittenExpressions), expressionGroupWithImplications);

                    if (removeRedundantBlocks)
                    {
                        var solver = ContextProvider.Context.MkSimpleSolver();
                        solver.Add(andBlockExpression);
                        if (solver.Check() == Status.SATISFIABLE)
                        {
                            andBlockExpressions.Add(andBlockExpression);
                        }
                    }
                    else
                    {
                        andBlockExpressions.Add(andBlockExpression);
                    }
                }
            } while (targetConstraintsDuringEvaluation.Count > 0);

            return andBlockExpressions.Count() == 1
                ? andBlockExpressions[0]
                : ContextProvider.Context.MkOr(andBlockExpressions);
        }

        private void UpdateImplicationsBasedOnReadExpressions(IEnumerable<string> overwrittenVarNames, IEnumerable<BoolExpr> concatenatedExpressionGroup, List<BoolExpr> expressionGroupWithImplications)
        {
            var expressionsToRemove = new List<BoolExpr>();
            var expressionsUpdated = false;
            var finalImplications = new List<BoolExpr>();

            finalImplications.AddRange(expressionGroupWithImplications.Skip(concatenatedExpressionGroup.Count())
                //.Except(concatenatedExpressionGroup)
                .Where(x => x.Args.Any(y => !overwrittenVarNames.Contains(y.ToString()))));

            foreach (var implication in finalImplications)
            {
                expressionGroupWithImplications.Remove(implication);
            }

            do
            {
                expressionsUpdated = false;
                var expressionsToExamine = new List<BoolExpr>(expressionGroupWithImplications);
                foreach (var sourceExpression in expressionsToExamine)
                {
                    var expressionToInspect = sourceExpression.GetExpressionWithoutNotClause();

                    if (expressionToInspect.Args.Any(x => overwrittenVarNames.Contains(x.ToString())))
                    //&& expressionGroupWithImplications.Contains(sourceExpression) // Нужна ли эта проверка?
                    {
                        if (expressionToInspect.Args.All(x => x.IsConst))
                        {
                            if (sourceExpression.IsEq) // Check - may not work!
                            {
                                UpdateExpressionsBasedOnReadEquality(
                                    overwrittenVarNames,
                                    concatenatedExpressionGroup,
                                    expressionGroupWithImplications,
                                    sourceExpression,
                                    expressionToInspect,
                                    finalImplications);
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
                        expressionsToRemove.Add(sourceExpression);
                    }
                }
                expressionsToRemove.ForEach(x => expressionGroupWithImplications.Remove(x));
                if (!expressionGroupWithImplications.SequenceEqual(expressionsToExamine))
                {
                    expressionsUpdated = true;
                }
            } while (expressionsUpdated);
            expressionGroupWithImplications.AddRange(finalImplications);
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
            var varToOverwrite = overwriteExpressionWithReadVars.Args[0].Args[1];
            var secondVar = overwriteExpressionWithReadVars.Args[0].Args[0];

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

            if (!newExpression.IsTrue)
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

            if (!newExpression.IsTrue)
                expressionGroupWithImplications.Add(newExpression);
        }

        private void UpdateExpressionsBasedOnWrittenEquality(List<BoolExpr> expressionGroupWithImplications, ConstraintVOVExpression overwriteExpr, BoolExpr overwriteExpressionWithReadVars)
        {
            var overwriteVarName = overwriteExpr.VariableToCompare.Name + "_r";
            bool addedExpressionsListUpdated;
            do
            {
                var addedExpressions = new List<BoolExpr>();
                addedExpressionsListUpdated = false;
                foreach (var readExpression in expressionGroupWithImplications)
                {
                    var expressionToInspect = readExpression.GetExpressionWithoutNotClause();

                    if (expressionToInspect.Args.Any(x => x.ToString() == overwriteVarName))
                    {
                        var oldValue = expressionToInspect.Args.FirstOrDefault(x => overwriteVarName != x.ToString());

                        if (oldValue.ToString() != overwriteExpressionWithReadVars.Args[0].ToString())
                        {
                            BoolExpr newExpression = implicationService.GetImplicationOfEqualityExpression(
                                overwriteExpressionWithReadVars.Args[0],
                                readExpression.IsNot,
                                expressionToInspect,
                                oldValue);

                            if (!expressionGroupWithImplications.Select(x => x.ToString()).Contains(newExpression.ToString()))
                            {
                                addedExpressions.Add(newExpression);
                                addedExpressionsListUpdated = true;
                            }
                        }
                    }
                }
                expressionGroupWithImplications.AddRange(addedExpressions);
            } while (addedExpressionsListUpdated);
        }

        private void UpdateExpressionsBasedOnReadGreaterExpression(
            IEnumerable<string> overwrittenVarNames,
            IEnumerable<BoolExpr> concatenatedExpressionGroup,
            List<BoolExpr> updatedExpression,
            BoolExpr sourceExpression)
        {
            var varToOverwrite = sourceExpression.Args.FirstOrDefault(x => overwrittenVarNames.Contains(x.ToString()));
            var secondVar = sourceExpression.Args.FirstOrDefault(x => !overwrittenVarNames.Contains(x.ToString()));

            var newExpression = sourceExpression.Args[0] == secondVar
                ? implicationService.GetImplicationOfGreaterExpression(
                concatenatedExpressionGroup,
                sourceExpression.IsGE,
                varToOverwrite,
                secondVar)
                : implicationService.GetImplicationOfLessExpression(
                concatenatedExpressionGroup,
                sourceExpression.IsLE,
                varToOverwrite,
                secondVar);

            if (!newExpression.IsTrue && !updatedExpression.Select(x => x.ToString()).Contains(newExpression.ToString()))
                updatedExpression.Add(newExpression);
        }

        private void UpdateExpressionsBasedOnReadUnequality(
            IEnumerable<string> overwrittenVarNames,
            IEnumerable<BoolExpr> concatenatedExpressionGroup,
            List<BoolExpr> updatedExpression,
            BoolExpr sourceExpression)
        {
            var varToOverwrite = sourceExpression.Args[0].Args.FirstOrDefault(x => overwrittenVarNames.Contains(x.ToString()));
            var secondVar = sourceExpression.Args[0].Args.FirstOrDefault(x => x != varToOverwrite);

            var newExpression = implicationService.GetImplicationOfInequalityExpression(
                concatenatedExpressionGroup,
                varToOverwrite,
                secondVar);

            if (newExpression != null && !updatedExpression.Select(x => x.ToString()).Contains(newExpression.ToString()))
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

            var newExpression = sourceExpression.Args[0] == secondVar
                ? implicationService.GetImplicationOfLessExpression(
                concatenatedExpressionGroup,
                sourceExpression.IsLE,
                varToOverwrite,
                secondVar)
                : implicationService.GetImplicationOfGreaterExpression(
                concatenatedExpressionGroup,
                sourceExpression.IsLE,
                varToOverwrite,
                secondVar);

            if (!newExpression.IsTrue && !updatedExpression.Select(x => x.ToString()).Contains(newExpression.ToString()))
                updatedExpression.Add(newExpression);
        }

        private void UpdateExpressionsBasedOnReadEquality(
            IEnumerable<string> overwrittenVarNames,
            IEnumerable<BoolExpr> concatenatedExpressionGroup,
            List<BoolExpr> updatedExpression,
            BoolExpr sourceExpression,
            BoolExpr? expressionToInspect,
            List<BoolExpr> finalImplications)
        {
            var varToOverwrite = expressionToInspect.Args.FirstOrDefault(x => overwrittenVarNames.Contains(x.ToString()));
            var secondVar = expressionToInspect.Args.FirstOrDefault(x => x != varToOverwrite);

            UpdateExpressionsByEqualityForGivenVars(overwrittenVarNames, concatenatedExpressionGroup, updatedExpression, sourceExpression, varToOverwrite, secondVar, finalImplications);

            if (overwrittenVarNames.Contains(secondVar.ToString()))
            {
                UpdateExpressionsByEqualityForGivenVars(overwrittenVarNames, concatenatedExpressionGroup, updatedExpression, sourceExpression, secondVar, varToOverwrite, finalImplications);
            }
        }

        private void UpdateExpressionsByEqualityForGivenVars(
            IEnumerable<string> overwrittenVarNames,
            IEnumerable<BoolExpr> concatenatedExpressionGroup,
            List<BoolExpr> updatedExpression,
            BoolExpr sourceExpression,
            Expr? varToOverwrite,
            Expr? secondVar,
            List<BoolExpr> finalImplications)
        {
            var allBoolExpressionsWithOverwrittenVar = concatenatedExpressionGroup // Clarify, does it work
                            .Where(x => x.Args.Contains(varToOverwrite)
                                || x.IsNot && x.Args[0].Args.Contains(varToOverwrite))
                            .Except(new[] { sourceExpression })
                            .ToList();

            foreach (var expression in allBoolExpressionsWithOverwrittenVar)
            {
                // Make a copy of expr - obliged to create new expressions
                var expressionToReplace = expression.GetExpressionWithoutNotClause();

                var operandToSave = expressionToReplace.Args[0] == varToOverwrite ? 1 : 0;// Take var/const opposite to varToOverwrite

                if (secondVar != expressionToReplace.Args[operandToSave])
                {
                    var newExpression = implicationService.GetImplicationOfEqualityExpression(
                                            secondVar,
                                            expression.IsNot,
                                            expressionToReplace,
                                            expressionToReplace.Args[operandToSave]);

                    if (!updatedExpression.Select(x => x.ToString()).Contains(newExpression.ToString()) &&
                        newExpression.Args.All(x => x.IsConst))
                    {
                        updatedExpression.Add(newExpression);
                    }
                    else
                    {
                        if (!finalImplications.Select(x => x.ToString()).Contains(newExpression.ToString()) && newExpression.Args.Any(x => !x.IsConst))
                        {
                            finalImplications.Add(newExpression);
                        }
                    }

                    if (updatedExpression.Contains(expression))
                    {
                        updatedExpression.Remove(expression);
                    }
                }
            }
        }

        private static BoolExpr GenerateAndBlockExpression(IEnumerable<IConstraintExpression> expressionsWithOverwrite, IEnumerable<BoolExpr> updatedExpression)
        {
            var targetExprList = new List<BoolExpr>(updatedExpression);
            foreach (var targetExpr in expressionsWithOverwrite)
            {
                // Write vars must become read ones
                targetExprList.Add(targetExpr.CloneAsReadExpression().GetSmtExpression(ContextProvider.Context));
            }

            return targetExprList.Count > 0
                ? ContextProvider.Context.MkAnd(targetExprList)
                : ContextProvider.Context.MkTrue();
        }
    }
}
