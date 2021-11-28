﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.DPNElements.Internals
{
    public class ValueInterval<T>
        where T : IComparable<T>
    {
        public IntervalPoint<T> Start { get; set; }
        public IntervalPoint<T> End { get; set; }
        public IntervalPoint<T> ForbiddenValue { get; set; }
    }
}
