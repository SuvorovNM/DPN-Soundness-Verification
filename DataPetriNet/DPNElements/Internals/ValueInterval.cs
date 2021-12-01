using DataPetriNet.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.DPNElements.Internals
{
    public class ValueInterval<T>
        where T : IComparable<T>, IEquatable<T>
    {
        public IntervalPoint<DefinableValue<T>> Start { get; set; }
        public IntervalPoint<DefinableValue<T>> End { get; set; }
        public IntervalPoint<DefinableValue<T>> ForbiddenValue { get; set; }
    }
}
