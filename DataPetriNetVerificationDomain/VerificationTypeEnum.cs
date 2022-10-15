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
        QeWithTransformation = 1,
        QeWithoutTransformation = 2,
        NsqeWithTransformation = 4,
        NsqeWithoutTransformation = 8,
    }
}
