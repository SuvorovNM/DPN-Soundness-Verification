using DataPetriNet.DPNElements;
using System.Collections.Generic;

namespace DataPetriNet
{
    public class DataPetriNet // TODO: Add Randomness
    {
        public List<Place> Places { get; set; }
        public List<Transition> Transitions { get; set; }
        public VariablesStore Variables { get; set; }

        public bool MakeStep()
        {
            var canMakeStep = false;
            foreach (var transition in Transitions)
            {
                canMakeStep = transition.TryFire(Variables);
                if (canMakeStep)
                {
                    return canMakeStep;
                }
            }

            return canMakeStep;
        }
    }
}
