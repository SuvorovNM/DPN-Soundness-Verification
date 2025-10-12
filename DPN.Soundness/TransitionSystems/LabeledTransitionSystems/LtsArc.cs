using DPN.Soundness.TransitionSystems.StateSpaceAbstraction;

namespace DPN.Soundness.TransitionSystems.LabeledTransitionSystems
{
    public class LtsArc : AbstractArc<LtsState,LtsTransition>
    {
        public LtsArc(LtsState sourceState, LtsTransition transitionToFire, LtsState targetState)
            : base(sourceState, transitionToFire, targetState)
        {
        }
    }
}
