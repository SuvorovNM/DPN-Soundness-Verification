using DataPetriNetOnSmt.Abstractions;
using Microsoft.Z3;
using System.Collections.Immutable;

namespace DataPetriNetOnSmt.SoundnessVerification.TransitionSystems
{
    public class LtsState : AbstractState
    {
        public bool IsCyclic { get; set; } = false;
        public HashSet<LtsState> ParentStates { get; set; }

        public LtsState()
        {
            ParentStates= new HashSet<LtsState>();
        }

        public LtsState(Context context) : base(context)
        {
            ParentStates= new HashSet<LtsState>();
        }

        public LtsState(BaseStateInfo stateInfo, LtsState parent) : base(stateInfo)
        {
            ParentStates = new HashSet<LtsState> { parent };
            ParentStates = ParentStates.Union(parent.ParentStates).ToHashSet();
        }
    }
}
