using DPN.Models.DPNElements;
using Microsoft.Z3;

namespace DPN.SoundnessVerification.TransitionSystems
{
    public record BaseStateInfo(Marking Marking, BoolExpr Constraints);

    public abstract class AbstractState//<TSelf>
        //where TSelf : AbstractState<TSelf>
    {
        private static int _stateCounter = 0;
        public Marking Marking { get; init; }
        public BoolExpr? Constraints { get; set; }
        public int Id { get; }

        public AbstractState(BaseStateInfo stateInfo)
        {
            Marking = stateInfo.Marking;
            Constraints = stateInfo.Constraints;
            Id = Interlocked.Increment(ref _stateCounter);
        }

        public AbstractState(Context context)
        {
            Marking = new Marking();
            Constraints = context.MkTrue();
            Id = Interlocked.Increment(ref _stateCounter);
        }

        public AbstractState()
        {
            Marking = new Marking();
            Id = Interlocked.Increment(ref _stateCounter);
        }
    }
}
