using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;

namespace DataPetriNetOnSmt.SoundnessVerification.Services
{
    public record SoundnessProperties(
        Dictionary<AbstractState, ConstraintStateType> StateTypes,
        bool Boundedness,
        string[] DeadTransitions,
        bool Deadlocks,
        bool Soundness);

    public static class LtsAnalyzer
    {
        public static SoundnessProperties CheckSoundness(DataPetriNet dpn, CoverabilityTree ct)
        {
            var bounded = ct.ConstraintStates.All(x => x.StateType != CtStateType.StrictlyCovered);

            Dictionary<AbstractState, ConstraintStateType> stateTypes = 
                GetStatesDividedByTypesNew(ct, dpn.Places.Where(x => x.IsFinal).ToArray());
            foreach (var state in ct.ConstraintStates)
            {
                if (state.StateType == CtStateType.StrictlyCovered)
                {
                    stateTypes[state] = ConstraintStateType.StrictlyCovered;
                }
            }


            var deadTransitions = dpn.Transitions
                .Select(x => x.BaseTransitionId)
                .Except(ct.ConstraintArcs.Select(y => y.Transition.NonRefinedTransitionId))
                .ToArray();

            var hasDeadlocks = false;
            var isFinalMarkingAlwaysReachable = true;
            var isFinalMarkingClean = true;

            foreach (var constraintState in ct.ConstraintStates)
            {
                hasDeadlocks |= stateTypes[constraintState].HasFlag(ConstraintStateType.Deadlock);
                isFinalMarkingAlwaysReachable &= !stateTypes[constraintState].HasFlag(ConstraintStateType.NoWayToFinalMarking);
                isFinalMarkingClean &= !stateTypes[constraintState].HasFlag(ConstraintStateType.UncleanFinal);
            }

            var isSound = bounded
                && !hasDeadlocks
                && isFinalMarkingAlwaysReachable
                && isFinalMarkingClean
                && deadTransitions.Length == 0;

            return new SoundnessProperties(stateTypes, bounded, deadTransitions, hasDeadlocks, isSound);
        }

        public static SoundnessProperties CheckLazySoundness(DataPetriNet dpn, CoverabilityGraph cg)
        {
            var stateTypes = GetStatesDividedByTypesNew(cg, dpn.Places.Where(x => x.IsFinal).ToArray());
            
            var hasDeadlocks = false;
            var isFinalMarkingAlwaysReachable = true;
            var isFinalMarkingClean = true;

            foreach (var constraintState in cg.ConstraintStates)
            {
                hasDeadlocks |= stateTypes[constraintState].HasFlag(ConstraintStateType.Deadlock);
                isFinalMarkingAlwaysReachable &= !stateTypes[constraintState].HasFlag(ConstraintStateType.NoWayToFinalMarking);
                isFinalMarkingClean &= !stateTypes[constraintState].HasFlag(ConstraintStateType.UncleanFinal);
            }

            var isSound = isFinalMarkingAlwaysReachable && isFinalMarkingClean;
            
            return new SoundnessProperties(stateTypes, cg.IsFullGraph, GetDeadTransitions(dpn, cg), hasDeadlocks, isSound);
        }

        public static SoundnessProperties CheckSoundness
            (DataPetriNet dpn, LabeledTransitionSystem cg)
        {
            Dictionary<AbstractState, ConstraintStateType> stateTypes;

            var boundedness = cg.IsFullGraph;
            if (boundedness)
            {
                stateTypes = GetStatesDividedByTypesNew(cg, dpn.Places.Where(x => x.IsFinal).ToArray());
            }
            else
            {
                stateTypes = cg.ConstraintStates.ToDictionary(x => (AbstractState)x, y => ConstraintStateType.Default);
            }

            var deadTransitions = GetDeadTransitions(dpn, cg);

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
                && deadTransitions.Length == 0;

            return new SoundnessProperties(stateTypes, cg.IsFullGraph, deadTransitions, hasDeadlocks, isSound);
        }

        private static string[] GetDeadTransitions(DataPetriNet dpn, LabeledTransitionSystem cg)
        {
            var deadTransitions = dpn.Transitions
                .Select(x => x.BaseTransitionId)
                .Except(cg.ConstraintArcs.Select(y => y.Transition.NonRefinedTransitionId))
                .ToArray();
            return deadTransitions;
        }

