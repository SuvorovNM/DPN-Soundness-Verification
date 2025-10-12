using DPN.Soundness.TransitionSystems.StateSpaceAbstraction;

namespace DPN.Soundness.TransitionSystems.CoverabilityTree
{
    public class CtArc : AbstractArc<CtState, CtTransition>
    {
        public CtArc(CtState sourceState, CtTransition transitionToFire, CtState targetState) 
            : base(sourceState, transitionToFire, targetState)
        {
        }
    }
}
