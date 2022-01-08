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

namespace DataPetriNetOnSmt.SoundnessVerification
{
    public class ConstraintExpressionOperationService
    {
        private static long integerMax = long.MaxValue;
        private static long integerMin = long.MinValue;
        private static double realMax = 99999999999999;
        private static double realMin = -99999999999999;

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
            if (target.Count == 0)
            {
                return source;
            }

            // Presume that source does not have any 'not' expressions except for inequality
            // Use old way to get 1 lvl of OR-hierarchy
            var targetConstraintsDuringEvaluation = new List<IConstraintExpression>(target);
            var andBlockExpressions = new List<BoolExpr>();
            do
            {
                var currentTargetBlock = CutFirstExpressionBlock(targetConstraintsDuringEvaluation);

                var expressionsWithOverwrite = currentTargetBlock
                    .Where(x => x.ConstraintVariable.VariableType == VariableType.Written)
                    .ToList();
                var overwrittenVarNames = expressionsWithOverwrite
                    .Select(x => x.ConstraintVariable.Name + "_r")
                    .Distinct();

                var sourceExpressionBlocks = SplitSourceExpressionByOrDelimiter(source); // TODO: Check correctness
                foreach (var expressionGroup in sourceExpressionBlocks)
                {
                    // All source constraints + read constraints from target ones
                    var concatenatedExpressionGroup = expressionGroup
                        .Union(currentTargetBlock.Except(expressionsWithOverwrite)
                        .Select(x => x.GetSmtExpression(ContextProvider.Context)));

                    var updatedExpression = new List<BoolExpr>(concatenatedExpressionGroup);

                    var bothOverwrittenExpressions = expressionsWithOverwrite
                        .Where(x => x as ConstraintVOVExpression != null
                            && expressionsWithOverwrite
                                .Select(x => x.ConstraintVariable.Name)
                                .Contains((x as ConstraintVOVExpression).VariableToCompare.Name));

                    // Add execution of VOV where both vars are overwritten- not add them at last stages
                    foreach (ConstraintVOVExpression overwriteExpr in bothOverwrittenExpressions)
                    {
                        var overwriteExpressionWithReadVars = overwriteExpr.Clone();
                        overwriteExpressionWithReadVars.ConstraintVariable = new ConstraintVariable
                        {
                            VariableType = VariableType.Read,
                            Domain = overwriteExpr.ConstraintVariable.Domain,
                            Name = overwriteExpr.ConstraintVariable.Name,
                        };

                        var expressionToAdd = overwriteExpressionWithReadVars.GetSmtExpression(ContextProvider.Context);

                        if (overwriteExpr.Predicate == BinaryPredicate.Equal)
                        {
                            var addedExpressions = new List<BoolExpr>();
                            foreach (var readExpression in updatedExpression)
                            {
                                var expressionToInspect = readExpression.IsNot
                                    ? readExpression.Args[0] as BoolExpr
                                    : readExpression;

                                if (expressionToInspect.Args.Any(x=> x.ToString() == overwriteExpr.VariableToCompare.Name))
                                {
                                    var varToOverwrite = expressionToInspect.Args.FirstOrDefault(x => overwriteExpr.VariableToCompare.Name == x.ToString());
                                    var oldValue = expressionToInspect.Args.FirstOrDefault(x => x != varToOverwrite);

                                    var operandToSave = expressionToInspect.Args[0] == oldValue
                                        ? 0
                                        : 1;

                                    BoolExpr newExpression = null;
                                    if (expressionToInspect.IsEq)
                                    {
                                        newExpression = ContextProvider.Context.MkEq(expressionToAdd.Args[0], oldValue);
                                    }
                                    if (expressionToInspect.IsGT)
                                    {
                                        newExpression = operandToSave == 0
                                            ? ContextProvider.Context.MkGt((ArithExpr)oldValue, (ArithExpr)expressionToAdd.Args[0])
                                            : ContextProvider.Context.MkGt((ArithExpr)expressionToAdd.Args[0], (ArithExpr)oldValue);
                                    }
                                    if (expressionToInspect.IsGE)
                                    {
                                        newExpression = operandToSave == 0
                                           ? ContextProvider.Context.MkGe((ArithExpr)oldValue, (ArithExpr)expressionToAdd.Args[0])
                                           : ContextProvider.Context.MkGe((ArithExpr)expressionToAdd.Args[0], (ArithExpr)oldValue);
                                    }
                                    if (expressionToInspect.IsLE)
                                    {
                                        newExpression = operandToSave == 0
                                           ? ContextProvider.Context.MkLe((ArithExpr)oldValue, (ArithExpr)expressionToAdd.Args[0])
                                           : ContextProvider.Context.MkLe((ArithExpr)expressionToAdd.Args[0], (ArithExpr)oldValue);
                                    }
                                    if (expressionToInspect.IsLT)
                                    {
                                        newExpression = operandToSave == 0
                                            ? ContextProvider.Context.MkLt((ArithExpr)oldValue, (ArithExpr)expressionToAdd.Args[0])
                                            : ContextProvider.Context.MkLt((ArithExpr)expressionToAdd.Args[0], (ArithExpr)oldValue);
                                    }

                                    if (readExpression.IsNot)
                                    {
                                        newExpression = ContextProvider.Context.MkNot(newExpression);
                                    }

                                    addedExpressions.Add(newExpression);
                                }
                            }
                            updatedExpression.AddRange(addedExpressions);
                        }
                        if (overwriteExpr.Predicate == BinaryPredicate.LessThan || overwriteExpr.Predicate == BinaryPredicate.LessThanOrEqual)
                        {
                            var varToOverwrite = expressionToAdd.Args[1];
                            var optimizer = SetOptimizer(concatenatedExpressionGroup, varToOverwrite);

                            optimizer.MkMaximize(varToOverwrite);
                            if (optimizer.Check() == Status.SATISFIABLE)
                            {
                                var maxVarValue = optimizer.Model.Consts
                                    .FirstOrDefault(x => x.Key.Name.ToString() == varToOverwrite.ToString())
                                    .Value;

                                if (maxVarValue.IsRatNum && maxVarValue.ToString() != realMax.ToString()
                                    || maxVarValue.IsIntNum && maxVarValue.ToString() != integerMax.ToString())
                                {
                                    var newExpression = overwriteExpr.Predicate == BinaryPredicate.LessThan
                                        ? ContextProvider.Context.MkLt((ArithExpr)expressionToAdd.Args[0], (ArithExpr)maxVarValue)
                                        : ContextProvider.Context.MkLe((ArithExpr)expressionToAdd.Args[0], (ArithExpr)maxVarValue);
                                    updatedExpression.Add(newExpression);
                                }
                            }
                        }
                        if (overwriteExpr.Predicate == BinaryPredicate.GreaterThan || overwriteExpr.Predicate == BinaryPredicate.GreaterThanOrEqual)
                        {
                            var varToOverwrite = expressionToAdd.Args[1];
                            var optimizer = SetOptimizer(concatenatedExpressionGroup, varToOverwrite);

                            optimizer.MkMinimize(varToOverwrite);
                            if (optimizer.Check() == Status.SATISFIABLE)
                            {
                                var minVarValue = optimizer.Model.Consts
                                    .FirstOrDefault(x => x.Key.Name.ToString() == varToOverwrite.ToString())
                                    .Value;

                                if (minVarValue.IsRatNum && minVarValue.ToString() != realMin.ToString()
                                    || minVarValue.IsIntNum && minVarValue.ToString() != integerMin.ToString())
                                {
                                    var newExpression = overwriteExpr.Predicate == BinaryPredicate.GreaterThan
                                        ? ContextProvider.Context.MkGt((ArithExpr)expressionToAdd.Args[0], (ArithExpr)minVarValue)
                                        : ContextProvider.Context.MkGe((ArithExpr)expressionToAdd.Args[0], (ArithExpr)minVarValue);
                                    updatedExpression.Add(newExpression);
                                }
                            }
                        }
                    }

                    foreach (var sourceExpression in concatenatedExpressionGroup)
                    {
                        var expressionToInspect = sourceExpression.IsNot
                            ? sourceExpression.Args[0] as BoolExpr
                            : sourceExpression;

                        if (updatedExpression.Contains(sourceExpression) // Нужна ли эта проверка?
                            && expressionToInspect.Args.Any(x => overwrittenVarNames.Contains(x.ToString())))
                        {
                            if (!expressionToInspect.Args.All(x => overwrittenVarNames.Contains(x.ToString()))
                                && !expressionToInspect.Args.Any(x => x.IsConst))
                            {
                                if (sourceExpression.IsEq) // Check - may not work!
                                {
                                    UpdateExpressionsBasedOnEquality(
                                        overwrittenVarNames,
                                        concatenatedExpressionGroup,
                                        updatedExpression,
                                        sourceExpression,
                                        expressionToInspect);
                                }
                                // Maybe we can add inequality when only one value is possible...
                                if (sourceExpression.IsLT || sourceExpression.IsLE)
                                {
                                    // Find max by optimization
                                    UpdateExpressionsBasedOnLessExpression(
                                        overwrittenVarNames,
                                        concatenatedExpressionGroup,
                                        updatedExpression,
                                        sourceExpression);
                                }
                                if (sourceExpression.IsGT || sourceExpression.IsGE)
                                {
                                    // Find min by optimization
                                    UpdateExpressionsBasedOnGreaterExpression(
                                        overwrittenVarNames,
                                        concatenatedExpressionGroup,
                                        updatedExpression,
                                        sourceExpression);
                                }
                            }
                            updatedExpression.Remove(sourceExpression);
                        }
                    }

                    andBlockExpressions.Add(GenerateAndBlockExpression(expressionsWithOverwrite.Except(bothOverwrittenExpressions), updatedExpression));
                }
            } while (targetConstraintsDuringEvaluation.Count > 0);

