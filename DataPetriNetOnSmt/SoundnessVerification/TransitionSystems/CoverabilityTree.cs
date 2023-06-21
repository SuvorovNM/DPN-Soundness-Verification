using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.SoundnessVerification.TransitionSystems
{
    public class CoverabilityTree : AbstractStateSpaceStructure<CtState, CtTransition, CtArc>
    // Покрасить все вершины, а затем обходить дерево в глубину до листьев или красных вершин
    {
        protected Stack<CtState> StatesToConsider { get; set; }
        protected List<CtState> LeafStates { get; set; }

        public CoverabilityTree(DataPetriNet dataPetriNet) : base(dataPetriNet)
        {
            LeafStates = new List<CtState>();
            StatesToConsider = new Stack<CtState>();
            StatesToConsider.Push(InitialState);
        }

        public override void GenerateGraph()
        {
            while (StatesToConsider.Count > 0)
            {
                var currentState = StatesToConsider.Pop();

                var enabledTransitions = currentState.Marking.GetEnabledTransitions(DataPetriNet);

                if (enabledTransitions.Count == 0)
                {
                    LeafStates.Add(currentState);
                }

                foreach (var transition in enabledTransitions)
                {
                    var smtExpression = transition.Guard.ActualConstraintExpression;

                    var overwrittenVarNames = transition.Guard.WriteVars;
                    var readExpression = DataPetriNet.Context.GetReadExpression(smtExpression, overwrittenVarNames);

                    if (expressionService.CanBeSatisfied(expressionService.ConcatExpressions(currentState.Constraints, readExpression, overwrittenVarNames)))
                    {
                        var constraintsIfTransitionFires = expressionService
                            .ConcatExpressions(currentState.Constraints, smtExpression, overwrittenVarNames);

                        if (expressionService.CanBeSatisfied(constraintsIfTransitionFires))
                        {
                            var updatedMarking = transition.FireOnGivenMarking(currentState.Marking, DataPetriNet.Arcs);
                            var stateToAddInfo = new BaseStateInfo(updatedMarking, constraintsIfTransitionFires);

                            AddNewState(currentState, new CtTransition(transition), stateToAddInfo);
                        }
                    }

                    var negatedGuardExpressions = DataPetriNet.Context.MkNot(readExpression);

                    if (!negatedGuardExpressions.IsTrue && !negatedGuardExpressions.IsFalse)
                    {
                        var constraintsIfSilentTransitionFires = expressionService
                            .ConcatExpressions(currentState.Constraints, negatedGuardExpressions, new Dictionary<string, DomainType>());

                        if (expressionService.CanBeSatisfied(constraintsIfSilentTransitionFires) &&
                            !expressionService.AreEqual(currentState.Constraints, constraintsIfSilentTransitionFires))
                        {
                            var stateToAddInfo = new BaseStateInfo(currentState.Marking, constraintsIfSilentTransitionFires);

                            AddNewState(currentState, new CtTransition(transition, true), stateToAddInfo);
                        }
                    }
                }
            }

            // Вынести в отдельный метод

            AddColorsToNodes();
        }

        private void AddColorsToNodes()
        {
            var dpnFinalMarking = DataPetriNet.FinalMarking;

            // Color simplepPaths that lead to M_F as green
            ColorSimplePathsThatLeadToFinalMarkingAsGreen();
            ColorCyclicPathsThatLeadToFinalMarkingAsGreen();
            ColorRemainedNodesAsRed();

            void ColorSimplePathsThatLeadToFinalMarkingAsGreen()
            {
                foreach (var leaf in LeafStates)
                {
                    var isFinal = leaf.Marking.CompareTo(dpnFinalMarking) == MarkingComparisonResult.Equal;
                    if (isFinal)
                    {
                        leaf.StateColor = CtStateColor.Green;
                        var parent = leaf.ParentNode;
                        while (parent != null)
                        {
                            parent.StateColor = CtStateColor.Green;
                            parent = parent.ParentNode;
                        }
                    }
                }
            }

            void ColorCyclicPathsThatLeadToFinalMarkingAsGreen()
            {
                foreach (var cycleLeaf in LeafStates.Where(x => x.StateType == CtStateType.NonstrictlyCovered))
                {
                    var isCoveredNodeGreen = cycleLeaf.CoveredNode.StateColor == CtStateColor.Green;

                    if (isCoveredNodeGreen)
                    {
                        cycleLeaf.StateColor = CtStateColor.Green;
                        var parent = cycleLeaf.ParentNode;
                        while (parent != null && parent.StateColor != CtStateColor.Green)
                        {
                            parent.StateColor = CtStateColor.Green;
                            parent = parent.ParentNode;
                        }
                    }
                }
            }

            void ColorRemainedNodesAsRed()
            {
                foreach (var state in ConstraintStates)
                {
                    if (state.StateColor != CtStateColor.Green)
                    {
                        state.StateColor = CtStateColor.Red;
                    }
                }
            }
        }

        protected override void AddNewState(CtState currentState, CtTransition transition, BaseStateInfo stateInfo)
        {
            // Since it is a tree, there can be only one node that is greater than or equal
            var coveredNode = FindParentNodeForWhichComparisonResultForCurrentNodeHolds
                (stateInfo, currentState, MarkingComparisonResult.GreaterThan | MarkingComparisonResult.Equal);

            if (coveredNode != null)
            {
                var isStrictCoverage =
                    stateInfo.Marking.CompareTo(coveredNode?.Marking) == MarkingComparisonResult.GreaterThan;

                var stateColor = isStrictCoverage ? CtStateType.StrictlyCovered : CtStateType.NonstrictlyCovered; // Not forget about final nodes

                var stateToAdd = new CtState(stateInfo, currentState, stateColor, coveredNode);

                ConstraintStates.Add(stateToAdd);
                ConstraintArcs.Add(new CtArc(currentState, transition, stateToAdd));
                LeafStates.Add(stateToAdd);
            }
            else
            {
                var stateToAdd = new CtState(stateInfo, currentState);
                ConstraintStates.Add(stateToAdd);
                ConstraintArcs.Add(new CtArc(currentState, transition, stateToAdd));
                StatesToConsider.Push(stateToAdd);
            }
        }

        protected override CtState? FindParentNodeForWhichComparisonResultForCurrentNodeHolds
            (BaseStateInfo stateInfo, CtState parentNode, MarkingComparisonResult comparisonResult)
        {
            var currentNode = parentNode;

            do
            {
                var isConditionHoldsForTokens =
                     comparisonResult.HasFlag(stateInfo.Marking.CompareTo(currentNode.Marking));

                if (isConditionHoldsForTokens && expressionService.AreEqual(stateInfo.Constraints, currentNode.Constraints))
                {
                    return currentNode;
                }

                currentNode = currentNode.ParentNode;
            } while (currentNode?.ParentNode != null);

            return null;
        }
    }
}
