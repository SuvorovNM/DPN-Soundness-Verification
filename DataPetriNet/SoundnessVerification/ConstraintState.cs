using DataPetriNet.Abstractions;
using DataPetriNet.DPNElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataPetriNet.SoundnessVerification
{
    public class ConstraintState
    {
        private static int stateCounter = 0;
        public Dictionary<Node, int> PlaceTokens { get; }
        public List<IConstraintExpression> Constraints { get; }
        public int Id { get; }

        public ConstraintState()
        {
            PlaceTokens = new Dictionary<Node, int>();
            Constraints = new List<IConstraintExpression>();
            Id = Interlocked.Increment(ref stateCounter);
        }

        public ConstraintState(Dictionary<Node, int> tokens, List<IConstraintExpression> constraints)
        {
            PlaceTokens = new Dictionary<Node, int>(tokens);
            Constraints = new List<IConstraintExpression>(constraints);
            Id = Interlocked.Increment(ref stateCounter);
        }
    }
}
