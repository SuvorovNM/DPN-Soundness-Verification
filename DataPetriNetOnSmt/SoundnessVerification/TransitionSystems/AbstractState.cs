using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.SoundnessVerification.TransitionSystems
{
    public record BaseStateInfo(Marking Marking, BoolExpr Constraints);

    public abstract class AbstractState<TSelf>
        where TSelf : AbstractState<TSelf>
    {
        private static int stateCounter = 0;
        public Marking Marking { get; init; }
        public BoolExpr? Constraints { get; set; }
        public HashSet<TSelf> ParentStates { get; set; } // Check the necessity of this property here
        public int Id { get; }

        public AbstractState(BaseStateInfo stateInfo, TSelf parent)
        {
            Marking = stateInfo.Marking;
            Constraints = stateInfo.Constraints;
            ParentStates = new HashSet<TSelf> { parent };
            ParentStates = ParentStates.Union(parent.ParentStates).ToHashSet();
            Id = Interlocked.Increment(ref stateCounter);
        }

        public AbstractState(Context context)
        {
            Marking = new Marking();
            Constraints = context.MkTrue();
            ParentStates = new HashSet<TSelf>();
            Id = Interlocked.Increment(ref stateCounter);
        }

        public AbstractState()
        {
            Marking = new Marking();
            ParentStates = new HashSet<TSelf>();
            Id = Interlocked.Increment(ref stateCounter);
        }
    }
}
