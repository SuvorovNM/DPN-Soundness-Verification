using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetVerificationDomain
{
    public struct ConditionsInfo
    {
        public byte? DeadTransitions;
        public bool? Boundedness;
        public bool? Soundness;
    }
}