            return andBlockExpressions.Count() == 1
                ? andBlockExpressions[0]
                : ContextProvider.Context.MkOr(andBlockExpressions);
        }

        private static BoolExpr GenerateAndBlockExpression(IEnumerable<IConstraintExpression> expressionsWithOverwrite, IEnumerable<BoolExpr> updatedExpression)
        {
            //var accumulatedExpr = ContextProvider.Context.MkAnd(updatedExpression);
            var targetExprList = new List<BoolExpr>();
            foreach (var targetExpr in expressionsWithOverwrite)
            {
                // Write vars must become read ones
                var clonedTargetExpr = targetExpr.Clone();
                clonedTargetExpr.ConstraintVariable = new ConstraintVariable
                {
                    Domain = targetExpr.ConstraintVariable.Domain,
                    Name = targetExpr.ConstraintVariable.Name,
                    VariableType = VariableType.Read
                };

                targetExprList.Add(clonedTargetExpr.GetSmtExpression(ContextProvider.Context));
            }
            targetExprList.AddRange(updatedExpression);
            return ContextProvider.Context.MkAnd(targetExprList);
        }

        private static void UpdateExpressionsBasedOnGreaterExpression(
            IEnumerable<string> overwrittenVarNames, 
            IEnumerable<BoolExpr> concatenatedExpressionGroup, 
            List<BoolExpr> updatedExpression, 
            BoolExpr sourceExpression)
        {
            var varToOverwrite = sourceExpression.Args.FirstOrDefault(x => !overwrittenVarNames.Contains(x.ToString()));
            var optimizer = SetOptimizer(concatenatedExpressionGroup, varToOverwrite);

            optimizer.MkMinimize(varToOverwrite);

            if (optimizer.Check() == Status.SATISFIABLE)
            {
                var minVarValue = optimizer.Model.Consts
                    .FirstOrDefault(x => x.Key.Name.ToString() == varToOverwrite.ToString())
                    .Value;

                if (minVarValue.IsRatNum && minVarValue.ToString() != realMin.ToString() 
                    || minVarValue.IsIntNum && minVarValue.ToString() != integerMin.ToString())
                {
                    var secondVar = sourceExpression.Args.FirstOrDefault(x => !overwrittenVarNames.Contains(x.ToString()));
                    var newExpression = sourceExpression.IsGT
                        ? ContextProvider.Context.MkGt((ArithExpr)secondVar, (ArithExpr)minVarValue)
                        : ContextProvider.Context.MkGe((ArithExpr)secondVar, (ArithExpr)minVarValue);
                    updatedExpression.Add(newExpression);
                }
            }
        }

