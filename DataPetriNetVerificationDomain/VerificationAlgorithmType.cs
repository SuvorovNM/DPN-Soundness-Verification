using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetVerificationDomain
{
    [Flags]
    public enum VerificationAlgorithmTypeEnum
    {
        DirectVersion = 2,
        ImprovedVersion = 4
    }
}
