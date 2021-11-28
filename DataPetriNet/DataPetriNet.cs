using DataPetriNet.DPNElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet
{
    public class DataPetriNet
    {
        public List<Place> Places { get; set; }
        public List<Transition> Transitions { get; set; }
        public Dictionary<Transition, List<Place>> PreSetDictionary { get; set; }
        public Dictionary<Transition, List<Place>> PostSetDictionary { get; set; }

        public VariablesStore Variables { get; set; }
    }
}
