using DataPetriNet.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.DPNElements.Internals
{
    public struct IntervalPoint<T>
        where T : IEquatable<T>, IComparable<T>
    {
        public DefinableValue<T> Value { get; set; }
        public bool HasValue { get; set; }

        public IntervalPoint(DefinableValue<T> value)
        {
            Value = value;
            HasValue = true;
        }

        public override bool Equals(Object obj)
        {
            return obj is IntervalPoint<T> c && this == c;
        }
        public bool Equals(IntervalPoint<T> other)
        {
            return HasValue == other.HasValue &&
                (!HasValue || Value == other.Value);
        }
        public override int GetHashCode()
        {
            return HasValue.GetHashCode() ^ (HasValue ? Value.GetHashCode() : 0);
        }

        public static bool operator ==(IntervalPoint<T> x, IntervalPoint<T> y)
        {
            return x.HasValue == y.HasValue &&
                (!x.HasValue || x.Equals(y.Value));
        }
        public static bool operator !=(IntervalPoint<T> x, IntervalPoint<T> y)
        {
            return !(x == y);
        }
    }
}
