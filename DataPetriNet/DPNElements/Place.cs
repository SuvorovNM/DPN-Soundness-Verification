using DataPetriNet.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.DPNElements
{
    public class Place: Node // TODO: define the necessity of using Ids
    {
        public int Tokens { get; set; }
    }
}
