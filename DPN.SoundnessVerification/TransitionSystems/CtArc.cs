namespace DPN.SoundnessVerification.TransitionSystems
{
    public class CtArc : AbstractArc<CtState, CtTransition>
    {
        public CtArc(CtState sourceState, CtTransition transitionToFire, CtState targetState) 
            : base(sourceState, transitionToFire, targetState)
        {
        }
    }
}
