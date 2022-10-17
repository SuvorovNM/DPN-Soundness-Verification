using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetVerificationDomain
{
    public struct DpnToGenerateInfo
    {
        public double Places = 0;
        public double Transitions = 0;
        public double ExtraArcs = 0;
        public double Variables = 0;
        public double Conditions = 0;

        public DpnToGenerateInfo()
        {

        }
    }
}
