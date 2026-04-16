using CsvHelper.Configuration;

namespace DPN.Experiments.Common.CsvClassMaps
{
    public sealed class VerificationOutputClassMap : ClassMap<MainVerificationInfo>
    {
        public VerificationOutputClassMap()
        {
	        Map(x => x.Id).Index(0).Name("Id");
	        Map(x => x.Places).Index(1).Name("PlacesCount");
	        Map(x => x.Transitions).Index(2).Name("TransitionsCount");
	        Map(x => x.Arcs).Index(3).Name("ArcsCount");
	        Map(x => x.Variables).Index(4).Name("VarsCount");
	        Map(x => x.Conditions).Index(5).Name("ConditionsCount");
	        
	        Map(x => x.Boundedness).Index(6).Name("Boundedness");
	        Map(x => x.Soundness).Index(7).Name("Soundness");
	        Map(x => x.DeadTransitions).Index(8).Name("DeadTransitions");
	        Map(x => x.Deadlocks).Index(9).Name("Deadlocks");
	        
	        Map(x => x.VerificationTime).Index(10).Name("VerificationTime");
	        Map(x => x.VerificationStatesConsidered).Index(11).Name("VerificationStatesConsidered");
	        Map(x => x.VerificationRefinementsCount).Index(12).Name("VerificationRefinementsCount");
	        Map(x => x.StateSpaceNodes).Index(13).Name("StateSpaceNodes");
	        Map(x => x.StateSpaceArcs).Index(14).Name("StateSpaceArcs");
	        
	        Map(x => x.RepairTime).Index(15).Name("RepairTime");
	        Map(x => x.RepairSuccess).Index(16).Name("RepairSuccess");
	        Map(x => x.RepairSteps).Index(17).Name("RepairSteps");
	        Map(x => x.RepairStatesConsidered).Index(18).Name("RepairStatesConsidered");
	        Map(x => x.RepairRefinementsCount).Index(19).Name("RepairRefinementsCount");
	        Map(x => x.EnhancedTransitions).Index(20).Name("EnhancedTransitions");
	        Map(x => x.EnhancedButRolledBackTransitions).Index(21).Name("EnhancedButRolledBackTransitions");
	        
            Map(x => x.SatisfiesCounditions).Ignore();
        }
    }
}
