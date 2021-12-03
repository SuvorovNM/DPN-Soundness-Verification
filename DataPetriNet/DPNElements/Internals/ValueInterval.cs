using DataPetriNet.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.DPNElements.Internals
{
    public interface IValueInteval
    {

    }
    public class ValueInterval<T> : IValueInteval
        where T : IComparable<T>, IEquatable<T>
    {
        public IntervalPoint<T> Start { get; set; }
        public IntervalPoint<T> End { get; set; }
        public IntervalPoint<T> ForbiddenValue { get; set; }
    }
}