        public static Dictionary<AbstractState, ConstraintStateType> GetStatesDividedByTypesNew<AbsState, AbsTransition, AbsArc>
            (AbstractStateSpaceStructure<AbsState, AbsTransition, AbsArc> graph, IEnumerable<Place> finalMarking)
            where AbsState : AbstractState, new()
            where AbsTransition : AbstractTransition
            where AbsArc : AbstractArc<AbsState, AbsTransition>
        {
            var stateDictionary = graph.ConstraintStates.ToDictionary(x => (AbstractState)x, y => ConstraintStateType.Default);

            DefineInitialState(stateDictionary);
            DefineDeadlocks(finalMarking, stateDictionary);

            var finalStates = graph.ConstraintStates
                    .Where(x => x.Marking.Keys.Intersect(finalMarking).All(y => x.Marking[y] > 0));

            DefineFinals(stateDictionary, finalStates);
            DefineUncleanFinals(finalMarking, stateDictionary, finalStates);
            DefineStatesWithNoWayToFinals(stateDictionary, finalStates);

            return stateDictionary;



            void DefineDeadlocks(IEnumerable<Place> terminalNodes, Dictionary<AbstractState, ConstraintStateType> stateDictionary)
            {
                graph.ConstraintStates
                                .Where(x => x.Marking.Keys.Except(terminalNodes).Any(y => x.Marking[y] > 0)
                                    && !graph.ConstraintArcs.Any(y => y.SourceState == x)
                                    && !(x is CtState state && state.StateType != CtStateType.NonCovered))
                                .ToList()
                                .ForEach(x => stateDictionary[x] = stateDictionary[x] | ConstraintStateType.Deadlock);
            }

            // Доработать
            void DefineStatesWithNoWayToFinals(Dictionary<AbstractState, ConstraintStateType> stateDictionary, IEnumerable<AbsState> finalStates)
                
            {
                var statesLeadingToFinals = new List<AbsState>(finalStates);
                var intermediateStates = new List<AbsState>(finalStates);
                var stateIncidenceDict = graph.ConstraintArcs
                    .GroupBy(x => x.TargetState)
                    .ToDictionary(x => x.Key, y => y.Select(x => x.SourceState).ToList());

                //AbsState state = null;

                if (typeof(AbsState) == typeof(CtState))
                {
                    foreach (var state in stateIncidenceDict.Keys)
                    {
                        if (state is CtState ctState && ctState.StateType != CtStateType.NonCovered)
                        {
                            if (stateIncidenceDict.ContainsKey(ctState.CoveredNode as AbsState))
                            {
                                stateIncidenceDict[ctState.CoveredNode as AbsState].Add(ctState as AbsState);
                            }
                        }
                    }
                }

                do//check for covered
                {
                    var nextStates = intermediateStates
                        .Where(x => stateIncidenceDict.ContainsKey(x))
                        .SelectMany(x => stateIncidenceDict[x])
                        .Where(x => !statesLeadingToFinals.Contains(x))
                        .Distinct();
                    statesLeadingToFinals.AddRange(intermediateStates);
                    intermediateStates = new List<AbsState>(nextStates);
                } while (intermediateStates.Count > 0);

                graph.ConstraintStates
                    .Except(statesLeadingToFinals)
                    .ToList()
                    .ForEach(x => stateDictionary[x] = stateDictionary[x] | ConstraintStateType.NoWayToFinalMarking);
            }

            static void DefineUncleanFinals(IEnumerable<Place> terminalNodes, Dictionary<AbstractState, ConstraintStateType> stateDictionary, IEnumerable<AbsState> finalStates)
            {
                finalStates
                                .Where(x => x.Marking.Keys.Any(y => x.Marking[y] > 1) || x.Marking.Keys.Except(terminalNodes).Any(y => x.Marking[y] > 0))
                                .ToList()
                                .ForEach(x => stateDictionary[x] = stateDictionary[x] | ConstraintStateType.UncleanFinal);
            }

            void DefineFinals(Dictionary<AbstractState, ConstraintStateType> stateDictionary, IEnumerable<AbsState> finalStates)
            {
                finalStates
                                .ToList()
                                .ForEach(x => stateDictionary[x] = stateDictionary[x] | ConstraintStateType.Final);
            }

            void DefineInitialState(Dictionary<AbstractState, ConstraintStateType> stateDictionary)
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
