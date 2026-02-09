using DPN.Models;
using Microsoft.Z3;

namespace DataPetriNetGeneration
{
    public class DPNGenerator(Context context) : IDisposable
    {
        private readonly DPNBackboneGenerator backboneGenerator = new(context);
        private readonly DPNConditionsGenerator conditionsGenerator = new(context);

        public void Dispose()
        {
            conditionsGenerator.Dispose();
        }

        public DataPetriNet Generate(
            int placesCount, 
            int transitionsCount, 
            int additionalResourcePlacesCount,
            int additionalArcsCount, 
            int varsCount, 
            int conditionsCount,
            bool soundnessPreference = false)
        {
            var dpn = backboneGenerator.GenerateBackbone(placesCount, transitionsCount,additionalArcsCount, additionalResourcePlacesCount);
            conditionsGenerator.GenerateConditions(dpn, varsCount, conditionsCount, soundnessPreference);

            return dpn;
        }
    }
}
