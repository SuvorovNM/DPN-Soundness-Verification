using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetVerificationDomain
{
    [Flags]
    public enum VerificationTypeEnum
    {
        None = 0,
        QeWithoutTransformation = 1,
        QeWithTransformation = 2,
        NsqeWithoutTransformation = 4,
        NsqeWithTransformation = 8,
    }
}
