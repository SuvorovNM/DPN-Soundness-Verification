using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetVeificationTimingEstimator
{
    public class VerificationOutput
    {
        public int PlacesCount { get; set; }
        public int TransitionsCount { get; set; }
        public int ArcsCount { get; set; }
        public int VarsCount { get; set; }
        public int ConditionsCount { get; set; }
        public bool Boundedness { get; set; }
        public int ConstraintStates { get; set;}
        public int ConstraintArcs { get; set;}
        public int DeadTransitions { get; set; }
        public bool Deadlocks { get; set; }
        public bool Soundness { get; set; }
        public long Milliseconds { get; set; }
    }
}
