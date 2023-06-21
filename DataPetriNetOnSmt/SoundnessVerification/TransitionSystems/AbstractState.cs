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

    public abstract class AbstractState//<TSelf>
        //where TSelf : AbstractState<TSelf>
    {
        private static int stateCounter = 0;
        public Marking Marking { get; init; }
        public BoolExpr? Constraints { get; set; }
        public int Id { get; }

        public AbstractState(BaseStateInfo stateInfo)
        {
            Marking = stateInfo.Marking;
            Constraints = stateInfo.Constraints;
            Id = Interlocked.Increment(ref stateCounter);
        }

        public AbstractState(Context context)
        {
            Marking = new Marking();
            Constraints = context.MkTrue();
            Id = Interlocked.Increment(ref stateCounter);
        }

        public AbstractState()
        {
            Marking = new Marking();
            Id = Interlocked.Increment(ref stateCounter);
        }
    }
}
