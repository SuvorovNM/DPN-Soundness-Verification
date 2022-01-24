using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.SoundnessVerification.Services
{
    public static class ConstraintGraphAnalyzer
    {
        public static Dictionary<StateType, List<ConstraintState>> GetStatesDividedByTypes(ConstraintGraph graph, IEnumerable<Place> terminalNodes)
        {
            var stateDict = new Dictionary<StateType, List<ConstraintState>>();

            stateDict[StateType.Initial] = new List<ConstraintState> { graph.InitialState };

            stateDict[StateType.Deadlock] = graph.ConstraintStates
                .Where(x => x.PlaceTokens.Keys.Except(terminalNodes).Any(y => x.PlaceTokens[y] > 0)
                    && !graph.ConstraintArcs.Any(y => y.SourceState == x))
                .ToList();

            stateDict[StateType.UncleanFinal] = graph.ConstraintStates
                .Where(x => x.PlaceTokens.Keys.Except(terminalNodes).Any(y => x.PlaceTokens[y] > 0)
                    && x.PlaceTokens.Keys.Intersect(terminalNodes).Any(y => x.PlaceTokens[y] > 0))
                .ToList();

            stateDict[StateType.CleanFinal] = graph.ConstraintStates
                .Where(x => x.PlaceTokens.Keys.Except(terminalNodes).All(y => x.PlaceTokens[y] == 0)
                    && x.PlaceTokens.Keys.Intersect(terminalNodes).Any(y => x.PlaceTokens[y] > 0))
                .ToList();

            var statesLeadingToFinals = new List<ConstraintState>(stateDict[StateType.CleanFinal]);
            var intermediateStates = new List<ConstraintState>(stateDict[StateType.CleanFinal]);
            var stateIncidenceDict = graph.ConstraintArcs
                .GroupBy(x => x.TargetState)
                .ToDictionary(x => x.Key, y => y.Select(x => x.SourceState).ToList());

            do
            {
                var nextStates = intermediateStates
                    .Where(x=> stateIncidenceDict.ContainsKey(x))
                    .SelectMany(x => stateIncidenceDict[x])
                    .Where(x=> !statesLeadingToFinals.Contains(x))
                    .Distinct();
                statesLeadingToFinals.AddRange(intermediateStates);
                intermediateStates = new List<ConstraintState>(nextStates);
            } while (intermediateStates.Count > 0);

            stateDict[StateType.NoWayToFinalMarking] = graph.ConstraintStates
                .Except(statesLeadingToFinals)
                .ToList();

            stateDict[StateType.SoundIntermediate] = graph.ConstraintStates
                .Except(stateDict[StateType.Initial])
                .Except(stateDict[StateType.Deadlock])
                .Except(stateDict[StateType.UncleanFinal])
                .Except(stateDict[StateType.CleanFinal])
                //.Except(stateDict[StateType.NoWayToFinalMarking]) TODO: Consider removing states with no way to final marking form soundIntermediate
                .ToList();

            return stateDict;
        }
    }
}
