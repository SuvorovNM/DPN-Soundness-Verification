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
    public class ConstraintGraph // TODO: insert Ids
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
                    var readOnlyExpressions = transition.Guard.ConstraintExpressions
                                                    .Where(x => x.ConstraintVariable.VariableType == VariableType.Read)
                                                    .ToList();

                    if (expressionService.CanBeSatisfied(expressionService.ConcatExpressions(currentState.Constraints, readOnlyExpressions)))
                    {
                        var constraintsIfTransitionFires = expressionService
                            .ConcatExpressions(currentState.Constraints, transition.Guard.ConstraintExpressions);

                        if (expressionService.CanBeSatisfied(constraintsIfTransitionFires))
                        {
                            var updatedMarking = transition.FireOnGivenMarking(currentState.PlaceTokens);

                            if (IsMonotonicallyIncreasedWithUnchangedConstraints(updatedMarking, constraintsIfTransitionFires))
                            {
                                return false; // The net is unbound
                            }

                            AddNewState(currentState, new ConstraintTransition(transition), updatedMarking, constraintsIfTransitionFires);
                        }
                    }

                    // Considering silent transition
                    var negatedGuardExpressions = GetInvertedReadExpression(transition.Guard.ConstraintExpressions);

                    var constraintsIfSilentTransitionFires = expressionService
                        .ConcatExpressions(currentState.Constraints, negatedGuardExpressions);

                    if (expressionService.CanBeSatisfied(constraintsIfSilentTransitionFires) &&
                        !expressionService.AreEqual(currentState.Constraints, constraintsIfSilentTransitionFires))
                    {
                        AddNewState(currentState, new ConstraintTransition(transition, true), currentState.PlaceTokens, constraintsIfSilentTransitionFires);
                    }
                }
            }

            return true;
        }

        private void AddNewState(ConstraintState currentState, 
                                ConstraintTransition transition, 
                                Dictionary<Node,int> marking, 
                                List<IConstraintExpression> constraintsIfFires)
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
                var canFire = transition.PreSetPlaces.All(x => marking[x] > 0);
                if (canFire)
                {
                    transitionsWhichCanFire.Add(transition);
                }
            }

            return transitionsWhichCanFire;
        }

        private bool IsMonotonicallyIncreasedWithUnchangedConstraints(Dictionary<Node, int> tokens, List<IConstraintExpression> constraintsIfFires)
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

        private ConstraintState FindEqualStateInGraph(Dictionary<Node, int> tokens, List<IConstraintExpression> constraintsIfFires)
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

        private List<IConstraintExpression> GetInvertedReadExpression(List<IConstraintExpression> sourceExpression)
        {
            if (sourceExpression is null)
            {
                throw new ArgumentNullException(nameof(sourceExpression));
            }
            if (sourceExpression.Count == 0)
            {
                return sourceExpression;
            }

            var blocks = new List<List<IConstraintExpression>>();
            var expressionDuringExecution = new List<IConstraintExpression>(sourceExpression);
            do
            {
                var expressionBlock = CutFirstExpressionBlock(expressionDuringExecution)
                    .Where(x => x.ConstraintVariable.VariableType == VariableType.Read)
                    .Select(x => x.GetInvertedExpression())
                    .ToList();

                if (expressionBlock.Count > 0)
                {
                    blocks.Add(expressionBlock);
                }
            } while (expressionDuringExecution.Count > 0);

            var allCombinations = GetAllPossibleCombos(blocks);
            return MakeSingleExpressionListFromMultipleLists(allCombinations);
        }

        private static List<IConstraintExpression> MakeSingleExpressionListFromMultipleLists(List<List<IConstraintExpression>> allCombinations)
        {
            var resultExpression = new List<IConstraintExpression>();

            foreach (var combination in allCombinations.Where(x => x.Count > 0))
            {
                var firstExpressionInBlock = combination[0].Clone();
                firstExpressionInBlock.LogicalConnective = LogicalConnective.Or;
                resultExpression.Add(firstExpressionInBlock);

                for (int i = 1; i < combination.Count; i++)
                {
                    var currentExpression = combination[i].Clone();
                    currentExpression.LogicalConnective = LogicalConnective.And;
                    resultExpression.Add(currentExpression);
                }
            }
            if (resultExpression.Count > 0)
            {
                resultExpression[0].LogicalConnective = LogicalConnective.Empty;
            }

            return resultExpression;
        }

        private static List<List<IConstraintExpression>> GetAllPossibleCombos(List<List<IConstraintExpression>> expressions)
        {
            IEnumerable<List<IConstraintExpression>> combos = new[] { new List<IConstraintExpression>() };

            foreach (var inner in expressions)
                combos = from c in combos
                         from i in inner
                         select c.Union(new List<IConstraintExpression> { i }).ToList();

            return combos.ToList();
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
