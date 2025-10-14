using DPN.Models;
using DPN.Models.DPNElements;
using DPN.Models.Enums;
using DPN.Soundness.TransitionSystems.StateSpaceAbstraction;
using Microsoft.Z3;

namespace DPN.Soundness.TransitionSystems.Reachability
{
    internal abstract class LabeledTransitionSystem : AbstractStateSpaceStructure<LtsState, LtsTransition, LtsArc>
    {
        public bool IsFullGraph { get; protected set; }

        protected Stack<LtsState> StatesToConsider { get; set; }

        protected LabeledTransitionSystem(DataPetriNet dataPetriNet) : base(dataPetriNet)
        {
            IsFullGraph = false;

            StatesToConsider = new Stack<LtsState>();
            StatesToConsider.Push(InitialState);
        }

        public abstract void GenerateGraph();

        protected void AddNewState(LtsState currentState,
            LtsTransition transition,
            BaseStateInfo stateInfo)
        {
            var equalStateInGraph = FindEqualStateInGraph(stateInfo.Marking, stateInfo.Constraints);
            if (equalStateInGraph != null)
            {
                ConstraintArcs.Add(new LtsArc(currentState, transition, equalStateInGraph));
                equalStateInGraph.ParentStates =
                    equalStateInGraph.ParentStates.Union(currentState.ParentStates).ToHashSet();
                equalStateInGraph.ParentStates.Add(currentState);

                if (equalStateInGraph.ParentStates.Contains(equalStateInGraph))
                {
                    equalStateInGraph.IsCyclic = true;
                }
            }
            else
            {
                var stateIfTransitionFires = new LtsState(stateInfo, currentState);
                ConstraintArcs.Add(new LtsArc(currentState, transition, stateIfTransitionFires));
                ConstraintStates.Add(stateIfTransitionFires);
                StatesToConsider.Push(stateIfTransitionFires);
            }
        }

        protected LtsState? FindParentNodeForWhichComparisonResultForCurrentNodeHolds
            (BaseStateInfo stateInfo, LtsState parentNode, MarkingComparisonResult comparisonResult)
        {
            foreach (var stateInGraph in parentNode.ParentStates.Union([parentNode]))
            {
                var isConditionHoldsForTokens =
                    stateInfo.Marking.CompareTo(stateInGraph.Marking) == comparisonResult;

                if (isConditionHoldsForTokens &&
                    ExpressionService.AreEqual(stateInfo.Constraints, stateInGraph.Constraints))
                {
                    return stateInGraph;
                }
            }

            return null;
        }


        private LtsState? FindEqualStateInGraph(Marking tokens, BoolExpr constraintsIfFires)
        {
            foreach (var stateInGraph in ConstraintStates)
            {
                var isConsideredStateTokensEqual =
                    tokens.CompareTo(stateInGraph.Marking) == MarkingComparisonResult.Equal;

                if (isConsideredStateTokensEqual &&
                    ExpressionService.AreEqual(constraintsIfFires, stateInGraph.Constraints))
                {
                    return stateInGraph;
                }
            }

            return null;
        }
    }
}