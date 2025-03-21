﻿using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.Extensions;
using Microsoft.Z3;

namespace DataPetriNetOnSmt.DPNElements
{
    public class Transition : Node, ICloneable
    {
        public Guard Guard { get; set; }
        public bool IsSplitted { get; set; }
        public string BaseTransitionId { get; set; }
        public Transition(Guard guard)
        {
            Guard = guard;
            IsSplitted = false;
            BaseTransitionId = Id;// null;
        }
        public Transition(string id, Guard guard, string? baseTransitionId = null)
        {
            Guard = guard;
            Label = id;
            Id = id;
            IsSplitted = baseTransitionId != null;
            BaseTransitionId = baseTransitionId ?? id;
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
                    Id + "+[" + secondTransitionId+"]",
                    Guard.MakeRefined(Guard, positiveConstraint), BaseTransitionId);

                var negativeTransition = new Transition(
                    Id + "-[" + secondTransitionId+"]",
                    Guard.MakeRefined(Guard, negativeConstraint), BaseTransitionId);

                return (positiveTransition, negativeTransition);
            }
        }

        public Marking FireOnGivenMarking(Marking tokens, IEnumerable<Arc> arcs)
        {
            var updatedMarking = new Marking(tokens);
            var arcsDict = arcs.ToDictionary(x => (x.Source, x.Destination), y => y.Weight);

            var presetPlaces = arcsDict.Where(x => x.Key.Destination == this).Select(x => (Place)x.Key.Source).ToList();
            var postsetPlaces = arcsDict.Where(x => x.Key.Source == this).Select(x => (Place)x.Key.Destination).ToList();

            foreach (var presetPlace in presetPlaces)
            {
                if (updatedMarking[presetPlace] < arcsDict[(presetPlace, this)])
                {
                    throw new ArgumentException("Transition cannot fire on given marking!");
                }

                if (updatedMarking[presetPlace] != int.MaxValue)
                {
                    updatedMarking[presetPlace] -= arcsDict[(presetPlace, this)];
                }
            }
            foreach (var postsetPlace in postsetPlaces)
            {
                if (updatedMarking[postsetPlace] != int.MaxValue)
                {
                    updatedMarking[postsetPlace] += arcsDict[(this, postsetPlace)];
                }
            }

            return updatedMarking;
        }

        public object Clone()
        {
            return new Transition(Id, (Guard)Guard.Clone(), string.IsNullOrEmpty(BaseTransitionId) ? null : BaseTransitionId) { Label = this.Label};
        }
    }
}
