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
    public abstract class LabeledTransitionSystem
    {
        protected ConstraintExpressionService expressionService;
        protected DataPetriNet DataPetriNet { get; init; }
        public ConstraintState InitialState { get; set; }
        public List<ConstraintState> ConstraintStates { get; set; }
        public List<ConstraintArc> ConstraintArcs { get; set; }
        public bool IsFullGraph { get; set; }
        public long Milliseconds { get; set; }

        protected Stack<ConstraintState> StatesToConsider { get; set; }

        public LabeledTransitionSystem(DataPetriNet dataPetriNet)
        {
            expressionService = new ConstraintExpressionService(dataPetriNet.Context);

            DataPetriNet = dataPetriNet;

            InitialState = dataPetriNet.GenerateInitialConstraintState();

            ConstraintStates = new List<ConstraintState> { InitialState };

            ConstraintArcs = new List<ConstraintArc>();

            IsFullGraph = false;
            Milliseconds = 0;

            StatesToConsider = new Stack<ConstraintState>();
            StatesToConsider.Push(InitialState);
        }

        public abstract void GenerateGraph();

        protected void AddNewState(ConstraintState currentState,
                                ConstraintTransition transition,
                                Dictionary<Node, int> marking,
                                BoolExpr constraintsIfFires)
        // TODO: Consider using less parameters
        {
            var equalStateInGraph = FindEqualStateInGraph(marking, constraintsIfFires);
            if (equalStateInGraph != null)
            {
                ConstraintArcs.Add(new ConstraintArc(currentState, transition, equalStateInGraph));
                equalStateInGraph.ParentStates = equalStateInGraph.ParentStates.Union(currentState.ParentStates).ToHashSet();
                equalStateInGraph.ParentStates.Add(currentState);

                if (equalStateInGraph.ParentStates.Contains(equalStateInGraph))
                {
                    equalStateInGraph.IsCyclic = true;
                }
            }
            else
            {
                var stateIfTransitionFires = new ConstraintState(marking, constraintsIfFires, currentState);
                ConstraintArcs.Add(new ConstraintArc(currentState, transition, stateIfTransitionFires));
                ConstraintStates.Add(stateIfTransitionFires);
                StatesToConsider.Push(stateIfTransitionFires);
            }
        }

        protected IEnumerable<Transition> GetEnabledTransitions(Dictionary<Node, int> marking)
        {
            var transitionsWhichCanFire = new List<Transition>();

            foreach (var transition in DataPetriNet.Transitions)
            {
                var preSetArcs = DataPetriNet.Arcs.Where(x => x.Destination == transition).ToList();

                var canFire = preSetArcs.All(x => marking[x.Source] >= x.Weight);

                if (canFire)
                {
                    transitionsWhichCanFire.Add(transition);
                }
            }

            return transitionsWhichCanFire;
        }

        protected bool IsMonotonicallyIncreasedWithSameConstraints(
            Dictionary<Node, int> tokens, 
            BoolExpr constraintsIfFires, 
            ConstraintState parentNode)
        {
            foreach (var stateInGraph in parentNode.ParentStates.Union(new[] { parentNode }))
            {
                var isConsideredStateTokensGreater = stateInGraph.PlaceTokens.Values.Sum() < tokens.Values.Sum() &&
                    tokens.Keys.All(key => tokens[key] >= stateInGraph.PlaceTokens[key]);

                if (isConsideredStateTokensGreater && expressionService.AreEqual(constraintsIfFires, stateInGraph.Constraints))
                {
                    return true;
                }
            }

            return false;
        }

        private ConstraintState? FindEqualStateInGraph(Dictionary<Node, int> tokens, BoolExpr constraintsIfFires)
        {
            foreach (var stateInGraph in ConstraintStates)
            {
                var isConsideredStateTokensEqual = tokens.Keys
                    .All(key => tokens[key] == stateInGraph.PlaceTokens[key]);

                if (isConsideredStateTokensEqual && expressionService.AreEqual(constraintsIfFires, stateInGraph.Constraints))
                {
                    return stateInGraph;
                }
            }

            return null;
        }
    }
}
