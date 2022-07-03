using DataPetriNetOnSmt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetGeneration
{
    public class DPNGenerator
    {
        private readonly DPNBackboneGenerator backboneGenerator;
        private readonly DPNConditionsGenerator conditionsGenerator;

        public DPNGenerator()
        {
            backboneGenerator = new DPNBackboneGenerator();
            conditionsGenerator = new DPNConditionsGenerator();
        }

        public DataPetriNet Generate(int placesCount, int transitionsCount, int additionalArcsCount, int varsCount, int conditionsCount)
        {
            var dpn = backboneGenerator.GenerateBackbone(placesCount, transitionsCount, additionalArcsCount);
            conditionsGenerator.GenerateConditions(dpn, varsCount, conditionsCount);

            return dpn;
        }
    }
}
