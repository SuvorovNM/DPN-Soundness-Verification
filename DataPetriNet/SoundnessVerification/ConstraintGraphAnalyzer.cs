using DataPetriNet.DPNElements;
using DataPetriNet.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.SoundnessVerification
{
    public class ConstraintGraphAnalyzer
    {
        public Dictionary<StateType, List<ConstraintState>> GetStatesDividedByTypes(ConstraintGraph graph, IEnumerable<Place> terminalNodes)
        {
            var stateDict = new Dictionary<StateType, List<ConstraintState>>();

            stateDict[StateType.Initial] = new List<ConstraintState> { graph.InitialState };

            stateDict[StateType.Deadlock] = graph.ConstraintStates
                .Where(x => x.PlaceTokens.Keys.Except(terminalNodes).Any(y => x.PlaceTokens[y] > 0) 
                    && !graph.ConstraintArcs.Any(y=>y.SourceState == x))
                .ToList();

            stateDict[StateType.UncleanFinal] = graph.ConstraintStates
                .Where(x => x.PlaceTokens.Keys.Except(terminalNodes).Any(y => x.PlaceTokens[y] > 0)
                    && x.PlaceTokens.Keys.Intersect(terminalNodes).Any(y => x.PlaceTokens[y] > 0))
                .ToList();

            stateDict[StateType.CleanFinal] = graph.ConstraintStates
                .Where(x => x.PlaceTokens.Keys.Except(terminalNodes).All(y => x.PlaceTokens[y] == 0)
                    && x.PlaceTokens.Keys.Intersect(terminalNodes).Any(y=>x.PlaceTokens[y] > 0))
                .ToList();

            stateDict[StateType.SoundIntermediate] = graph.ConstraintStates
                .Except(stateDict[StateType.Initial])
                .Except(stateDict[StateType.Deadlock])
                .Except(stateDict[StateType.UncleanFinal])
                .Except(stateDict[StateType.CleanFinal])
                .ToList();

            return stateDict;
        }
    }
}
