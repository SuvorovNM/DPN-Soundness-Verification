using DataPetriNet.Abstractions;
using DataPetriNet.DPNElements;
using DataPetriNet.Enums;
using DataPetriNet.Services.ExpressionServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.SoundnessVerification
{
    public class ConstraintExpressionOperationService
    {
        private readonly ExpressionsServiceStore expressionServices;
        public ConstraintExpressionOperationService()
        {
            expressionServices = new ExpressionsServiceStore();
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

        public List<IConstraintExpression> ShortenExpression(List<IConstraintExpression> expression)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var resultExpression = new List<IConstraintExpression>();

            var constraintStateDuringEvaluation = new List<IConstraintExpression>(expression);
            do
            {
                expressionServices.Clear();

                var currentBlock = CutFirstExpressionBlock(constraintStateDuringEvaluation);

                foreach (var expressionItem in currentBlock)
                {
                    expressionServices[expressionItem.ConstraintVariable.Domain].AddValueInterval(expressionItem);
                }

                var blockShortenedExpression = new List<IConstraintExpression>();
                foreach(var expressionVariable in currentBlock.Select(x => x.ConstraintVariable).Distinct())
                {
                    // If the expression block is not satisfiable
                    if (!expressionServices[expressionVariable.Domain].GenerateExpressionsBasedOnIntervals(expressionVariable.Name, out var variableExpressions))
                    {
                        blockShortenedExpression.Clear();
                        break;
                    }

                    if (variableExpressions.Count > 0)
                    {
                        variableExpressions[0].LogicalConnective = LogicalConnective.And;
                        blockShortenedExpression.AddRange(variableExpressions);
                    }
                }

                if (blockShortenedExpression.Count > 0)
                {
                    blockShortenedExpression[0].LogicalConnective = LogicalConnective.Or;
                    resultExpression.AddRange(blockShortenedExpression);
                }

            } while (constraintStateDuringEvaluation.Count > 0);

            if (resultExpression.Count > 0)
            {
                resultExpression[0].LogicalConnective = LogicalConnective.Empty;
            }

            return resultExpression;
        }

        public bool CanBeSatisfied(List<IConstraintExpression> expression)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }
            if (expression.Count == 0)
            {
                return false;
            }

            var constraintStateDuringEvaluation = new List<IConstraintExpression>(expression);
            bool expressionResult;

            do
            {
                expressionServices.Clear();
                expressionResult = true;
                // Block of ANDs which is currently evaluated
                var currentBlock = CutFirstExpressionBlock(constraintStateDuringEvaluation);

                foreach (var expressionItem in currentBlock)
                {
                    expressionServices[expressionItem.ConstraintVariable.Domain].AddValueInterval(expressionItem);
                }

                // Select values for written variables
                if (expressionResult)
                {
                    foreach (var variable in currentBlock
                        .Select(x => x.ConstraintVariable)
                        .Distinct())
                    {
                        expressionResult &= expressionServices[variable.Domain].TryInferValue(variable.Name, out _);
                    }
                }

            } while (constraintStateDuringEvaluation.Count > 0 && !expressionResult);

            return expressionResult;
        }

        public bool AreEqual(List<IConstraintExpression> expressionSource, List<IConstraintExpression> expressionTarget)
        {
            if (expressionSource is null)
            {
                throw new ArgumentNullException(nameof(expressionSource));
            }
            if (expressionTarget is null)
            {
                throw new ArgumentNullException(nameof(expressionTarget));
            }

            if (expressionSource.Count != expressionTarget.Count)
            {
                return false;
            }

            var blockedSourceConstraints = new List<List<IConstraintExpression>>();
            var blockedTargetConstraints = new List<List<IConstraintExpression>>();

            var sourceConstraintsForEvaluation = new List<IConstraintExpression>(expressionSource);
            var targetConstraintsForEvaluation = new List<IConstraintExpression>(expressionTarget);

            do
            {
                // Assume that order of constants is equal - generally, it is set up in expressionServices
                blockedSourceConstraints.Add(CutFirstExpressionBlock(sourceConstraintsForEvaluation)
                                                    .OrderBy(x => x.ConstraintVariable.VariableType)
                                                        .ThenBy(x => x.ConstraintVariable.Name)
                                                            .ThenBy(x => x.Predicate)
                                                    .ToList());
                blockedTargetConstraints.Add(CutFirstExpressionBlock(targetConstraintsForEvaluation)
                                                     .OrderBy(x => x.ConstraintVariable.VariableType)
                                                        .ThenBy(x => x.ConstraintVariable.Name)
                                                            .ThenBy(x => x.Predicate)
                                                     .ToList());

            } while (sourceConstraintsForEvaluation.Count > 0 && targetConstraintsForEvaluation.Count > 0);

            if (sourceConstraintsForEvaluation.Count > 0 || targetConstraintsForEvaluation.Count > 0)
            {
                return false;
            }

            do
            {
                var currentSourceBlock = blockedSourceConstraints[0];
                blockedSourceConstraints.RemoveAt(0);

                bool isFound = true;
                var index = 0;
                do
                {
                    if (blockedTargetConstraints[index].Count == currentSourceBlock.Count)
                    {
                        for (int i = 0; i < currentSourceBlock.Count; i++)
                        {
                            isFound &= currentSourceBlock[i].Equals(blockedTargetConstraints[index][i]);
                        }

                        index++;
                    }
                    else
                    {
                        isFound = false;
                    }

                } while (isFound!= true && index < blockedTargetConstraints.Count);

                if (!isFound)
                {
                    return false;
                }

            } while (blockedSourceConstraints.Count > 0);

            return true;
        }

        public List<IConstraintExpression> ConcatExpressions(List<IConstraintExpression> source, List<IConstraintExpression> target)
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

            var allTargetToRead = new List<IConstraintExpression>(target.Select(x => x.Clone()));
            allTargetToRead.ForEach(x => x.ConstraintVariable = new ConstraintVariable
            {
                Domain = x.ConstraintVariable.Domain,
                Name = x.ConstraintVariable.Name,
                VariableType = VariableType.Read,
            });
            if (source.Count == 0)
            {
                return allTargetToRead;
            }

            var result = new List<IConstraintExpression>();

            var writtenVariables = target
                .Where(x => x.ConstraintVariable.VariableType == VariableType.Written)
                .Select(x => (x.ConstraintVariable.Domain, x.ConstraintVariable.Name));

            var sourceExpressionsExceptRewrittenInTarget = source
                .Where(x => !writtenVariables.Contains((x.ConstraintVariable.Domain, x.ConstraintVariable.Name)))
                .ToList();

            var sourceConstraintsDuringEvaluation = new List<IConstraintExpression>(sourceExpressionsExceptRewrittenInTarget);

            do
            {
                var currentSourceBlock = CutFirstExpressionBlock(sourceConstraintsDuringEvaluation);

                var targetConstraintsDuringEvaluation = new List<IConstraintExpression>(allTargetToRead);
                targetConstraintsDuringEvaluation[0].LogicalConnective = LogicalConnective.And;

                do
                {
                    var currentTargetBlock = CutFirstExpressionBlock(targetConstraintsDuringEvaluation).Select(x => x.Clone()).ToList();

                    if (currentTargetBlock.Count > 0)
                    {
                        var sourceBlockToInsert = currentSourceBlock.Select(x => x.Clone()).ToList();
                        sourceBlockToInsert[0].LogicalConnective = LogicalConnective.Or;

                        currentTargetBlock[0].LogicalConnective = LogicalConnective.And;                        
                        result.AddRange(sourceBlockToInsert.Concat(currentTargetBlock));
                    }

                } while (targetConstraintsDuringEvaluation.Count > 0);

            } while (sourceConstraintsDuringEvaluation.Count > 0);

            if (result.Count > 0)
            {
                result[0].LogicalConnective = LogicalConnective.Empty;
            }
            return ShortenExpression(result);
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
