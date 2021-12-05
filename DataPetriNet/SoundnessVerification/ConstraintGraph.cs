using DataPetriNet.Abstractions;
using DataPetriNet.DPNElements;
using DataPetriNet.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.SoundnessVerification
{
    public class ConstraintGraph
    {
        private ConstraintExpressionOperationService expressionService;
        public DataPetriNet DataPetriNet { get; set; }
        public ConstraintState InitialState { get; set; }
        public List<ConstraintState> ConstraintStates { get; set; }
        public List<ConstraintArc> ConstraintArcs { get; set; }
        
        public Stack<ConstraintState> StatesToConsider { get; set; }

        public ConstraintGraph(DataPetriNet dataPetriNet)
        {
            expressionService = new ConstraintExpressionOperationService();

            DataPetriNet = dataPetriNet;

            InitialState = dataPetriNet.GenerateInitialConstraintState();

            ConstraintStates = new List<ConstraintState> {InitialState };

            ConstraintArcs = new List<ConstraintArc>();

            StatesToConsider = new Stack<ConstraintState>();
            StatesToConsider.Push(InitialState);
        }

        public bool GenerateGraph()
        {
            while (StatesToConsider.Count > 0)
            {
                var currentState = StatesToConsider.Pop();

                foreach (var transition in GetTransitionsWhichCanFire(currentState.PlaceTokens))
                {
                    // Considering classical transition
                    var stateIfTransitionFires = new ConstraintState(currentState, transition);

                    if (expressionService.CanBeSatisfied(stateIfTransitionFires.Constraints))
                    {
                        if (IsMonotonicallyIncreasedWithUnchangedConstraints(stateIfTransitionFires))
                        {
                            return false; // The net is unbound
                        }

                        AddNewState(currentState, new ConstraintTransition(transition), stateIfTransitionFires);
                    }

                    if (transition.Label == "Simple assessment")
                    {

                    }

                    // Considering silent transition
                    var negatedGuardExpressions = expressionService
                        .InverseExpression(transition.Guard.ConstraintExpressions
                                                .Where(x => x.ConstraintVariable.VariableType != VariableType.Written)
                                                .ToList());
                    var constraintsIfSilentTransitionFires = expressionService
                        .ConcatExpressions(currentState.Constraints, negatedGuardExpressions);

                    if (expressionService.CanBeSatisfied(constraintsIfSilentTransitionFires) && 
                        !expressionService.AreEqual(currentState.Constraints, constraintsIfSilentTransitionFires))
                    {
                        var silentState = new ConstraintState(currentState.PlaceTokens, constraintsIfSilentTransitionFires);

                        AddNewState(currentState, new ConstraintTransition(transition, isSilent: true), silentState);
                    }
                }
            }

            return true;
        }

        private void AddNewState(ConstraintState currentState, ConstraintTransition transition, ConstraintState stateIfTransitionFires)
        {
            var equalStateInGraph = FindEqualStateInGraph(stateIfTransitionFires);
            if (equalStateInGraph != null)
            {
                ConstraintArcs.Add(new ConstraintArc(currentState, transition, equalStateInGraph));
            }
            else
            {
                ConstraintArcs.Add(new ConstraintArc(currentState, transition, stateIfTransitionFires));
                ConstraintStates.Add(stateIfTransitionFires);
                StatesToConsider.Push(stateIfTransitionFires);
            }
        }

        // TODO: Проверить, что для обычных Transition отработает - возможно нужны именно ConstraintTransition
        private IEnumerable<Transition> GetTransitionsWhichCanFire(Dictionary<Node, int> marking)
        {
            var transitionsWhichCanFire = new List<Transition>();

            foreach (var transition in DataPetriNet.Transitions)
            {
                var canFire = transition.PreSetPlaces.All(x => marking[x] > 0);
                if (canFire)
                {
                    transitionsWhichCanFire.Add(transition);
                }
            }

            return transitionsWhichCanFire;
        }

        private bool IsMonotonicallyIncreasedWithUnchangedConstraints(ConstraintState sourceState)
        {
            foreach (var stateInGraph in ConstraintStates)
            {
                var isConsideredStateTokensGreaterOrEqual = sourceState.PlaceTokens.Keys.All(key => sourceState.PlaceTokens[key] >= stateInGraph.PlaceTokens[key]);
                if (isConsideredStateTokensGreaterOrEqual && expressionService.AreEqual(sourceState.Constraints, stateInGraph.Constraints))
                {
                    return true;
                }
            }

            return false;
        }

        private ConstraintState FindEqualStateInGraph(ConstraintState sourceState)
        {
            foreach (var stateInGraph in ConstraintStates)
            {
                var isConsideredStateTokensEqual = sourceState.PlaceTokens.Keys.All(key => sourceState.PlaceTokens[key] == stateInGraph.PlaceTokens[key]);
                if (isConsideredStateTokensEqual && expressionService.AreEqual(sourceState.Constraints, stateInGraph.Constraints))
                {
                    return stateInGraph;
                }
            }

            return null;
        }
    }
}
