using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetVerificationForGivenParameters
{
    public class VerificationInput
    {
        public int NumberOfRecords { get; set; }
        public int PlacesCount { get; set; }
        public int TransitionsCount { get; set; }
        public int ExtraArcsCount { get; set; }
        public int VarsCount { get; set; }
        public int ConditionsCount { get; set; }
        public int Protocol { get; set; }
    }
}
