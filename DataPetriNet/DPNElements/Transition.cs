using DataPetriNet.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.DPNElements
{
    public class Transition : Node
    {
        public Guard Guard { get; set; }

        public bool TryFire(VariablesStore variables, List<Place> preSet, List<Place> postSet)
        {
            var canFire = preSet.All(x => x.Tokens > 0) && Guard.Verify(variables);
            if (canFire)
            {
                Fire(variables, preSet, postSet);
            }

            return canFire;
        }
        private void Fire(VariablesStore variables, List<Place> preSet, List<Place> postSet)
        {
            Guard.UpdateVariables(variables);
            preSet.ForEach(x => x.Tokens--);
            postSet.ForEach(x => x.Tokens++);
        }
    }
}
