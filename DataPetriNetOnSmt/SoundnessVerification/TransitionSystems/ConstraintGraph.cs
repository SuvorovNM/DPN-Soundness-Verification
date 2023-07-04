using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.Extensions;
using Microsoft.VisualBasic;
using Microsoft.Z3;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace DataPetriNetOnSmt.SoundnessVerification.TransitionSystems
{
    public class ColoredConstraintGraph : ConstraintGraph
    {
        public Dictionary<LtsState, CtStateColor> StateColorDictionary { get; set; }
        public ColoredConstraintGraph(DataPetriNet dataPetriNet)
            : base(dataPetriNet)
        {
            StateColorDictionary = new Dictionary<LtsState, CtStateColor>();
        }

        public override void GenerateGraph()
        {
            base.GenerateGraph();

            AddColors();
        }

        private void AddColors()
        {
            var finalStates = ConstraintStates
                .Where(x => x.Marking.CompareTo(DataPetriNet.FinalMarking) == MarkingComparisonResult.Equal);

            var statesLeadingToFinals = new List<LtsState>(finalStates);
            var intermediateStates = new List<LtsState>(finalStates);
            var stateIncidenceDict = ConstraintArcs
                .GroupBy(x => x.TargetState)
                .ToDictionary(x => x.Key, y => y.Select(x => x.SourceState).ToList());


            do
            {
                var nextStates = intermediateStates
                    .Where(x => stateIncidenceDict.ContainsKey(x))
                    .SelectMany(x => stateIncidenceDict[x])
                    .Where(x => !statesLeadingToFinals.Contains(x))
                    .Distinct();
                statesLeadingToFinals.AddRange(intermediateStates);
                intermediateStates = new List<LtsState>(nextStates);
            } while (intermediateStates.Count > 0);

            var statesNotLeadingToFinals = ConstraintStates
                .Except(statesLeadingToFinals)
                .ToList();

            foreach (var state in statesLeadingToFinals)
            {
                StateColorDictionary[state] = CtStateColor.Green;
            }
            foreach (var state in ConstraintStates.Except(statesLeadingToFinals))
            {
                StateColorDictionary[state] = CtStateColor.Red;
            }
        }
    }

    public class ConstraintGraph : LabeledTransitionSystem // TODO: insert Ids
    {
        public ConstraintGraph(DataPetriNet dataPetriNet)
        : base(dataPetriNet)
        {

        }

        public override void GenerateGraph()
        {
            IsFullGraph = false;
            Stopwatch stopwatch = Stopwatch.StartNew();

            while (StatesToConsider.Count > 0)
            {
                var currentState = StatesToConsider.Pop();

                foreach (var transition in currentState.Marking.GetEnabledTransitions(DataPetriNet))
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

                            var coveredNode = FindParentNodeForWhichComparisonResultForCurrentNodeHolds
                                (stateToAddInfo, currentState, MarkingComparisonResult.GreaterThan);
                            if (coveredNode != null)
                            {
                                return; // The net is unbounded
                            }

                            AddNewState(currentState, new LtsTransition(transition), stateToAddInfo);
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

                            var coveredNode = FindParentNodeForWhichComparisonResultForCurrentNodeHolds
                                (stateToAddInfo, currentState, MarkingComparisonResult.GreaterThan);
                            if (coveredNode != null)
                            {
                                return; // The net is unbounded
                            }

                            AddNewState(currentState, new LtsTransition(transition, true), stateToAddInfo);                           
                        }
                    }
                }
            }
            stopwatch.Stop();
            Milliseconds = stopwatch.ElapsedMilliseconds;
            IsFullGraph = true;
        }
    }
}
