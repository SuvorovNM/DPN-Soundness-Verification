using DPN.Models;
using DPN.Models.DPNElements;
using DPN.Models.Enums;
using DPN.Models.Extensions;
using DPN.SoundnessVerification.TransitionSystems;

namespace DPN.SoundnessVerification.Services;

public static class RelaxedLazySoundnessAnalyzer
{
    private static string[] GetDeadTransitions<TAbsState, TAbsTransition, TAbsArc>(DataPetriNet dpn,
        AbstractStateSpaceStructure<TAbsState, TAbsTransition, TAbsArc> cg)
        where TAbsState : AbstractState, new()
        where TAbsTransition : AbstractTransition
        where TAbsArc : AbstractArc<TAbsState, TAbsTransition>
    {
        var deadTransitions = dpn.Transitions
            .Select(x => x.BaseTransitionId)
            .Except(cg.ConstraintArcs.Select(y => y.Transition.NonRefinedTransitionId))
            .ToArray();

        return deadTransitions;
    }

    public static SoundnessProperties CheckSoundness(DataPetriNet dpn, CoverabilityGraph cg)
    {
        var stateDictionary =
            cg.ConstraintStates.ToDictionary(x => x as AbstractState, _ => ConstraintStateType.Default);

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

        var hasDeadlocks = cg.ConstraintStates.Aggregate(false,
            (current, constraintState) =>
                current | stateDictionary[constraintState].HasFlag(ConstraintStateType.Deadlock));

        var isSound = unfeasibleTransitions.Length == 0;

        // TODO: update dictionary
        return new SoundnessProperties(
            SoundnessType.RelaxedLazy,
            stateDictionary.ToDictionary(x=>x.Key.Id, x=>x.Value),
            cg.IsFullGraph,
            unfeasibleTransitions,
            hasDeadlocks,
            isSound);
    }

    public static SoundnessProperties CheckSoundness(DataPetriNet dpn, CoverabilityTree ct)
    {
        var stateDictionary =
            ct.ConstraintStates.ToDictionary(x => x as AbstractState, _ => ConstraintStateType.Default);

        DefineInitialState(ct, stateDictionary);
        var finalMarking = dpn.Places.Where(x => x.IsFinal).ToArray();

        var finalStates = ct.ConstraintStates
            .Where(x => x.Marking.Keys.Intersect(finalMarking).All(y => x.Marking[y] == 1))
            .ToArray();

        DefineFinals(stateDictionary, finalStates);
        DefineUncleanFinals(finalMarking, stateDictionary);


        DefineDeadlocks(ct, stateDictionary);
        DefineStatesWithNoWayToFinals(ct, stateDictionary, finalStates);


        var unfeasibleTransitions = ct.ConstraintArcs
            .GroupBy(a => a.Transition.NonRefinedTransitionId)
            .ToDictionary(
                arcsGroup => arcsGroup.Key,
                arcsGroup =>
                    arcsGroup.All(a => stateDictionary[a.TargetState].HasFlag(ConstraintStateType.NoWayToFinalMarking)))
            .Where(a => a.Value)
            .Select(a => a.Key)
            .Union(GetDeadTransitions(dpn, ct))
            .ToArray();

        var hasDeadlocks = ct.ConstraintStates.Aggregate(false,
            (current, constraintState) =>
                current | stateDictionary[constraintState].HasFlag(ConstraintStateType.Deadlock));

        var isSound = unfeasibleTransitions.Length == 0;

        var isBounded = ct.ConstraintStates.All(s => s.StateType != CtStateType.StrictlyCovered);

        return new SoundnessProperties(
            SoundnessType.RelaxedLazy,
            stateDictionary.ToDictionary(x=>x.Key.Id, x=>x.Value),
            isBounded,
            unfeasibleTransitions,
            hasDeadlocks,
            isSound);
    }


    private static void DefineDeadlocks(CoverabilityGraph cg,
        Dictionary<AbstractState, ConstraintStateType> stateDictionary)
    {
        cg.ConstraintStates
            .Where(x => !stateDictionary[x].HasFlag(ConstraintStateType.Final) &&
                        !stateDictionary[x].HasFlag(ConstraintStateType.UncleanFinal))
            .Where(x => cg.ConstraintArcs.All(y => y.SourceState != x))
            .ToList()
            .ForEach(x => stateDictionary[x] |= ConstraintStateType.Deadlock);
    }
    
    private static void DefineDeadlocks(CoverabilityTree ct,
        Dictionary<AbstractState, ConstraintStateType> stateDictionary)
    {
        ct.ConstraintStates
            .Where(x => !stateDictionary[x].HasFlag(ConstraintStateType.Final) &&
                        !stateDictionary[x].HasFlag(ConstraintStateType.UncleanFinal))
            .Where(x => x.StateType == CtStateType.NonCovered && ct.ConstraintArcs.All(y => y.SourceState != x))
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
    
    private static void DefineStatesWithNoWayToFinals(
        CoverabilityTree ct,
        Dictionary<AbstractState, ConstraintStateType> stateDictionary,
        CtState[] finalStates)

    {
        var statesLeadingToFinals = new HashSet<CtState>(finalStates);
        var intermediateStates = new HashSet<CtState>(finalStates);
        var stateIncidenceDict = ct.ConstraintArcs
            .GroupBy(x => x.TargetState)
            .ToDictionary(x => x.Key, y => y.Select(x => x.SourceState).ToList());

        do
        {
            var nextStates = intermediateStates
                .Where(x => stateIncidenceDict.ContainsKey(x))
                .SelectMany(x => stateIncidenceDict[x])
                .Where(x => !statesLeadingToFinals.Contains(x))
                .ToHashSet();

            var covered = ct
                .ConstraintStates
                .Where(s => s.CoveredNode != null && nextStates.Contains(s.CoveredNode) &&
                            !statesLeadingToFinals.Contains(s.CoveredNode));

            statesLeadingToFinals.AddRange(intermediateStates);
            intermediateStates = nextStates.Union(covered).ToHashSet();
        } while (intermediateStates.Count > 0);
        
        ct.ConstraintStates
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

    private static void DefineFinals(Dictionary<AbstractState, ConstraintStateType> stateDictionary,
        LtsState[] finalStates)
    {
        Array.ForEach(finalStates, x => stateDictionary[x] |= ConstraintStateType.Final);
    }

    private static void DefineInitialState(CoverabilityGraph cg,
        Dictionary<AbstractState, ConstraintStateType> stateDictionary)
    {
        stateDictionary[cg.InitialState] |= ConstraintStateType.Initial;
    }

    private static void DefineInitialState(CoverabilityTree ct,
        Dictionary<AbstractState, ConstraintStateType> stateDictionary)
    {
        stateDictionary[ct.InitialState] |= ConstraintStateType.Initial;
    }

    private static void DefineFinals(Dictionary<AbstractState, ConstraintStateType> stateDictionary,
        CtState[] finalStates)
    {
        Array.ForEach(finalStates, x => stateDictionary[x] |= ConstraintStateType.Final);
    }
}