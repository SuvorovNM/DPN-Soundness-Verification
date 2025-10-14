using DPN.Models;

namespace DPN.Soundness;

public record RepairResult(DataPetriNet Dpn, bool IsSuccess, uint RepairSteps, TimeSpan RepairTime);