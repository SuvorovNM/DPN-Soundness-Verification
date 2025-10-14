using DPN.Models;
using DPN.Models.DPNElements;
using DPN.Models.Enums;
using DPN.Models.Extensions;
using DPN.Soundness.TransitionSystems.StateSpaceAbstraction;
using Microsoft.Z3;

namespace DPN.Soundness.TransitionSystems.Coverability
{
    public class CoverabilityTree : AbstractStateSpaceStructure<CtState, CtTransition, CtArc>
    // Покрасить все вершины, а затем обходить дерево в глубину до листьев или красных вершин
    {
        protected Stack<CtState> StatesToConsider { get; set; }
        public List<CtState> LeafStates { get; protected set; }
        protected bool WithTauTransitions { get; init; }
        public bool IsFullGraph { get; private set; }
        private bool StopOnCoveringFinalPosition { get; init; }
        private Place FinalPosition { get; init; }

        public CoverabilityTree(DataPetriNet dataPetriNet, bool stopOnCoveringFinalPosition, bool withTauTransitions = false) : base(dataPetriNet)
        {
            LeafStates = new List<CtState>();
            StatesToConsider = new Stack<CtState>();
            StatesToConsider.Push(InitialState);
            WithTauTransitions = withTauTransitions;
            StopOnCoveringFinalPosition =  stopOnCoveringFinalPosition;
            FinalPosition = dataPetriNet.Places.Single(p => p.IsFinal);
        }

        public override void GenerateGraph()
        {
            var transitionGuards = new Dictionary<Transition, BoolExpr>();
            var tauTransitionsGuards = new Dictionary<Transition, BoolExpr>();
            foreach (var transition in DataPetriNet.Transitions)
            {
                var smtExpression = transition.Guard.ActualConstraintExpression;
                var overwrittenVarNames = transition.Guard.WriteVars;
                var readExpression = DataPetriNet.Context.GetReadExpression(smtExpression, overwrittenVarNames);
                transitionGuards.Add(transition, readExpression);

                var negatedGuardExpressions = DataPetriNet.Context.MkNot(readExpression);
                tauTransitionsGuards.Add(transition, negatedGuardExpressions);
            }

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
                    var readExpression = transitionGuards[transition];

                    if (ExpressionService.CanBeSatisfied(DataPetriNet.Context.MkAnd(currentState.Constraints, readExpression)))
                    {
                        var constraintsIfTransitionFires = ExpressionService
                            .ConcatExpressions(currentState.Constraints, smtExpression, overwrittenVarNames);

                        if (!constraintsIfTransitionFires.IsFalse)
                        {
                            var updatedMarking = transition.FireOnGivenMarking(currentState.Marking, DataPetriNet.Arcs);
                            var stateToAddInfo = new BaseStateInfo(updatedMarking, (BoolExpr)constraintsIfTransitionFires.Simplify());

                            AddNewState(currentState, new CtTransition(transition), stateToAddInfo);
                            
                            if (StopOnCoveringFinalPosition && stateToAddInfo.Marking[FinalPosition] > 1)
                            {
	                            IsFullGraph = false;
	                            return;
                            }
                        }
                        
                    }

                    if (WithTauTransitions)
                    {
                        var negatedGuardExpressions = tauTransitionsGuards[transition];

                        var constraintsIfSilentTransitionFires = DataPetriNet.Context.MkAnd(currentState.Constraints, negatedGuardExpressions);
                        
                        if (ExpressionService.CanBeSatisfied(constraintsIfSilentTransitionFires) &&
                            !ExpressionService.AreEqual(currentState.Constraints, constraintsIfSilentTransitionFires))
                        {
                            var stateToAddInfo = new BaseStateInfo(currentState.Marking, (BoolExpr)constraintsIfSilentTransitionFires.Simplify());

                            AddNewState(currentState, new CtTransition(transition, true), stateToAddInfo);
                        }
                    }
                }
            }

            AddColorsToNodes();
        }

        private void AddColorsToNodes()
        {
            var dpnFinalMarking = DataPetriNet.FinalMarking;

            // Color simple Paths that lead to M_F as green
            ColorSimplePathsThatLeadToFinalMarkingAsGreen();
            ColorCyclicPathsThatLeadToFinalMarkingAsGreen();
            ColorRemainedNodesAsRed();

            void ColorSimplePathsThatLeadToFinalMarkingAsGreen()
            {
                foreach (var leaf in ConstraintStates)//LeafStates
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
                var changeOnPreviousStep = true;
                while (changeOnPreviousStep)
                {
                    changeOnPreviousStep = false;
                    foreach (var cycleLeaf in LeafStates.Where(x => x.StateType == CtStateType.NonstrictlyCovered && x.StateColor != CtStateColor.Green))
                    {
                        var isCoveredNodeGreen = cycleLeaf.CoveredNode.StateColor == CtStateColor.Green;

                        if (isCoveredNodeGreen)
                        {
                            changeOnPreviousStep = true;
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

                foreach (var place in DataPetriNet.Places)
                {
                    if (coveredNode.Marking[place] < stateInfo.Marking[place])
                    {
                        stateInfo.Marking[place] = int.MaxValue;
                    }
                }
                
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

                if (isConditionHoldsForTokens && ExpressionService.AreEqual(stateInfo.Constraints, currentNode.Constraints))
                {
                    return currentNode;
                }

                currentNode = currentNode.ParentNode;
            } while (currentNode?.ParentNode != null);

            return null;
        }
    }
}
