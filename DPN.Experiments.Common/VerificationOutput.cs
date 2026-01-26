using DPN.Models;
using DPN.Soundness;
using DPN.Soundness.TransitionSystems.StateSpace;

namespace DPN.Experiments.Common
{
	public class MainVerificationInfo
	{
		public bool SatisfiesCounditions { get; init; }
		public ushort Places { get; init; }
		public ushort Transitions { get; init; }
		public ushort Arcs { get; init; }
		public ushort Variables { get; init; }
		public ushort Conditions { get; init; }
		public bool Boundedness { get; init; }
		public int StateSpaceNodes { get; init; }
		public int StateSpaceArcs { get; init; }
		public ushort DeadTransitions { get; init; }
		public bool Deadlocks { get; init; }
		public bool Soundness { get; init; }
		public string VerificationTime { get; init; }
		public int VerificationStatesConsidered { get; init; }
		public ushort VerificationRefinementsCount { get; init; }
		public string? RepairTime { get; init; }
		public bool? RepairSuccess { get; init; }
		public string Id { get; init; }
		public ushort RepairSteps { get; init; }
		public ushort EnhancedTransitions { get; init; }
		public ushort EnhancedButRolledBackTransitions { get; init; }
		public int RepairStatesConsidered { get; init; }
		public ushort RepairRefinementsCount { get; init; }

		public MainVerificationInfo()
		{
		}

		public MainVerificationInfo(
			DataPetriNet dpn,
			bool satisfiesConditions,
			VerificationResult verificationResult,
			RepairResult? repairResult)
		{
			Places = (ushort)dpn.Places.Count;
			Transitions = (ushort)dpn.Transitions.Count;
			Arcs = (ushort)dpn.Arcs.Count;
			Variables = (ushort)dpn.Variables.GetAllVariables().Length;
			Conditions = (ushort)dpn.Transitions
				.Sum(x => AtomicFormulaCounter.CountAtomicFormulas(x.Guard.ActualConstraintExpression));
			Boundedness = verificationResult.SoundnessProperties.Boundedness;
			StateSpaceNodes = verificationResult.StateSpaceGraph.Nodes.Length;
			StateSpaceArcs = verificationResult.StateSpaceGraph.Arcs.Length;
			DeadTransitions = (ushort)verificationResult.SoundnessProperties.DeadTransitions.Length;
			Deadlocks = verificationResult.SoundnessProperties.Deadlocks;
			Soundness = verificationResult.SoundnessProperties.Soundness;
			VerificationTime = verificationResult.VerificationTime!.Value.ToString();
			VerificationStatesConsidered = verificationResult.TotalStatesConsidered;
			VerificationRefinementsCount = (ushort)verificationResult.TotalRefinementsDone;
			SatisfiesCounditions = satisfiesConditions;
			Id = dpn.Name;

			RepairTime = repairResult?.RepairTime.ToString();
			RepairSuccess = repairResult?.IsSuccess;
			RepairSteps = repairResult?.RepairSteps ?? 0;
			EnhancedTransitions = (ushort)(repairResult?.RepairModifications.EnhancedTransitions.Count ?? 0);
			EnhancedButRolledBackTransitions = (ushort)(repairResult?.RepairModifications.TransitionEnhancedButRolledBack.Count ?? 0);
			RepairStatesConsidered = repairResult?.TotalStatesConsidered ?? 0;
			RepairRefinementsCount = (ushort)(repairResult?.TotalRefinementsDone ?? 0);
		}
	}

	public class VerificationOutputWithNumber : MainVerificationInfo
	{
		public int Number { get; init; }

		public VerificationOutputWithNumber(MainVerificationInfo verificationOutput, int number)
		{
			Places = verificationOutput.Places;
			Transitions = verificationOutput.Transitions;
			Arcs = verificationOutput.Arcs;
			Variables = verificationOutput.Variables;
			Conditions = verificationOutput.Conditions;
			Boundedness = verificationOutput.Boundedness;
			DeadTransitions = verificationOutput.DeadTransitions;
			Deadlocks = verificationOutput.Deadlocks;
			Soundness = verificationOutput.Soundness;
			VerificationTime = verificationOutput.VerificationTime;
			Number = number;
			SatisfiesCounditions = verificationOutput.SatisfiesCounditions;
			StateSpaceNodes = verificationOutput.StateSpaceNodes;
			StateSpaceArcs = verificationOutput.StateSpaceArcs;
			VerificationStatesConsidered = verificationOutput.VerificationStatesConsidered;
			VerificationRefinementsCount = verificationOutput.VerificationRefinementsCount;
			Id = verificationOutput.Id;
			RepairTime = verificationOutput.RepairTime;
			RepairSuccess = verificationOutput.RepairSuccess;
			RepairSteps = verificationOutput.RepairSteps;
			EnhancedTransitions = verificationOutput.EnhancedTransitions;
			EnhancedButRolledBackTransitions = verificationOutput.EnhancedButRolledBackTransitions;
			RepairStatesConsidered = verificationOutput.RepairStatesConsidered;
			RepairRefinementsCount = verificationOutput.RepairRefinementsCount;
		}
	}
}