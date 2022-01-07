using DataPetriNetOnSmt.Abstractions;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.SoundnessVerification
{
    public class ConstraintState
    {
        private static int stateCounter = 0;
        public Dictionary<Node, int> PlaceTokens { get; }
        public BoolExpr Constraints { get; }
        public int Id { get; }

        public ConstraintState()
        {
            PlaceTokens = new Dictionary<Node, int>();
            Constraints = ContextProvider.Context.MkTrue();
            Id = Interlocked.Increment(ref stateCounter);
        }

        public ConstraintState(Dictionary<Node, int> tokens, BoolExpr constraints)
        {
            PlaceTokens = new Dictionary<Node, int>(tokens);
            Constraints = constraints;
            Id = Interlocked.Increment(ref stateCounter);
        }
    }
}
