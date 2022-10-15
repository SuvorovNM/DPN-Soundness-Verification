using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.Extensions;
using Microsoft.Z3;

namespace DataPetriNetOnSmt.SoundnessVerification
{
    public class ConstraintGraph // TODO: insert Ids
    {
        public Context Context { get; init; }
        private AbstractConstraintExpressionService expressionService;
        private DataPetriNet DataPetriNet { get; init; }
        public ConstraintState InitialState { get; set; }
        public List<ConstraintState> ConstraintStates { get; set; }
        public List<ConstraintArc> ConstraintArcs { get; set; }
        public bool IsFullGraph { get; set; }

        private Stack<ConstraintState> StatesToConsider { get; set; }

        public ConstraintGraph(DataPetriNet dataPetriNet, AbstractConstraintExpressionService abstractConstraintExpressionService)
        {
            expressionService = abstractConstraintExpressionService;
            Context = dataPetriNet.Context;
            //expressionService = new ConstraintExpressionOperationServiceWithManualConcat();

            DataPetriNet = dataPetriNet;

            InitialState = dataPetriNet.GenerateInitialConstraintState();

            ConstraintStates = new List<ConstraintState> { InitialState };

            ConstraintArcs = new List<ConstraintArc>();

            IsFullGraph = false;

            StatesToConsider = new Stack<ConstraintState>();
            StatesToConsider.Push(InitialState);
        }

        public ConstraintGraph()
        {
            ConstraintArcs = new List<ConstraintArc>();
            ConstraintStates = new List<ConstraintState>();
            StatesToConsider = new Stack<ConstraintState>();
        }

        public void GenerateGraph(bool removeRedundantBlocks = false)
        {
            IsFullGraph = false;

            while (StatesToConsider.Count > 0)
            {
                var currentState = StatesToConsider.Pop();

                foreach (var transition in GetTransitionsWhichCanFire(currentState.PlaceTokens))
                {
                    // Considering classical transition
                    var readOnlyExpressions = GetReadExpressions(transition.Guard.ConstraintExpressions);

                    if (readOnlyExpressions.Count == 0 || 
                            expressionService.CanBeSatisfied(expressionService.ConcatExpressions(currentState.Constraints, readOnlyExpressions, removeRedundantBlocks)))
                    {
                        var constraintsIfTransitionFires = expressionService
                            .ConcatExpressions(currentState.Constraints, transition.Guard.ConstraintExpressions, removeRedundantBlocks);

                        if (expressionService.CanBeSatisfied(constraintsIfTransitionFires))
                        {
                            var updatedMarking = transition.FireOnGivenMarking(currentState.PlaceTokens, DataPetriNet.Arcs);

                            if (IsMonotonicallyIncreasedWithUnchangedConstraints(updatedMarking, constraintsIfTransitionFires, currentState))
                            {
                                return; // The net is unbound
                            }

                            AddNewState(currentState, new ConstraintTransition(transition), updatedMarking, constraintsIfTransitionFires);
                        }
                    }

                    var negatedGuardExpressions = expressionService
                        .GetInvertedReadExpression(transition.Guard.ConstraintExpressions);

                    if (negatedGuardExpressions.Count > 0)
                    {
                        var constraintsIfSilentTransitionFires = expressionService
                            .ConcatExpressions(currentState.Constraints, negatedGuardExpressions, removeRedundantBlocks);

                        if (expressionService.CanBeSatisfied(constraintsIfSilentTransitionFires) &&
                            !expressionService.AreEqual(currentState.Constraints, constraintsIfSilentTransitionFires))
                        {
                            if (IsMonotonicallyIncreasedWithUnchangedConstraints(currentState.PlaceTokens, constraintsIfSilentTransitionFires, currentState))
                            {
                                return; // The net is unbound
                            }

                            AddNewState(currentState, new ConstraintTransition(transition, true), currentState.PlaceTokens, constraintsIfSilentTransitionFires);
                        }
                    }
                }
            }

            IsFullGraph = true;
        }


        private List<IConstraintExpression> GetReadExpressions(List<IConstraintExpression> constraints) // TODO: Check for correctness
        {
            // Костыль, необходима проверка на то, что минимум в каждом OR-блоке есть readExpression
            // Иначе ReadExpressions тождественно равен True
            var lastOrIdx = -1;
            var lastReadIdx = -2;

            for (int i = 0; i < constraints.Count; i++)
            {
                if (constraints[i].LogicalConnective == LogicalConnective.Or)
                {
                    if (lastReadIdx < lastOrIdx)
                    {
                        return new List<IConstraintExpression>();
                    }
                    lastOrIdx = i;
                }
                if (constraints[i].ConstraintVariable.VariableType == VariableType.Read)
                {
                    lastReadIdx = i;
                }                
            }

            if (lastReadIdx == -2 || lastReadIdx < lastOrIdx)
            {
                return new List<IConstraintExpression>();
            }

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
                equalStateInGraph.ParentStates = equalStateInGraph.ParentStates.Union(currentState.ParentStates).ToHashSet();
                equalStateInGraph.ParentStates.Add(currentState);
            }
            else
            {
                var stateIfTransitionFires = new ConstraintState(marking, constraintsIfFires, currentState);
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

        private bool IsMonotonicallyIncreasedWithUnchangedConstraints(Dictionary<Node, int> tokens, BoolExpr constraintsIfFires, ConstraintState parentNode)
        {
            foreach (var stateInGraph in parentNode.ParentStates.Union(new[] { parentNode })) 
            {
                var isConsideredStateTokensGreaterOrEqual = stateInGraph.PlaceTokens.Values.Sum() < tokens.Values.Sum() &&
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
