using DataPetriNetOnSmt;
using DataPetriNetOnSmt.SoundnessVerification.Services;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using DataPetriNetOnSmt.SoundnessVerification;

namespace DataPetriNetVerificationDomain
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
        public int LtsStates { get; init; }
        public int LtsArcs { get; init; }
        public int CgRefStates { get; init; }
        public int CgRefArcs { get; init; }
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
            ClassicalLabeledTransitionSystem lts,
            ConstraintGraph? cg,
            ConstraintGraph? cgRefined,
            SoundnessProperties? soundnessProperties,
            long millisecondsForVerification,
            long millisecondsForRepair,
            bool repairSuccess)
        {
            Places = (ushort)dpn.Places.Count;
            Transitions = (ushort)dpn.Transitions.Count;
            Arcs = (ushort)dpn.Arcs.Count;
            Variables = (ushort)dpn.Variables.GetAllVariables().Count;
            Conditions = (ushort)dpn.Transitions
                .SelectMany(x => x.Guard.BaseConstraintExpressions.Select(y => y.GetSmtExpression(dpn.Context)))
                .Distinct()
                .Count();
            Boundedness = soundnessProperties?.Boundedness ?? false ;
            LtsStates = lts.ConstraintStates.Count;
            LtsArcs = lts.ConstraintArcs.Count;
            CgStates = cg?.ConstraintStates.Count ?? -1;
            CgArcs = cg?.ConstraintArcs.Count ?? -1;
            DeadTransitions = (ushort)(soundnessProperties?.DeadTransitions.Length ?? 0);
            Deadlocks = soundnessProperties?.Deadlocks ?? false;
            Soundness = soundnessProperties?.Soundness ?? false;
            VerificationTime = millisecondsForVerification.ToString();
            SatisfiesCounditions = satisfiesConditions;
            Id = dpn.Name;

            CgRefArcs = cgRefined?.ConstraintArcs?.Count ?? -1;
            CgRefStates= cgRefined?.ConstraintStates?.Count ?? -1;
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
            LtsStates = verificationOutput.LtsStates;
            LtsArcs = verificationOutput.LtsArcs;
            CgRefArcs = verificationOutput.CgRefArcs;
            CgRefStates= verificationOutput.CgRefStates;
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
