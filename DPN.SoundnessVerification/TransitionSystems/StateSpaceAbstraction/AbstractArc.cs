namespace DPN.SoundnessVerification.TransitionSystems
{
    public class AbstractArc<TAbsState,TAbsTransition> 
        where TAbsState : AbstractState
        where TAbsTransition : AbstractTransition
    {
        public TAbsTransition Transition { get; set; }
        public TAbsState SourceState { get; set; }
        public TAbsState TargetState { get; set; }

        public AbstractArc(TAbsState sourceState, TAbsTransition transitionToFire, TAbsState targetState)
        {
            SourceState = sourceState;
            TargetState = targetState;
            Transition = transitionToFire;
        }
    }
}
