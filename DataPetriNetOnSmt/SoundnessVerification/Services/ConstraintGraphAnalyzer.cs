using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using System.ComponentModel.DataAnnotations;

namespace DataPetriNetOnSmt.SoundnessVerification.Services
{
    public record SoundnessProperties(
        Dictionary<LtsState, ConstraintStateType> StateTypes,
        bool Boundedness,
        List<string> DeadTransitions,
        bool Deadlocks,
        bool Soundness);

    public static class LtsAnalyzer
    {
        public static SoundnessProperties CheckSoundness(DataPetriNet dpn, LabeledTransitionSystem cg)
        {
            var stateTypes = GetStatesDividedByTypesNew(cg, dpn.Places.Where(x => x.IsFinal).ToArray());

            var boundedness = cg.IsFullGraph;

            var deadTransitions = dpn.Transitions
                .Select(x => x.BaseTransitionId)
                .Except(cg.ConstraintArcs.Select(y => y.Transition.NonRefinedTransitionId))
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

        public static Dictionary<LtsState, ConstraintStateType> GetStatesDividedByTypesNew(LabeledTransitionSystem graph, IEnumerable<Place> terminalNodes)
        {
            var stateDictionary = graph.ConstraintStates.ToDictionary(x => x, y => ConstraintStateType.Default);

            if (graph.IsFullGraph)
            {
                DefineInitialState(graph, stateDictionary);
                DefineDeadlocks(graph, terminalNodes, stateDictionary);

                var finalStates = graph.ConstraintStates
                        .Where(x => x.Marking.Keys.Intersect(terminalNodes).All(y => x.Marking[y] > 0));

                DefineFinals(stateDictionary, finalStates);
                DefineUncleanFinals(terminalNodes, stateDictionary, finalStates);
                DefineStatesWithNoWayToFinals(graph, stateDictionary, finalStates);
            }

            return stateDictionary;


            static void DefineDeadlocks(LabeledTransitionSystem graph, IEnumerable<Place> terminalNodes, Dictionary<LtsState, ConstraintStateType> stateDictionary)
            {
                graph.ConstraintStates
                                .Where(x => x.Marking.Keys.Except(terminalNodes).Any(y => x.Marking[y] > 0)
                                    && !graph.ConstraintArcs.Any(y => y.SourceState == x))
                                .ToList()
                                .ForEach(x => stateDictionary[x] = stateDictionary[x] | ConstraintStateType.Deadlock);
            }

            static void DefineStatesWithNoWayToFinals(LabeledTransitionSystem graph, Dictionary<LtsState, ConstraintStateType> stateDictionary, IEnumerable<LtsState> finalStates)
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
                    .ForEach(x => stateDictionary[x] = stateDictionary[x] | ConstraintStateType.NoWayToFinalMarking);
            }

            static void DefineUncleanFinals(IEnumerable<Place> terminalNodes, Dictionary<LtsState, ConstraintStateType> stateDictionary, IEnumerable<LtsState> finalStates)
            {
                finalStates
                                .Where(x => x.Marking.Keys.Any(y => x.Marking[y] > 1) || x.Marking.Keys.Except(terminalNodes).Any(y => x.Marking[y] > 0))
                                .ToList()
                                .ForEach(x => stateDictionary[x] = stateDictionary[x] | ConstraintStateType.UncleanFinal);
            }

            static void DefineFinals(Dictionary<LtsState, ConstraintStateType> stateDictionary, IEnumerable<LtsState> finalStates)
            {
                finalStates
                                .ToList()
                                .ForEach(x => stateDictionary[x] = stateDictionary[x] | ConstraintStateType.Final);
            }

            static void DefineInitialState(LabeledTransitionSystem graph, Dictionary<LtsState, ConstraintStateType> stateDictionary)
            {
                stateDictionary[graph.InitialState] = stateDictionary[graph.InitialState] | ConstraintStateType.Initial;
            }
        }

        public static Dictionary<StateType, List<LtsState>> GetStatesDividedByTypes(ConstraintGraph graph, IEnumerable<Place> terminalNodes)
        {
            var stateDict = new Dictionary<StateType, List<LtsState>>();

            stateDict[StateType.Initial] = new List<LtsState> { graph.InitialState };
            stateDict[StateType.Deadlock] = new List<LtsState>();
            stateDict[StateType.UncleanFinal] = new List<LtsState>();
            stateDict[StateType.CleanFinal] = new List<LtsState>();
            stateDict[StateType.NoWayToFinalMarking] = new List<LtsState>();
            stateDict[StateType.SoundIntermediate] = new List<LtsState>();

            if (graph.IsFullGraph)
            {
                FillDeadlocks(graph, terminalNodes, stateDict);
                FillCleanFinals(graph, terminalNodes, stateDict);
                FillUncleanFinals(graph, terminalNodes, stateDict);
                FillNoWayToFinalMarking(graph, stateDict);
            }

            FillSoundIntermediate(graph, stateDict);

            return stateDict;

            static void FillDeadlocks(ConstraintGraph graph, IEnumerable<Place> terminalNodes, Dictionary<StateType, List<LtsState>> stateDict)
            {
                stateDict[StateType.Deadlock] = graph.ConstraintStates
                                .Where(x => x.Marking.Keys.Except(terminalNodes).Any(y => x.Marking[y] > 0)
                                    && !graph.ConstraintArcs.Any(y => y.SourceState == x))
                                .ToList();
            }

            static void FillUncleanFinals(ConstraintGraph graph, IEnumerable<Place> terminalNodes, Dictionary<StateType, List<LtsState>> stateDict)
            {
                stateDict[StateType.UncleanFinal] = graph.ConstraintStates
                    .Where(x => x.Marking.Keys.Intersect(terminalNodes).All(y => x.Marking[y] > 0)
                        && (x.Marking.Keys.Any(y => x.Marking[y] > 1) || x.Marking.Keys.Except(terminalNodes).Any(y => x.Marking[y] > 0)))
                    .ToList();
            }

            static void FillCleanFinals(ConstraintGraph graph, IEnumerable<Place> terminalNodes, Dictionary<StateType, List<LtsState>> stateDict)
            {
                stateDict[StateType.CleanFinal] = graph.ConstraintStates
                                .Where(x => x.Marking.Keys.Except(terminalNodes).All(y => x.Marking[y] == 0)
                                    && x.Marking.Keys.Intersect(terminalNodes).Any(y => x.Marking[y] > 0))
                                .ToList();
            }

            static void FillNoWayToFinalMarking(ConstraintGraph graph, Dictionary<StateType, List<LtsState>> stateDict)
            {
                var statesLeadingToFinals = new List<LtsState>(stateDict[StateType.CleanFinal]);
                var intermediateStates = new List<LtsState>(stateDict[StateType.CleanFinal]);
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

                stateDict[StateType.NoWayToFinalMarking] = graph.ConstraintStates
                    .Except(statesLeadingToFinals)
                    .ToList();
            }

            static void FillSoundIntermediate(ConstraintGraph graph, Dictionary<StateType, List<LtsState>> stateDict)
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
