using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using System.Collections.Generic;

namespace ToGraphParser.Extensions
{
    public static class DictionaryExtension
    {
        public static void AddStatesForType(this Dictionary<LtsState, StateType> resultDictionary, KeyValuePair<StateType, List<LtsState>> statesToAdd)
        {
            var stateType = statesToAdd.Key;

            foreach (var state in statesToAdd.Value)
            {
                if (resultDictionary.ContainsKey(state))
                {
                    resultDictionary[state] = stateType;
                }
                else
                {
                    resultDictionary.Add(state, stateType);
                }
            }
        }
    }
}
