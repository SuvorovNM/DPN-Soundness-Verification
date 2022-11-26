using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.Extensions;
using Microsoft.Z3;

namespace DataPetriNetOnSmt.DPNElements
{
    public class Transition : Node, ICloneable
    {
        public Guard Guard { get; set; }
        public bool IsSplitted { get; set; }
        public string? BaseTransitionId { get; set; }
        public Transition(Guard guard)
        {
            Guard = guard;
            IsSplitted = false;
            BaseTransitionId = Id;// null;
        }
        public Transition(string label, Guard guard, string? baseTransitionId = null)
        {
            Guard = guard;
            Label = label;
            Id = label;
            IsSplitted = baseTransitionId != null;
            BaseTransitionId = baseTransitionId;
        }

        public (Transition? positive, Transition? negative) Split(BoolExpr formulaToConjunct, string secondTransitionId) // Context context,
        {
            var positiveConstraint = Guard.Context.MkAnd(
                                Guard.ActualConstraintExpression,
                                formulaToConjunct);

            var negativeConstraint = Guard.Context.MkAnd(
                Guard.ActualConstraintExpression,
                Guard.Context.MkNot(formulaToConjunct));

            var isPositiveSatisfiable = Guard.Context.CanBeSatisfied(positiveConstraint);
            var isNegativeSatisfiable = Guard.Context.CanBeSatisfied(negativeConstraint);

            if (!isPositiveSatisfiable || !isNegativeSatisfiable)
            {
                return (null, null);
            }
            else
            {
                var positiveTransition = new Transition(
                    Id + "+" + secondTransitionId,
                    new Guard(Guard.Context, Guard.BaseConstraintExpressions, positiveConstraint), BaseTransitionId ?? Id);

                var negativeTransition = new Transition(
                    Id + "-" + secondTransitionId,
                    new Guard(Guard.Context, Guard.BaseConstraintExpressions, negativeConstraint), BaseTransitionId ?? Id);

                return (positiveTransition, negativeTransition);
            }
        }

        public bool TryFire(VariablesStore variables, IEnumerable<Arc> arcs, Context ctx)
        {
            if (arcs is null || !arcs.Any())
            {
                throw new ArgumentNullException(nameof(arcs));
            }

            var arcsDict = arcs.ToDictionary(x => (x.Source, x.Destination), y => y.Weight);

            // Currently only transitions with preset places can fire - need to clarify it.
            var canFire = arcsDict.Where(x => x.Key.Destination == this)
                .All(x => ((Place)x.Key.Source).Tokens >= x.Value) &&
                Guard.Verify(variables, ctx);

            if (canFire)
            {
                Fire(variables, arcsDict);
            }

            return canFire;
        }
        private void Fire(VariablesStore variables, Dictionary<(Node Source, Node Destination), int> arcsDict)
        {
            Guard.UpdateGlobalVariables(variables);

            var presetPlaces = arcsDict.Where(x => x.Key.Destination == this).Select(x => (Place)x.Key.Source).ToList();
            var postsetPlaces = arcsDict.Where(x => x.Key.Source == this).Select(x => (Place)x.Key.Destination).ToList();

            presetPlaces.ForEach(x => x.Tokens -= arcsDict[(x, this)]);
            postsetPlaces.ForEach(x => x.Tokens += arcsDict[(this, x)]);
        }

        public Dictionary<Node, int> FireOnGivenMarking(Dictionary<Node, int> tokens, IEnumerable<Arc> arcs)
        {
            var updatedMarking = new Dictionary<Node, int>(tokens);
            var arcsDict = arcs.ToDictionary(x => (x.Source, x.Destination), y => y.Weight);

            var presetPlaces = arcsDict.Where(x => x.Key.Destination == this).Select(x => (Place)x.Key.Source).ToList();
            var postsetPlaces = arcsDict.Where(x => x.Key.Source == this).Select(x => (Place)x.Key.Destination).ToList();

            foreach (var presetPlace in presetPlaces)
            {
                if (updatedMarking[presetPlace] < arcsDict[(presetPlace, this)])
                {
                    throw new ArgumentException("Transition cannot fire on given marking!");
                }
                updatedMarking[presetPlace] -= arcsDict[(presetPlace, this)];
            }
            foreach (var postsetPlace in postsetPlaces)
            {
                updatedMarking[postsetPlace] += arcsDict[(this, postsetPlace)];
            }

            return updatedMarking;
        }

        public object Clone()
        {
            return new Transition(Label, (Guard)Guard.Clone());
        }
    }
}
