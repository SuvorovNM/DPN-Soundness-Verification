using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;

namespace DataPetriNetOnSmt.SoundnessVerification.Services;

public static class LazySoundnessAnalyzer
{
    private static string[] GetDeadTransitions(DataPetriNet dpn, LabeledTransitionSystem cg)
    {
        var deadTransitions = dpn.Transitions
            .Select(x => x.BaseTransitionId)
            .Except(cg.ConstraintArcs.Select(y => y.Transition.NonRefinedTransitionId))
            .ToArray();
        return deadTransitions;
    }

    public static SoundnessProperties CheckLazySoundness(DataPetriNet dpn, CoverabilityGraph cg)
    {
        var stateTypes = GetStatesDividedByTypes(cg, dpn.Places.Where(x => x.IsFinal).ToArray());

        var hasDeadlocks = false;
        var isFinalMarkingAlwaysReachable = true;
        var isFinalMarkingClean = true;

        foreach (var constraintState in cg.ConstraintStates)
        {
            hasDeadlocks |= stateTypes[constraintState].HasFlag(ConstraintStateType.Deadlock);
            isFinalMarkingAlwaysReachable &=
                !stateTypes[constraintState].HasFlag(ConstraintStateType.NoWayToFinalMarking);
            isFinalMarkingClean &= !stateTypes[constraintState].HasFlag(ConstraintStateType.UncleanFinal);
        }

        var isSound = isFinalMarkingAlwaysReachable && isFinalMarkingClean;

        return new SoundnessProperties(SoundnessType.Lazy, stateTypes, cg.IsFullGraph,
            GetDeadTransitions(dpn, cg), hasDeadlocks, isSound);
    }

    private static Dictionary<AbstractState, ConstraintStateType> GetStatesDividedByTypes
        (CoverabilityGraph graph, Place[] finalMarking)
    {
        var stateDictionary = graph.ConstraintStates.ToDictionary(x => x as AbstractState, y => ConstraintStateType.Default);

        DefineInitialState(stateDictionary);
        DefineDeadlocks(finalMarking, stateDictionary);

        var finalStates = graph.ConstraintStates
            .Where(x => x.Marking.Keys.Intersect(finalMarking).All(y => x.Marking[y] == 1))
            .ToArray();

        DefineFinals(stateDictionary, finalStates);
        DefineUncleanFinals(finalMarking, stateDictionary, finalStates);
        DefineStatesWithNoWayToFinals(stateDictionary, finalStates);

        return stateDictionary;


        void DefineDeadlocks(IEnumerable<Place> terminalNodes,
            Dictionary<AbstractState, ConstraintStateType> stateDictionary)
        {
            graph.ConstraintStates
                .Where(x => x.Marking.Keys.Except(terminalNodes).Any(y => x.Marking[y] > 0)
                            && graph.ConstraintArcs.All(y => y.SourceState != x))
                .ToList()
                .ForEach(x => stateDictionary[x] |= ConstraintStateType.Deadlock);
        }

        // Доработать
        void DefineStatesWithNoWayToFinals(Dictionary<AbstractState, ConstraintStateType> stateDictionary,
            LtsState[] finalStates)

        {
            var statesLeadingToFinals = new List<LtsState>(finalStates);
            var intermediateStates = new List<LtsState>(finalStates);
            var stateIncidenceDict = graph.ConstraintArcs
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

            graph.ConstraintStates
                .Except(statesLeadingToFinals)
                .ToList()
                .ForEach(x => stateDictionary[x] |= ConstraintStateType.NoWayToFinalMarking);
        }

        static void DefineUncleanFinals(IEnumerable<Place> terminalNodes,
            Dictionary<AbstractState, ConstraintStateType> stateDictionary, LtsState[] finalStates)
        {
            finalStates
                .Where(x => x.Marking.Keys.Intersect(terminalNodes).Any(y => x.Marking[y] > 1))
                .ToList()
                .ForEach(x => stateDictionary[x] |= ConstraintStateType.UncleanFinal);
        }

        void DefineFinals(Dictionary<AbstractState, ConstraintStateType> stateDictionary, LtsState[] finalStates)
        {
            Array.ForEach(finalStates, x => stateDictionary[x] |= ConstraintStateType.Final);
        }

        void DefineInitialState(Dictionary<AbstractState, ConstraintStateType> stateDictionary)
        {
            stateDictionary[graph.InitialState] |= ConstraintStateType.Initial;
        }
    }
}