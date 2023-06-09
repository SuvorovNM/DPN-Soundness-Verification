namespace DataPetriNetOnSmt.SoundnessVerification.TransitionSystems
{
    public class LtsArc : AbstractArc<LtsState,LtsTransition>
    {
        public bool IsVisited { get; set; }

        public LtsArc(LtsState sourceState, LtsTransition transitionToFire, LtsState targetState)
            : base(sourceState, transitionToFire, targetState)
        {
        }
    }
}
