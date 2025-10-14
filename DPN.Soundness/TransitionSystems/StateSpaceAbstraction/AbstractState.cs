using DPN.Models.DPNElements;
using Microsoft.Z3;

namespace DPN.Soundness.TransitionSystems.StateSpaceAbstraction
{
	internal record BaseStateInfo(Marking Marking, BoolExpr Constraints);

	internal class AbstractState
    {
        private static int _stateCounter = 0;
        public Marking Marking { get; init; }
        public BoolExpr? Constraints { get; set; }
        public int Id { get; }

        protected AbstractState(BaseStateInfo stateInfo)
        {
            Marking = stateInfo.Marking;
            Constraints = stateInfo.Constraints;
            Id = Interlocked.Increment(ref _stateCounter);
        }

        protected AbstractState(Context context)
        {
            Marking = new Marking();
            Constraints = context.MkTrue();
            Id = Interlocked.Increment(ref _stateCounter);
        }

        protected AbstractState()
        {
            Marking = new Marking();
            Id = Interlocked.Increment(ref _stateCounter);
        }
    }
}
