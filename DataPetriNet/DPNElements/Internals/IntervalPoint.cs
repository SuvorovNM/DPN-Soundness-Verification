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
    }
}
