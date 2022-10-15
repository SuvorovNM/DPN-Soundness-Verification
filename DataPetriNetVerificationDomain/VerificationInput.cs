using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetVerificationDomain
{
    public struct VerificationInput
    {
        public DpnToGenerateInfo DpnInfo;
        public VerificationTypeEnum VerificationType;
        public ConditionsInfo ConditionsInfo;
        public IterationsInfo IterationsInfo;
        public string OutputDirectory;
    }
}
