using DataPetriNetOnSmt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.SoundnessVerification.Services
{
    public interface ITransformation
    {
        public (DataPetriNet dpn, Dictionary<string, long> timers) Transform(DataPetriNet sourceDpn);
    }
}
