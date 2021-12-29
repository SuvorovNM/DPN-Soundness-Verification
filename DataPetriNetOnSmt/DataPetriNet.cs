
using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataPetriNetOnSmt
{
    public class DataPetriNet
    {
        private readonly Random randomGenerator;
        private readonly Context context;

        public List<Place> Places { get; set; }
        public List<Transition> Transitions { get; set; }
        public List<Arc> Arcs { get; set; }
        public VariablesStore Variables { get; set; }

        public DataPetriNet()
        {
            randomGenerator = new Random();
            context = new Context();

            Places = new List<Place>();
            Transitions = new List<Transition>();
            Arcs = new List<Arc>();
        }

        public bool MakeStep()
        {
            var canMakeStep = false; // TODO: Find a more quicker way to get random elements?
            foreach (var transition in Transitions)//.OrderBy(x => randomGenerator.Next())
            {
                canMakeStep = transition.TryFire(Variables, Arcs, context);
                if (canMakeStep)
                {
                    return canMakeStep;
                }
            }

            return canMakeStep;
        }       
    }
}
