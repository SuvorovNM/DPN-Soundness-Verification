using DPN.Models;

namespace DPN.Soundness.Repair;

public interface ISoundnessRepairer
{
	RepairResult Repair(DataPetriNet sourceDpn, Dictionary<string, string> repairProperties);
}