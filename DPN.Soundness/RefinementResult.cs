using DPN.Models;
using DPN.Soundness.TransitionSystems.StateSpace;

namespace DPN.Soundness;

public record RefinementResult(DataPetriNet RefinedDpn, StateSpaceGraph StateSpaceStructure);