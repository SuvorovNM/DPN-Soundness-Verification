using DataPetriNet.DPNElements;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataPetriNet
{
    public class DataPetriNet // TODO: Add Randomness
    {
        private Random randomGenerator;
        public List<Place> Places { get; set; }
        public List<Transition> Transitions { get; set; }
        public VariablesStore Variables { get; set; }

        public DataPetriNet()
        {
            randomGenerator = new Random();
            Places = new List<Place>();
            Transitions = new List<Transition>();
        }

        public bool MakeStep()
        {
            var canMakeStep = false; // TODO: Find a more quicker way to get random elements?
            foreach (var transition in Transitions.OrderBy(x => randomGenerator.Next()))
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
