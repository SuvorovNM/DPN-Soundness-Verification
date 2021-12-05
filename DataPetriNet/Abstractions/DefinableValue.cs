using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DataPetriNet.Abstractions
{
    public interface IDefinableValue
    {
        bool IsDefined { get; }
    }
    public class DefinableValue<T> : IDefinableValue, IEquatable<DefinableValue<T>>
        where T : IEquatable<T>, IComparable<T>
    {
        public DefinableValue()
        {

        }
        public DefinableValue(T value)
        {
            Value = value;
        }

        public T Value
        {
            get
            {
                if (IsDefined)
                    return definableValue;
                else
                    throw new ArgumentNullException(nameof(Value));
            }
            set
            {
                definableValue = value;
                IsDefined = true;
            }
        }
        private T definableValue;

        public bool IsDefined { get; private set; }

        public override bool Equals(Object obj)
        {
            return obj is DefinableValue<T> c && this == c;
        }
        public bool Equals(DefinableValue<T> other)
        {
            return IsDefined == other.IsDefined &&
                (!IsDefined || Value.Equals(other.Value));
        }
        public override int GetHashCode()
        {
            return IsDefined.GetHashCode() ^ (IsDefined ? Value.GetHashCode() : 0);
        }

        public static bool operator ==(DefinableValue<T> x, DefinableValue<T> y)
        {
            return x.IsDefined == y.IsDefined &&
                (!x.IsDefined || x.Equals(y.Value));
        }
        public static bool operator !=(DefinableValue<T> x, DefinableValue<T> y)
        {
            return !(x == y);
        }
    }
}