        private static Optimize SetOptimizer(IEnumerable<BoolExpr> concatenatedExpressionGroup, Expr? varToOverwrite)
        {
            var optimizer = ContextProvider.Context.MkOptimize();
            foreach (var expression in concatenatedExpressionGroup)
            {
                optimizer.Assert(expression);
            }
            ArithExpr minimalPossibleValue = varToOverwrite.IsRatNum
                ? ContextProvider.Context.MkReal(realMin.ToString(CultureInfo.InvariantCulture))
                : ContextProvider.Context.MkInt(integerMin.ToString());

            ArithExpr maximalPossibleValue = varToOverwrite.IsRatNum
                ? ContextProvider.Context.MkReal(realMax.ToString(CultureInfo.InvariantCulture))
                : ContextProvider.Context.MkInt(integerMax.ToString());

            optimizer.Assert(ContextProvider.Context.MkGe((ArithExpr)varToOverwrite, minimalPossibleValue));
            optimizer.Assert(ContextProvider.Context.MkLe((ArithExpr)varToOverwrite, maximalPossibleValue));

            return optimizer;
        }

        private static void UpdateExpressionsBasedOnLessExpression(IEnumerable<string> overwrittenVarNames, IEnumerable<BoolExpr> concatenatedExpressionGroup, List<BoolExpr> updatedExpression, BoolExpr sourceExpression)
        {
            var varToOverwrite = sourceExpression.Args.FirstOrDefault(x => !overwrittenVarNames.Contains(x.ToString()));
            var optimizer = SetOptimizer(concatenatedExpressionGroup, varToOverwrite);            
            
            optimizer.MkMaximize(varToOverwrite);
            if (optimizer.Check() == Status.SATISFIABLE)
            {
                var maxVarValue = optimizer.Model.Consts
                    .FirstOrDefault(x => x.Key.Name.ToString() == varToOverwrite.ToString())
                    .Value;

                if (maxVarValue.IsRatNum && maxVarValue.ToString() != realMax.ToString()
                    || maxVarValue.IsIntNum && maxVarValue.ToString() != integerMax.ToString())
                {
                    var secondVar = sourceExpression.Args.FirstOrDefault(x => !overwrittenVarNames.Contains(x.ToString()));
                    var newExpression = sourceExpression.IsLT
                        ? ContextProvider.Context.MkLt((ArithExpr)secondVar, (ArithExpr)maxVarValue)
                        : ContextProvider.Context.MkLe((ArithExpr)secondVar, (ArithExpr)maxVarValue);
                    updatedExpression.Add(newExpression);
                }
            }
        }

