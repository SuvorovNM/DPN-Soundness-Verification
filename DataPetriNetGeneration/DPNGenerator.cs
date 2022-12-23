﻿using DataPetriNetOnSmt;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetGeneration
{
    public class DPNGenerator : IDisposable
    {
        private readonly DPNBackboneGenerator backboneGenerator;
        private readonly DPNConditionsGenerator conditionsGenerator;

        public DPNGenerator(Context context)
        {
            backboneGenerator = new DPNBackboneGenerator(context);
            conditionsGenerator = new DPNConditionsGenerator(context);
        }

        public void Dispose()
        {
            conditionsGenerator.Dispose();
        }

        public DataPetriNet Generate(
            int placesCount, 
            int transitionsCount, 
            int additionalArcsCount, 
            int varsCount, 
            int conditionsCount,
            bool soundnessPreference = false)
        {
            var dpn = backboneGenerator.GenerateBackbone(placesCount, transitionsCount, additionalArcsCount);
            conditionsGenerator.GenerateConditions(dpn, varsCount, conditionsCount, soundnessPreference);

            return dpn;
        }
    }
}
