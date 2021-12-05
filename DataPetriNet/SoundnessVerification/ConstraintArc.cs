using DataPetriNet.DPNElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.SoundnessVerification
{
    public class ConstraintArc
    {
        public ConstraintTransition Transition { get; set; }
        public ConstraintState SourceState { get; set; }
        public ConstraintState TargetState { get; set; }

        public ConstraintArc(ConstraintState sourceState, ConstraintTransition transitionToFire, ConstraintState targetState)
        {
            SourceState = sourceState;
            TargetState = targetState;
            Transition = transitionToFire;
        }
    }
}
