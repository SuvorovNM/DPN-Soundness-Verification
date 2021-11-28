using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.DPNElements.Internals
{
    public struct IntervalPoint<T>
    {
        public T Value { get; set; }
        public bool HasValue { get; set; }

        public IntervalPoint(T value)
        {
            Value = value;
            HasValue = true;
        }
    }
}
