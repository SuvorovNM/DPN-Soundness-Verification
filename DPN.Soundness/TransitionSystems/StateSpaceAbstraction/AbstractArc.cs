namespace DPN.Soundness.TransitionSystems.StateSpaceAbstraction
{
    public class AbstractArc<TAbsState, TAbsTransition>(TAbsState sourceState, TAbsTransition transitionToFire, TAbsState targetState)
	    where TAbsState : AbstractState
	    where TAbsTransition : AbstractTransition
    {
        public TAbsTransition Transition { get; set; } = transitionToFire;
        public TAbsState SourceState { get; set; } = sourceState;
        public TAbsState TargetState { get; set; } = targetState;
    }
}
