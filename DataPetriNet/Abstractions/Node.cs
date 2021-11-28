using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.Abstractions
{
    public abstract class Node : ILabeledElement
    {
        public string Label { get; set; }
    }
}
