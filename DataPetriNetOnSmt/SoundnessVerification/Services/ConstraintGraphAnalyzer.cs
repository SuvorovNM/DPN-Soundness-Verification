using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;

namespace DataPetriNetOnSmt.SoundnessVerification.Services
{
    public static class ConstraintGraphAnalyzer
    {
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
                /*stateDict[StateType.UncleanFinal] = graph.ConstraintStates
                                .Where(x => x.PlaceTokens.Keys.Except(terminalNodes).Any(y => x.PlaceTokens[y] > 0)
                                    && x.PlaceTokens.Keys.Intersect(terminalNodes).Any(y => x.PlaceTokens[y] > 0))
                                .ToList();*/
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
