using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using System.ComponentModel.DataAnnotations;

namespace DataPetriNetOnSmt.SoundnessVerification.Services
{
    public record SoundnessProperties(
        Dictionary<ConstraintState, ConstraintStateType> StateTypes,
        bool Boundedness,
        List<string> DeadTransitions,
        bool Deadlocks,
        bool Soundness);

    public static class ConstraintGraphAnalyzer
    {
        public static SoundnessProperties CheckSoundness(DataPetriNet dpn, LabeledTransitionSystem cg)
        {
            var stateTypes = GetStatesDividedByTypesNew(cg, dpn.Places.Where(x => x.IsFinal).ToArray());

            var boundedness = cg.IsFullGraph;

            var restoredDpnTransitions = dpn.Transitions
                .Select(x => x.Id.IndexOf("_st_") >= 0
                    ? x.Id[..x.Id.IndexOf("_st_")]
                    : x.Id);
            var restoredConstraintArcTransitions = cg.ConstraintArcs
                .Where(x => !x.Transition.IsSilent)
                .Select(x => x.Transition.Id.IndexOf("_st_") >= 0
                    ? x.Transition.Id[..x.Transition.Id.IndexOf("_st_")]
                    : x.Transition.Id);

            var deadTransitions = restoredDpnTransitions
                .Except(restoredConstraintArcTransitions)
                .ToList();

            var hasDeadlocks = false;
            var isFinalMarkingAlwaysReachable = true;
            var isFinalMarkingClean = true;

            foreach (var constraintState in cg.ConstraintStates)
            {
                hasDeadlocks |= stateTypes[constraintState].HasFlag(ConstraintStateType.Deadlock);
                isFinalMarkingAlwaysReachable &= !stateTypes[constraintState].HasFlag(ConstraintStateType.NoWayToFinalMarking);
                isFinalMarkingClean &= !stateTypes[constraintState].HasFlag(ConstraintStateType.UncleanFinal);
            }

            var isSound = boundedness
                && !hasDeadlocks
                && isFinalMarkingAlwaysReachable
                && isFinalMarkingClean
                && deadTransitions.Count == 0;

            return new SoundnessProperties(stateTypes, cg.IsFullGraph, deadTransitions, hasDeadlocks, isSound);
        }

        public static Dictionary<ConstraintState, ConstraintStateType> GetStatesDividedByTypesNew(LabeledTransitionSystem graph, IEnumerable<Place> terminalNodes)
        {
            var stateDictionary = graph.ConstraintStates.ToDictionary(x => x, y => ConstraintStateType.Default);

            if (graph.IsFullGraph)
            {
                DefineInitialState(graph, stateDictionary);
                DefineDeadlocks(graph, terminalNodes, stateDictionary);

                var finalStates = graph.ConstraintStates
                        .Where(x => x.PlaceTokens.Keys.Intersect(terminalNodes).All(y => x.PlaceTokens[y] > 0));

                DefineFinals(stateDictionary, finalStates);
                DefineUncleanFinals(terminalNodes, stateDictionary, finalStates);
                DefineStatesWithNoWayToFinals(graph, stateDictionary, finalStates);
            }

            return stateDictionary;


            static void DefineDeadlocks(LabeledTransitionSystem graph, IEnumerable<Place> terminalNodes, Dictionary<ConstraintState, ConstraintStateType> stateDictionary)
            {
                graph.ConstraintStates
                                .Where(x => x.PlaceTokens.Keys.Except(terminalNodes).Any(y => x.PlaceTokens[y] > 0)
                                    && !graph.ConstraintArcs.Any(y => y.SourceState == x))
                                .ToList()
                                .ForEach(x => stateDictionary[x] = stateDictionary[x] | ConstraintStateType.Deadlock);
            }

            static void DefineStatesWithNoWayToFinals(LabeledTransitionSystem graph, Dictionary<ConstraintState, ConstraintStateType> stateDictionary, IEnumerable<ConstraintState> finalStates)
            {
                var statesLeadingToFinals = new List<ConstraintState>(finalStates);
                var intermediateStates = new List<ConstraintState>(finalStates);
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
                    intermediateStates = new List<ConstraintState>(nextStates);
                } while (intermediateStates.Count > 0);

                graph.ConstraintStates
                    .Except(statesLeadingToFinals)
                    .ToList()
                    .ForEach(x => stateDictionary[x] = stateDictionary[x] | ConstraintStateType.NoWayToFinalMarking);
            }

            static void DefineUncleanFinals(IEnumerable<Place> terminalNodes, Dictionary<ConstraintState, ConstraintStateType> stateDictionary, IEnumerable<ConstraintState> finalStates)
            {
                finalStates
                                .Where(x => x.PlaceTokens.Keys.Any(y => x.PlaceTokens[y] > 1) || x.PlaceTokens.Keys.Except(terminalNodes).Any(y => x.PlaceTokens[y] > 0))
                                .ToList()
                                .ForEach(x => stateDictionary[x] = stateDictionary[x] | ConstraintStateType.UncleanFinal);
            }

