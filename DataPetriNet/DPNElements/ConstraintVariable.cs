using DataPetriNet.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.DPNElements
{
    public class ConstraintVariable
    {
        public string Name { get; set; }
        public DomainType Domain { get; set; }
        public VariableType VariableType { get; set; }
    }
}
