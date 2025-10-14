using CsvHelper.Configuration;

namespace DPN.Experiments.Common.CsvClassMaps
{
    public class VerificationOutputClassMap : ClassMap<MainVerificationInfo>
    {
        public VerificationOutputClassMap()
        {
            Map(x => x.Places).Index(0).Name("PlacesCount");
            Map(x => x.Transitions).Index(1).Name("TransitionsCount");
            Map(x => x.Arcs).Index(2).Name("ArcsCount");
            Map(x => x.Variables).Index(3).Name("VarsCount");
            Map(x => x.Conditions).Index(4).Name("ConditionsCount");
            Map(x => x.Boundedness).Index(5).Name("Boundedness");
            Map(x => x.StateSpaceNodes).Index(6).Name("StateSpaceNodes");
            Map(x => x.StateSpaceArcs).Index(7).Name("StateSpaceArcs");
            Map(x => x.CgStates).Index(8).Name("ConstraintStates");
            Map(x => x.CgArcs).Index(9).Name("ConstraintArcs");
            Map(x => x.DeadTransitions).Index(12).Name("DeadTransitions");
            Map(x => x.Deadlocks).Index(13).Name("Deadlocks");
            Map(x => x.Soundness).Index(14).Name("Soundness");
            Map(x => x.VerificationTime).Index(15).Name("VerificationTime");
            Map(x => x.RepairSuccess).Index(16).Name("RepairSuccess");
            Map(x => x.RepairTime).Index(17).Name("RepairTime");

            Map(x => x.SatisfiesCounditions).Ignore();
        }
    }
}
