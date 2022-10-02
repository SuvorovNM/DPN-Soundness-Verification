using DataPetriNetOnSmt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetTransformation
{
    public interface ITransformation
    {
        public DataPetriNet Transform(DataPetriNet sourceDpn);
    }
}
