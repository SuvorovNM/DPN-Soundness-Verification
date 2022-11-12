namespace DataPetriNetOnSmt.SoundnessVerification.TransitionSystems
{
    public class ConstraintArc
    {
        public ConstraintTransition Transition { get; set; }
        public ConstraintState SourceState { get; set; }
        public ConstraintState TargetState { get; set; }

        public ConstraintArc(ConstraintState sourceState, ConstraintTransition transitionToFire, ConstraintState targetState)
        {
            SourceState = sourceState;
            TargetState = targetState;
            Transition = transitionToFire;
        }
    }
}
