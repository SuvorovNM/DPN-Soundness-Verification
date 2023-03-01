using DataPetriNetOnSmt.Abstractions;
using Microsoft.Z3;

namespace DataPetriNetOnSmt.SoundnessVerification.TransitionSystems
{
    public class ConstraintState
    {
        private static int stateCounter = 0;
        public Dictionary<Node, int> PlaceTokens { get; }
        public BoolExpr Constraints { get; set; }
        public HashSet<ConstraintState> ParentStates { get; set; }
        public int Id { get; }
        public bool IsCyclic { get; set; }

        public ConstraintState(Context context)
        {
            PlaceTokens = new Dictionary<Node, int>();
            Constraints = context.MkTrue();
            ParentStates = new HashSet<ConstraintState>();
            Id = Interlocked.Increment(ref stateCounter);
            IsCyclic = false;
        }

        public ConstraintState(Dictionary<Node, int> tokens, BoolExpr constraints, ConstraintState parent)
        {
            PlaceTokens = new Dictionary<Node, int>(tokens);
            Constraints = constraints;
            ParentStates = new HashSet<ConstraintState> { parent };
            ParentStates = ParentStates.Union(parent.ParentStates).ToHashSet();
            Id = Interlocked.Increment(ref stateCounter);
            //PreviousStepStates = new HashSet<ConstraintState>() { parent };
            IsCyclic = false;
        }
    }
}
