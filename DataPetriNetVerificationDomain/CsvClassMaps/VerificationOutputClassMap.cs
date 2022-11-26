using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetVerificationDomain.CsvClassMaps
{
    public class VerificationOutputClassMap : ClassMap<VerificationOutput>
    {
        public VerificationOutputClassMap()
        {
            Map(x => x.Places).Index(0).Name("PlacesCount");
            Map(x => x.Transitions).Index(1).Name("TransitionsCount");
            Map(x => x.Arcs).Index(2).Name("ArcsCount");
            Map(x => x.Variables).Index(3).Name("VarsCount");
            Map(x => x.Conditions).Index(4).Name("ConditionsCount");
            Map(x => x.Boundedness).Index(5).Name("Boundedness");
            Map(x => x.ConstraintStates).Index(6).Name("ConstraintStates");
            Map(x => x.ConstraintArcs).Index(7).Name("ConstraintArcs");
            Map(x => x.DeadTransitions).Index(8).Name("DeadTransitions");
            Map(x => x.Deadlocks).Index(9).Name("Deadlocks");
            Map(x => x.Soundness).Index(10).Name("Soundness");
            Map(x => x.VerificationTime).Index(11).Name("Milliseconds");

            Map(x => x.SatisfiesCounditions).Ignore();
            Map(x => x.VerificationType).Ignore();
        }
    }
}
