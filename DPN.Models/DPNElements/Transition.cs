using DPN.Models.Extensions;
using DPN.Models.Abstractions;

namespace DPN.Models.DPNElements
{
    public class Transition : Node, ICloneable
    {
        public Guard Guard { get; set; }
        public bool IsSplit { get; init; }
        public bool IsTau { get; init; }
        public string BaseTransitionId { get; init; }

        public Transition(string id, Guard guard, string? baseTransitionId = null, bool isSplit = false)
        {
            Guard = guard;
            Label = id;
            Id = id;
            IsSplit = isSplit;
            IsTau = id.StartsWith("τ");
            BaseTransitionId = baseTransitionId ?? id;
        }

        public Transition? MakeTau()
        {
            var readExpression = Guard.Context.GetExistsExpression(Guard.ActualConstraintExpression, Guard.WriteVars);
            var negatedExpression = Guard.Context.MkNot(readExpression);

            if (readExpression is { IsTrue: false, IsFalse: false } && Guard.Context.CanBeSatisfied(negatedExpression))
            {
                return new Transition($"τ({Label})", new Guard(Guard.Context, negatedExpression), Id);
            }

            return null;
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
            return new Transition(Id, (Guard)Guard.Clone(), string.IsNullOrEmpty(BaseTransitionId) ? null : BaseTransitionId, IsSplit) { Label = this.Label};
        }
    }
}
