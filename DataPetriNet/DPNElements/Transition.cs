using DataPetriNet.Abstractions;
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
    }
}
