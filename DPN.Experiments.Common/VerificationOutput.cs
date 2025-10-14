using DPN.Models;
using DPN.Soundness;
using DPN.Soundness.TransitionSystems.Reachability;
using DPN.Soundness.TransitionSystems.StateSpaceGraph;

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
        public string? RepairTime { get; init; }
        public bool? RepairSuccess { get; init; }
        public string Id { get; init; }
        public int CgStates { get; init; }
        public int CgArcs { get; init; } 
        
        public MainVerificationInfo()
        {

        }
        
        public MainVerificationInfo(
            DataPetriNet dpn, 
            bool satisfiesConditions,
            StateSpaceAbstraction stateSpace,
            SoundnessProperties? soundnessProperties,
            long millisecondsForVerification,
            long millisecondsForRepair,
            bool repairSuccess)
        {
            Places = (ushort)dpn.Places.Count;
            Transitions = (ushort)dpn.Transitions.Count;
            Arcs = (ushort)dpn.Arcs.Count;
            Variables = (ushort)dpn.Variables.GetAllVariables().Length;
            Conditions = (ushort)dpn.Transitions
                .Sum(x => AtomicFormulaCounter.CountAtomicFormulas(x.Guard.BaseConstraintExpressions));
            Boundedness = soundnessProperties?.Boundedness ?? false ;
            StateSpaceNodes = stateSpace.Nodes.Length;
            StateSpaceArcs = stateSpace.Arcs.Length;
            DeadTransitions = (ushort)(soundnessProperties?.DeadTransitions.Length ?? 0);
            Deadlocks = soundnessProperties?.Deadlocks ?? false;
            Soundness = soundnessProperties?.Soundness ?? false;
            VerificationTime = millisecondsForVerification.ToString();
            SatisfiesCounditions = satisfiesConditions;
            Id = dpn.Name;

            RepairTime = millisecondsForRepair.ToString();
            RepairSuccess = repairSuccess;
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
            Id = verificationOutput.Id;
            RepairTime = verificationOutput.RepairTime;
            RepairSuccess = verificationOutput.RepairSuccess;
        }
    }
}
