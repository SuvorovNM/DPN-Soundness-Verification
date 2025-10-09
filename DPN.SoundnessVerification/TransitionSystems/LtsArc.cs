namespace DPN.SoundnessVerification.TransitionSystems
{
    public class LtsArc : AbstractArc<LtsState,LtsTransition>
    {
        public LtsArc(LtsState sourceState, LtsTransition transitionToFire, LtsState targetState)
            : base(sourceState, transitionToFire, targetState)
        {
        }
    }
}