        private static void UpdateExpressionsBasedOnEquality(
            IEnumerable<string> overwrittenVarNames,
            IEnumerable<BoolExpr> concatenatedExpressionGroup,
            List<BoolExpr> updatedExpression,
            BoolExpr sourceExpression,
            BoolExpr? expressionToInspect)
        {
            var varToOverwrite = expressionToInspect.Args.FirstOrDefault(x => !overwrittenVarNames.Contains(x.ToString()));
            var secondVar = expressionToInspect.Args.FirstOrDefault(x => overwrittenVarNames.Contains(x.ToString()));

            var allBoolExpressionsWithOverwrittenVar = concatenatedExpressionGroup // Clarify, does it work
                .Where(x => (x.Args.Contains(varToOverwrite) && !expressionToInspect.Args.All(x => overwrittenVarNames.Contains(x.ToString())))
                    || (x.IsNot && x.Args[0].Args.Contains(varToOverwrite) && !expressionToInspect.Args[0].Args.All(x => overwrittenVarNames.Contains(x.ToString()))))
                .Except(new[] { sourceExpression })
                .ToList();

            foreach (var expression in allBoolExpressionsWithOverwrittenVar)
            {
                // Make a copy of expr - obliged to create new expressions
                var expressionToReplace = expression.IsNot
                    ? expression.Args[0] as BoolExpr
                    : expression;

                var operandToSave = expressionToReplace.Args[0] == varToOverwrite
                    ? 0
                    : 1;

                BoolExpr newExpression = null;
                if (expressionToReplace.IsEq)
                {
                    newExpression = ContextProvider.Context.MkEq(secondVar, expressionToReplace.Args[operandToSave]);
                }
                if (expressionToReplace.IsGT)
                {
                    newExpression = operandToSave == 0
                        ? ContextProvider.Context.MkGt((ArithExpr)expressionToReplace.Args[operandToSave], (ArithExpr)secondVar)
                        : ContextProvider.Context.MkGt((ArithExpr)secondVar, (ArithExpr)expressionToReplace.Args[operandToSave]);
                }
                if (expressionToReplace.IsGE)
                {
                    newExpression = operandToSave == 0
                       ? ContextProvider.Context.MkGe((ArithExpr)expressionToReplace.Args[operandToSave], (ArithExpr)secondVar)
                       : ContextProvider.Context.MkGe((ArithExpr)secondVar, (ArithExpr)expressionToReplace.Args[operandToSave]);
                }
                if (expressionToReplace.IsLE)
                {
                    newExpression = operandToSave == 0
                       ? ContextProvider.Context.MkLe((ArithExpr)expressionToReplace.Args[operandToSave], (ArithExpr)secondVar)
                       : ContextProvider.Context.MkLe((ArithExpr)secondVar, (ArithExpr)expressionToReplace.Args[operandToSave]);
                }
                if (expressionToReplace.IsLT)
                {
                    newExpression = operandToSave == 0
                        ? ContextProvider.Context.MkLt((ArithExpr)expressionToReplace.Args[operandToSave], (ArithExpr)secondVar)
                        : ContextProvider.Context.MkLt((ArithExpr)secondVar, (ArithExpr)expressionToReplace.Args[operandToSave]);
                }

                if (expression.IsNot)
                {
                    newExpression = ContextProvider.Context.MkNot(newExpression);
                }

                updatedExpression.Add(newExpression);

                if (updatedExpression.Contains(expression))
                {
                    updatedExpression.Remove(expression);
                }
            }
        }

        private static IEnumerable<BoolExpr[]> SplitSourceExpressionByOrDelimiter(BoolExpr source)
        {
            if (!source.IsOr)
            {
                var expressionBlocks = new List<BoolExpr[]>();
                var expressions = source.Args.Select(x => x as BoolExpr).ToArray();
                expressionBlocks.Add(expressions);

                return expressionBlocks;
            }

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
