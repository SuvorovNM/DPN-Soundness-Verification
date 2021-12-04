using DataPetriNet.DPNElements;
using System.Collections.Generic;

namespace DataPetriNet
{
    public class DataPetriNet // TODO: Add Randomness
    {
        public List<Place> Places { get; set; }
        public List<Transition> Transitions { get; set; }
        public Dictionary<Transition, List<Place>> PreSetDictionary { get; set; } // TODO: Maybe put in Transitions?
        public Dictionary<Transition, List<Place>> PostSetDictionary { get; set; }

        public VariablesStore Variables { get; set; }

        public bool MakeStep()
        {
            var canMakeStep = false;
            foreach (var transition in PreSetDictionary.Keys)
            {
                canMakeStep = transition.TryFire(Variables, PreSetDictionary[transition], PostSetDictionary[transition]);
                if (canMakeStep)
                {
                    return canMakeStep;
                }
            }

            return canMakeStep;
        }
    }
}
