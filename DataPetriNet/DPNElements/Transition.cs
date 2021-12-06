using DataPetriNet.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataPetriNet.DPNElements
{
    public class Transition : Node
    {
        public Guard Guard { get; set; }
        public List<Place> PreSetPlaces { get; set; }
        public List<Place> PostSetPlaces { get; set; }

        public bool TryFire(VariablesStore variables)
        {
            // Currently only transitions with preset places can fire - need to clarify it.
            var canFire = PreSetPlaces.Any() && PreSetPlaces.All(x => x.Tokens > 0) && Guard.Verify(variables);
            if (canFire)
            {
                Fire(variables);
            }

            return canFire;
        }
        private void Fire(VariablesStore variables)
        {
            Guard.UpdateGlobalVariables(variables);
            PreSetPlaces.ForEach(x => x.Tokens--);
            PostSetPlaces.ForEach(x => x.Tokens++);
        }

        public Dictionary<Node, int> FireOnGivenMarking(Dictionary<Node, int> tokens)
        {
            var updatedMarking = new Dictionary<Node, int>(tokens);

            foreach (var presetPlace in PreSetPlaces)
            {
                if (updatedMarking[presetPlace] <= 0)
                {
                    throw new ArgumentException("Transition cannot fire on given marking!");
                }
                updatedMarking[presetPlace]--;
            }
            foreach (var postsetPlace in PostSetPlaces)
            {
                updatedMarking[postsetPlace]++;
            }

            return updatedMarking;
        }
    }
}
