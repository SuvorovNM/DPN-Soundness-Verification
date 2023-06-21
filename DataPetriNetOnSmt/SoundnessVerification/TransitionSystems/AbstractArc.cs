using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.SoundnessVerification.TransitionSystems
{
    public abstract class AbstractArc<AbsState,AbsTransition> 
        where AbsState : AbstractState
        where AbsTransition : AbstractTransition
    {
        public AbsTransition Transition { get; set; }
        public AbsState SourceState { get; set; }
        public AbsState TargetState { get; set; }

        public AbstractArc(AbsState sourceState, AbsTransition transitionToFire, AbsState targetState)
        {
            SourceState = sourceState;
            TargetState = targetState;
            Transition = transitionToFire;
        }
    }
}