            static void DefineFinals(Dictionary<ConstraintState, ConstraintStateType> stateDictionary, IEnumerable<ConstraintState> finalStates)
            {
                finalStates
                                .ToList()
                                .ForEach(x => stateDictionary[x] = stateDictionary[x] | ConstraintStateType.Final);
            }

            static void DefineInitialState(LabeledTransitionSystem graph, Dictionary<ConstraintState, ConstraintStateType> stateDictionary)
            {
                stateDictionary[graph.InitialState] = stateDictionary[graph.InitialState] | ConstraintStateType.Initial;
            }
        }

        public static Dictionary<StateType, List<ConstraintState>> GetStatesDividedByTypes(ConstraintGraph graph, IEnumerable<Place> terminalNodes)
        {
            var stateDict = new Dictionary<StateType, List<ConstraintState>>();

            stateDict[StateType.Initial] = new List<ConstraintState> { graph.InitialState };
            stateDict[StateType.Deadlock] = new List<ConstraintState>();
            stateDict[StateType.UncleanFinal] = new List<ConstraintState>();
            stateDict[StateType.CleanFinal] = new List<ConstraintState>();
            stateDict[StateType.NoWayToFinalMarking] = new List<ConstraintState>();
            stateDict[StateType.SoundIntermediate] = new List<ConstraintState>();

            if (graph.IsFullGraph)
            {
                FillDeadlocks(graph, terminalNodes, stateDict);
                FillCleanFinals(graph, terminalNodes, stateDict);
                FillUncleanFinals(graph, terminalNodes, stateDict);
                FillNoWayToFinalMarking(graph, stateDict);
            }

            FillSoundIntermediate(graph, stateDict);

            return stateDict;

            static void FillDeadlocks(ConstraintGraph graph, IEnumerable<Place> terminalNodes, Dictionary<StateType, List<ConstraintState>> stateDict)
            {
                stateDict[StateType.Deadlock] = graph.ConstraintStates
                                .Where(x => x.PlaceTokens.Keys.Except(terminalNodes).Any(y => x.PlaceTokens[y] > 0)
                                    && !graph.ConstraintArcs.Any(y => y.SourceState == x))
                                .ToList();
            }

            static void FillUncleanFinals(ConstraintGraph graph, IEnumerable<Place> terminalNodes, Dictionary<StateType, List<ConstraintState>> stateDict)
            {
                stateDict[StateType.UncleanFinal] = graph.ConstraintStates
                    .Where(x => x.PlaceTokens.Keys.Intersect(terminalNodes).All(y => x.PlaceTokens[y] > 0)
                        && (x.PlaceTokens.Keys.Any(y => x.PlaceTokens[y] > 1) || x.PlaceTokens.Keys.Except(terminalNodes).Any(y => x.PlaceTokens[y] > 0)))
                    .ToList();
            }

            static void FillCleanFinals(ConstraintGraph graph, IEnumerable<Place> terminalNodes, Dictionary<StateType, List<ConstraintState>> stateDict)
            {
                stateDict[StateType.CleanFinal] = graph.ConstraintStates
                                .Where(x => x.PlaceTokens.Keys.Except(terminalNodes).All(y => x.PlaceTokens[y] == 0)
                                    && x.PlaceTokens.Keys.Intersect(terminalNodes).Any(y => x.PlaceTokens[y] > 0))
                                .ToList();
            }

            static void FillNoWayToFinalMarking(ConstraintGraph graph, Dictionary<StateType, List<ConstraintState>> stateDict)
            {
                var statesLeadingToFinals = new List<ConstraintState>(stateDict[StateType.CleanFinal]);
                var intermediateStates = new List<ConstraintState>(stateDict[StateType.CleanFinal]);
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
                    intermediateStates = new List<ConstraintState>(nextStates);
                } while (intermediateStates.Count > 0);

                stateDict[StateType.NoWayToFinalMarking] = graph.ConstraintStates
                    .Except(statesLeadingToFinals)
                    .ToList();
            }

            static void FillSoundIntermediate(ConstraintGraph graph, Dictionary<StateType, List<ConstraintState>> stateDict)
            {
                stateDict[StateType.SoundIntermediate] = graph.ConstraintStates
                                .Except(stateDict[StateType.Initial])
                                .Except(stateDict[StateType.Deadlock])
                                .Except(stateDict[StateType.UncleanFinal])
                                .Except(stateDict[StateType.CleanFinal])
                                .Except(stateDict[StateType.NoWayToFinalMarking]) //TODO: Consider removing states with no way to final marking form soundIntermediate
                                .ToList();
            }
        }
    }
}
