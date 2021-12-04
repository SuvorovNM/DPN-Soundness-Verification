using DataPetriNet.Abstractions;
using DataPetriNet.DPNElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.ConstraintGraph
{
    public class ConstraintState
    {
        private readonly ConstraintExpressionOperationService constraintExpressionOperationService;
        //public int[] Places { get; }
        public Dictionary<Place, int> PlaceTokens { get; }
        public List<IConstraintExpression> Constraints { get; }
        public List<ConstraintArc> OutgoingArcs { get; }

        public ConstraintState()
        {
            PlaceTokens = new Dictionary<Place, int>();
            Constraints = new List<IConstraintExpression>();
            OutgoingArcs = new List<ConstraintArc>();
            constraintExpressionOperationService = new ConstraintExpressionOperationService();
        }

        public ConstraintState(ConstraintState sourceState, Transition firedTransition)
        {
            constraintExpressionOperationService = new ConstraintExpressionOperationService();
            PlaceTokens = new Dictionary<Place, int>(sourceState.PlaceTokens);
            OutgoingArcs = new List<ConstraintArc>();

            foreach(var presetPlace in firedTransition.PreSetPlaces)
            {
                PlaceTokens[presetPlace]--;
            }
            foreach(var postsetPlace in firedTransition.PostSetPlaces)
            {
                PlaceTokens[postsetPlace]++;
            }

            Constraints = constraintExpressionOperationService.ConcatExpressions(sourceState.Constraints, firedTransition.Guard.ConstraintExpressions);
        }
    }
}
