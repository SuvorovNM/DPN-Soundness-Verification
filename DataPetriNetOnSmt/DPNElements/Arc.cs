using DataPetriNetOnSmt.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.DPNElements
{
    public class Arc
    {
        private const int defaultWeight = 1;

        public Node Source { get; set; }
        public Node Destination { get; set; }
        public int Weight { get; set; }

        public Arc(Node source, Node dest, int weight = defaultWeight)
        {
            Source = source;
            Destination = dest;
            Weight = weight;
        }
    }
}
