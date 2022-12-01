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
            // NEEDS FIXING!
            Map(x => x.Places).Index(0).Name("PlacesCount");
            Map(x => x.Transitions).Index(1).Name("TransitionsCount");
            Map(x => x.Arcs).Index(2).Name("ArcsCount");
            Map(x => x.Variables).Index(3).Name("VarsCount");
            Map(x => x.Conditions).Index(4).Name("ConditionsCount");
            Map(x => x.Boundedness).Index(5).Name("Boundedness");
            Map(x => x.LtsStates).Index(6).Name("LtsStates");
            Map(x => x.LtsArcs).Index(7).Name("LtsArcs");
            Map(x => x.CgStates).Index(8).Name("ConstraintStates");
            Map(x => x.CgArcs).Index(9).Name("ConstraintArcs");
            Map(x => x.CgRefStates).Index(10).Name("ConstraintRefStates");
            Map(x => x.CgRefArcs).Index(11).Name("ConstraintRefArcs");
            Map(x => x.DeadTransitions).Index(12).Name("DeadTransitions");
            Map(x => x.Deadlocks).Index(13).Name("Deadlocks");
            Map(x => x.Soundness).Index(14).Name("Soundness");
            Map(x => x.VerificationTime).Index(15).Name("VerificationTime");
            Map(x => x.LtsTime).Index(16).Name("LtsTime");
            Map(x => x.CgTime).Index(17).Name("CgTime");
            Map(x => x.CgRefTime).Index(18).Name("CgRefTime");

            Map(x => x.SatisfiesCounditions).Ignore();
            //Map(x => x.VerificationType).Ignore();
        }
    }
}
