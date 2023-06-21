using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.SoundnessVerification.TransitionSystems
{
    public class CtArc : AbstractArc<CtState, CtTransition>
    {
        public CtArc(CtState sourceState, CtTransition transitionToFire, CtState targetState) 
            : base(sourceState, transitionToFire, targetState)
        {
        }
    }
}
