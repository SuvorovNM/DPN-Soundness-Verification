using DPN.Models;
using DPN.Models.DPNElements;
using DPN.Models.Enums;
using DPN.SoundnessVerification.TransitionSystems;

namespace DPN.SoundnessVerification.Services;

public static class SoundnessAnalyzer
{
    public static SoundnessProperties CheckSoundness(DataPetriNet dpn, LabeledTransitionSystem cg)
    {
        Dictionary<AbstractState, ConstraintStateType> stateTypes;

        var boundedness = cg.IsFullGraph;
        stateTypes = boundedness 
            ? GetStatesDividedByTypesNew(cg, dpn.Places.Where(x => x.IsFinal).ToArray()) 
            : cg.ConstraintStates.ToDictionary(x => (AbstractState)x, y => ConstraintStateType.Default);

        var deadTransitions = GetDeadTransitions(dpn, cg);

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

        var isSound = boundedness
                      && !hasDeadlocks
                      && isFinalMarkingAlwaysReachable
                      && isFinalMarkingClean
                      && deadTransitions.Length == 0;

        return new SoundnessProperties(
	        SoundnessType.Classical, 
	        stateTypes.ToDictionary(x=>x.Key.Id, x=>x.Value), 
	        cg.IsFullGraph,
            deadTransitions, 
	        hasDeadlocks, 
	        isSound);
    }

    private static string[] GetDeadTransitions(DataPetriNet dpn, LabeledTransitionSystem cg)
    {
        var deadTransitions = dpn.Transitions
            .Select(x => x.BaseTransitionId)
            .Except(cg.ConstraintArcs.Select(y => y.Transition.NonRefinedTransitionId))
            .ToArray();
        return deadTransitions;
    }

    private static Dictionary<AbstractState, ConstraintStateType> GetStatesDividedByTypesNew
        (LabeledTransitionSystem graph, Place[] finalMarking)
    {
        var stateDictionary =
            graph.ConstraintStates.ToDictionary(x => (AbstractState)x, y => ConstraintStateType.Default);

        DefineInitialState(stateDictionary);

        var finalStates = graph.ConstraintStates
            .Where(x => x.Marking.Keys.Intersect(finalMarking).All(y => x.Marking[y] > 0))
            .ToArray();

        DefineFinals(stateDictionary, finalStates);
        DefineUncleanFinals(finalMarking, stateDictionary, finalStates);
        
        DefineDeadlocks(stateDictionary);
        DefineStatesWithNoWayToFinals(stateDictionary, finalStates);

        return stateDictionary;
        
        void DefineDeadlocks(Dictionary<AbstractState, ConstraintStateType> stateDictionary)
        {
            graph.ConstraintStates
                .Where(x=>!stateDictionary[x].HasFlag(ConstraintStateType.Final) && !stateDictionary[x].HasFlag(ConstraintStateType.UncleanFinal))
                .Where(x => graph.ConstraintArcs.All(y => y.SourceState != x))
                .ToList()
                .ForEach(x => stateDictionary[x] |= ConstraintStateType.Deadlock);
        }

        // Доработать
        void DefineStatesWithNoWayToFinals(Dictionary<AbstractState, ConstraintStateType> stateDictionary,
            IEnumerable<LtsState> finalStates)

        {
            var statesLeadingToFinals = new List<LtsState>(finalStates);
            var intermediateStates = new List<LtsState>(finalStates);
            var stateIncidenceDict = graph.ConstraintArcs
                .GroupBy(x => x.TargetState)
                .ToDictionary(x => x.Key, y => y.Select(x => x.SourceState).ToList());

            do //check for covered
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
            Dictionary<AbstractState, ConstraintStateType> stateDictionary, IEnumerable<LtsState> finalStates)
        {
            finalStates
                .Where(x => x.Marking.Keys.Any(y => x.Marking[y] > 1) ||
                            x.Marking.Keys.Except(terminalNodes).Any(y => x.Marking[y] > 0))
                .ToList()
                .ForEach(x => stateDictionary[x] |= ConstraintStateType.UncleanFinal);
        }

        void DefineFinals(Dictionary<AbstractState, ConstraintStateType> stateDictionary,
            IEnumerable<LtsState> finalStates)
        {
            finalStates
                .ToList()
                .ForEach(x => stateDictionary[x] |= ConstraintStateType.Final);
        }

        void DefineInitialState(Dictionary<AbstractState, ConstraintStateType> stateDictionary)
        {
            stateDictionary[graph.InitialState] |= ConstraintStateType.Initial;
        }
    }
}