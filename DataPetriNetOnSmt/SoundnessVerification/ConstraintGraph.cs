using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.Extensions;
using DataPetriNetOnSmt.SoundnessVerification.Services;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.SoundnessVerification
{
    public class ConstraintGraph // TODO: insert Ids
    {
        public AbstractConstraintExpressionService expressionService;
        public DataPetriNet DataPetriNet { get; set; }
        public ConstraintState InitialState { get; set; }
        public List<ConstraintState> ConstraintStates { get; set; }
        public List<ConstraintArc> ConstraintArcs { get; set; }

        public Stack<ConstraintState> StatesToConsider { get; set; }

        public ConstraintGraph(DataPetriNet dataPetriNet, AbstractConstraintExpressionService abstractConstraintExpressionService)
        {
            expressionService = abstractConstraintExpressionService;
            //expressionService = new ConstraintExpressionOperationServiceWithManualConcat();

            DataPetriNet = dataPetriNet;

            InitialState = dataPetriNet.GenerateInitialConstraintState();

            ConstraintStates = new List<ConstraintState> { InitialState };

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
                    var readOnlyExpressions = GetReadExpressions(transition.Guard.ConstraintExpressions);

                    if (expressionService.CanBeSatisfied(expressionService.ConcatExpressions(currentState.Constraints, readOnlyExpressions)))
                    {
                        var constraintsIfTransitionFires = expressionService
                            .ConcatExpressions(currentState.Constraints, transition.Guard.ConstraintExpressions, true);

                        if (expressionService.CanBeSatisfied(constraintsIfTransitionFires))
                        {
                            var updatedMarking = transition.FireOnGivenMarking(currentState.PlaceTokens, DataPetriNet.Arcs);

                            if (IsMonotonicallyIncreasedWithUnchangedConstraints(updatedMarking, constraintsIfTransitionFires))
                            {
                                return false; // The net is unbound
                            }

                            AddNewState(currentState, new ConstraintTransition(transition), updatedMarking, constraintsIfTransitionFires);
                        }
                    }

                    var negatedGuardExpressions = expressionService
                        .GetInvertedReadExpression(transition.Guard.ConstraintExpressions);

                    if (negatedGuardExpressions.Count > 0)
                    {
                        var constraintsIfSilentTransitionFires = expressionService
                            .ConcatExpressions(currentState.Constraints, negatedGuardExpressions, true);

                        if (expressionService.CanBeSatisfied(constraintsIfSilentTransitionFires) &&
                            !expressionService.AreEqual(currentState.Constraints, constraintsIfSilentTransitionFires))
                        {
                            AddNewState(currentState, new ConstraintTransition(transition, true), currentState.PlaceTokens, constraintsIfSilentTransitionFires);
                        }
                    }
                }
            }

            return true;
        }


        private List<IConstraintExpression> GetReadExpressions(List<IConstraintExpression> constraints) // TODO: Check for correctness
        {
            var expressionList = constraints
                .GetExpressionsOfType(VariableType.Read)
                .ToList();

            var readOnlyConstraintsIndex = 0;
            var lastCorrespondingConstraintIndex = -1;
            var lastOrExpressionIndex = -1;

            for (int constraintsIndex = 0; constraintsIndex < constraints.Count && readOnlyConstraintsIndex < expressionList.Count; constraintsIndex++)
            {
                if (constraints[constraintsIndex].LogicalConnective == LogicalConnective.Or)
                {
                    lastOrExpressionIndex = constraintsIndex;
                }

                if (constraints[constraintsIndex] == expressionList[readOnlyConstraintsIndex])
                {
                    if (lastOrExpressionIndex > lastCorrespondingConstraintIndex)
                    {
                        expressionList[readOnlyConstraintsIndex] = constraints[constraintsIndex].Clone();
                        expressionList[readOnlyConstraintsIndex].LogicalConnective = LogicalConnective.Or;
                    }
                    readOnlyConstraintsIndex++;
                    lastCorrespondingConstraintIndex = constraintsIndex;
                }
            }

            return expressionList;
        }


        private void AddNewState(ConstraintState currentState,
                                ConstraintTransition transition,
                                Dictionary<Node, int> marking,
                                BoolExpr constraintsIfFires)
        // TODO: Consider using less parameters
        {
            var equalStateInGraph = FindEqualStateInGraph(marking, constraintsIfFires);
            if (equalStateInGraph != null)
            {
                ConstraintArcs.Add(new ConstraintArc(currentState, transition, equalStateInGraph));
            }
            else
            {
                var stateIfTransitionFires = new ConstraintState(marking, constraintsIfFires);
                ConstraintArcs.Add(new ConstraintArc(currentState, transition, stateIfTransitionFires));
                ConstraintStates.Add(stateIfTransitionFires);
                StatesToConsider.Push(stateIfTransitionFires);
            }
        }

        private IEnumerable<Transition> GetTransitionsWhichCanFire(Dictionary<Node, int> marking)
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

        private bool IsMonotonicallyIncreasedWithUnchangedConstraints(Dictionary<Node, int> tokens, BoolExpr constraintsIfFires)
        {
            foreach (var stateInGraph in ConstraintStates)
            {
                var isConsideredStateTokensGreaterOrEqual = stateInGraph.PlaceTokens.Values.Sum() > tokens.Values.Sum() &&
                    tokens.Keys.All(key => tokens[key] >= stateInGraph.PlaceTokens[key]);

                if (isConsideredStateTokensGreaterOrEqual && expressionService.AreEqual(constraintsIfFires, stateInGraph.Constraints))
                {
                    return true;
                }
            }

            return false;
        }

        private ConstraintState FindEqualStateInGraph(Dictionary<Node, int> tokens, BoolExpr constraintsIfFires)
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
