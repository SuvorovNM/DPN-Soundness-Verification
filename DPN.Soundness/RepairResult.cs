using DPN.Models;

namespace DPN.Soundness;

public record RepairResult(
	DataPetriNet Dpn, 
	bool IsSuccess,
	ushort RepairSteps, 
	RepairModifications RepairModifications, 
	TimeSpan RepairTime, 
	int TotalStatesConsidered, 
	int TotalRefinementsDone);

public record RepairModifications(HashSet<string> EnhancedTransitions, HashSet<string> TransitionEnhancedButRolledBack);