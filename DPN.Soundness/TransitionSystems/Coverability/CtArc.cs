using DPN.Soundness.TransitionSystems.StateSpaceAbstraction;

namespace DPN.Soundness.TransitionSystems.Coverability
{
    public class CtArc(CtState sourceState, CtTransition transitionToFire, CtState targetState) : AbstractArc<CtState, CtTransition>(sourceState, transitionToFire, targetState);
}
