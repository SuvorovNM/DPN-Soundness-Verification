using DPN.Soundness.TransitionSystems.StateSpaceAbstraction;

namespace DPN.Soundness.TransitionSystems.Reachability
{
	internal class LtsArc(LtsState sourceState, LtsTransition transitionToFire, LtsState targetState) : AbstractArc<LtsState, LtsTransition>(sourceState, transitionToFire, targetState);
}
