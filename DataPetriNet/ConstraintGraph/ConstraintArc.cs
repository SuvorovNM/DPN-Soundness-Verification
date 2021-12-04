using DataPetriNet.DPNElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.ConstraintGraph
{
    public class ConstraintArc
    {
        public Transition Transition { get; set; }
        public ConstraintState PreviousState { get; set; }
        public ConstraintState NextState { get; set; }
    }
}
