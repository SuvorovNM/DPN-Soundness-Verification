using DataPetriNetOnSmt.Abstractions;
using Microsoft.Z3;
using System.Collections.Immutable;

namespace DataPetriNetOnSmt.SoundnessVerification.TransitionSystems
{
    public class LtsState : AbstractState<LtsState>
    {
        public bool IsCyclic { get; set; } = false;
        //public HashSet<LtsState> ParentStates { get; set; }

        public LtsState()
        {

        }

        public LtsState(Context context) : base(context)
        {
        }

        public LtsState(BaseStateInfo stateInfo, LtsState parent) : base(stateInfo, parent)
        {
        }
    }
}
