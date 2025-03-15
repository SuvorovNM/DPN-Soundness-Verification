using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;

namespace DataPetriNetOnSmt.SoundnessVerification.Services;

public static class RelaxedLazySoundnessAnalyzer
{
    private static string[] GetDeadTransitions(DataPetriNet dpn, LabeledTransitionSystem cg)
    {
        var deadTransitions = dpn.Transitions
            .Select(x => x.BaseTransitionId)
            .Except(cg.ConstraintArcs.Select(y => y.Transition.NonRefinedTransitionId))
            .ToArray();

        return deadTransitions;
    }

    public static SoundnessProperties CheckSoundness(DataPetriNet dpn, CoverabilityGraph cg)
    {
        var stateDictionary = cg.ConstraintStates.ToDictionary(x => x as AbstractState, _ => ConstraintStateType.Default);
        
        DefineInitialState(cg, stateDictionary);
        var finalMarking = dpn.Places.Where(x => x.IsFinal).ToArray();
        
        var finalStates = cg.ConstraintStates
            .Where(x => x.Marking.Keys.Intersect(finalMarking).All(y => x.Marking[y] == 1))
            .ToArray();

        DefineFinals(stateDictionary, finalStates);
        DefineUncleanFinals(finalMarking, stateDictionary);

        if (cg.IsFullGraph)
        {
            DefineDeadlocks(cg, stateDictionary);
            DefineStatesWithNoWayToFinals(cg, stateDictionary, finalStates);
        }

        var unfeasibleTransitions = cg.ConstraintArcs
            .GroupBy(a => a.Transition.NonRefinedTransitionId)
            .ToDictionary(
                arcsGroup => arcsGroup.Key,
                arcsGroup =>
                    arcsGroup.All(a => stateDictionary[a.TargetState].HasFlag(ConstraintStateType.NoWayToFinalMarking)))
            .Where(a => a.Value)
            .Select(a => a.Key)
            .Union(GetDeadTransitions(dpn, cg))
            .ToArray();

        var hasDeadlocks = cg.ConstraintStates.Aggregate(false, (current, constraintState) => current | stateDictionary[constraintState].HasFlag(ConstraintStateType.Deadlock));

        var isSound = unfeasibleTransitions.Length == 0;

        return new SoundnessProperties(
            SoundnessType.RelaxedLazy, 
            stateDictionary, 
            cg.IsFullGraph,
            unfeasibleTransitions, 
            hasDeadlocks, 
            isSound);
    }
    
    
    private static void DefineDeadlocks(CoverabilityGraph cg, Dictionary<AbstractState, ConstraintStateType> stateDictionary)
    {
        cg.ConstraintStates
            .Where(x=>!stateDictionary[x].HasFlag(ConstraintStateType.Final) && !stateDictionary[x].HasFlag(ConstraintStateType.UncleanFinal))
            .Where(x => cg.ConstraintArcs.All(y => y.SourceState != x))
            .ToList()
            .ForEach(x => stateDictionary[x] |= ConstraintStateType.Deadlock);
    }

    // Доработать
    private static void DefineStatesWithNoWayToFinals(
        CoverabilityGraph cg,
        Dictionary<AbstractState, ConstraintStateType> stateDictionary,
        LtsState[] finalStates)

    {
        var statesLeadingToFinals = new List<LtsState>(finalStates);
        var intermediateStates = new List<LtsState>(finalStates);
        var stateIncidenceDict = cg.ConstraintArcs
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

        cg.ConstraintStates
            .Except(statesLeadingToFinals)
            .ToList()
            .ForEach(x => stateDictionary[x] |= ConstraintStateType.NoWayToFinalMarking);
    }
    
    private static void DefineUncleanFinals(
        Place[] terminalNodes,
        Dictionary<AbstractState, ConstraintStateType> stateDictionary)
    {
        var uncleanFinalNodes = stateDictionary
            .Where(x => x.Key.Marking.Keys.Intersect(terminalNodes).Any(y => x.Key.Marking[y] > 1))
            .Select(x => x.Key);
        
        foreach (var cgNode in uncleanFinalNodes)
        {
            if (cgNode.Marking.Keys.Intersect(terminalNodes).Any(y => cgNode.Marking[y] > 1))
            {
                stateDictionary[cgNode] |= ConstraintStateType.UncleanFinal;
            }
        }
    }

    private static void DefineFinals(Dictionary<AbstractState, ConstraintStateType> stateDictionary, LtsState[] finalStates)
    {
        Array.ForEach(finalStates, x => stateDictionary[x] |= ConstraintStateType.Final);
    }

    private static void DefineInitialState(CoverabilityGraph cg, Dictionary<AbstractState, ConstraintStateType> stateDictionary)
    {
        stateDictionary[cg.InitialState] |= ConstraintStateType.Initial;
    }
}