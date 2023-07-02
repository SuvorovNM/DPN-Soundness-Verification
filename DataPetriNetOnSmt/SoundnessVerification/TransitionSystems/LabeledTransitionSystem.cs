using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.Extensions;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.SoundnessVerification.TransitionSystems
{
    public abstract class LabeledTransitionSystem : AbstractStateSpaceStructure<LtsState, LtsTransition, LtsArc>
    {
        public bool IsFullGraph { get; set; }
        public long Milliseconds { get; set; }

        protected Stack<LtsState> StatesToConsider { get; set; }

        public LabeledTransitionSystem(DataPetriNet dataPetriNet) : base(dataPetriNet)
        {
            IsFullGraph = false;
            Milliseconds = 0;

            StatesToConsider = new Stack<LtsState>();
            StatesToConsider.Push(InitialState);
        }

        protected override void AddNewState(LtsState currentState,
                                LtsTransition transition,
                                BaseStateInfo stateInfo)
        {
            var equalStateInGraph = FindEqualStateInGraph(stateInfo.Marking, stateInfo.Constraints);
            if (equalStateInGraph != null)
            {
                ConstraintArcs.Add(new LtsArc(currentState, transition, equalStateInGraph));
                equalStateInGraph.ParentStates = equalStateInGraph.ParentStates.Union(currentState.ParentStates).ToHashSet();
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

        protected override LtsState? FindParentNodeForWhichComparisonResultForCurrentNodeHolds
            (BaseStateInfo stateInfo, LtsState parentNode, MarkingComparisonResult comparisonResult)
        {
            foreach (var stateInGraph in parentNode.ParentStates.Union(new[] { parentNode }))
            {
                var isConditionHoldsForTokens =
                    stateInfo.Marking.CompareTo(stateInGraph.Marking) == comparisonResult;

                if (isConditionHoldsForTokens && expressionService.AreEqual(stateInfo.Constraints, stateInGraph.Constraints))
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

                if (isConsideredStateTokensEqual && expressionService.AreEqual(constraintsIfFires, stateInGraph.Constraints))
                {
                    return stateInGraph;
                }
            }

            return null;
        }
    }
}
